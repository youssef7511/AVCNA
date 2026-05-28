"""
One-shot data ingestion: wipes the data tables and loads the CNAM VEI Excel file.

Tables wiped (data only; schema/migrations preserved):
    medic, dci, family, labos, formes, voie, presents, specialites,
    poso, interact, stock, catveic, specmedic

Source file: ../../MEDIC-CNAM-V E I-18-03-2025.xlsx
"""
from __future__ import annotations
import re
import sys
import datetime as dt
from pathlib import Path
import pandas as pd
import pymysql

DB_CFG = dict(host='127.0.0.1', port=3307, user='medwin', password='0101',
              database='MEDICDB', charset='utf8mb4')

XLSX_PATH = Path(__file__).resolve().parents[3] / "MEDIC-CNAM-V-E-I-18-03-2025.xlsx"

WIPE_TABLES = [
    'specmedic', 'stock', 'interact', 'poso',
    'medic',  # data rows
    'specialites', 'presents', 'voie', 'formes', 'labos',
    'family', 'dci', 'catveic',
]

# ---------- Forme detection dictionary (abbreviation → canonical name) ----------
FORME_PATTERNS = [
    (r'\bcomp\.?\s*pell\.?\b', 'Comprimé pelliculé'),
    (r'\bcomp\.?\s*s[eé]c\.?\b', 'Comprimé sécable'),
    (r'\bcomp\.?\s*eff\.?\b',  'Comprimé effervescent'),
    (r'\bcomp\.?\s*disp\.?\b', 'Comprimé dispersible'),
    (r'\bcomp\.?\s*orodisp\.?\b', 'Comprimé orodispersible'),
    (r'\bcomp\b\.?',           'Comprimé'),
    (r'\bg[eé]lule\b',         'Gélule'),
    (r'\bcaps?\.?\b',          'Capsule'),
    (r'\bsusp\.?\s*or\.?\b',   'Suspension orale'),
    (r'\bsusp\.?\b',           'Suspension'),
    (r'\bsirop\b',             'Sirop'),
    (r'\bsol\.?\s*inj\.?\b',   'Solution injectable'),
    (r'\bsol\.?\s*or\.?\b',    'Solution orale'),
    (r'\bsol\.?\s*buv\.?\b',   'Solution buvable'),
    (r'\bsol\b\.?',            'Solution'),
    (r'\bcoll\.?yre?\b',       'Collyre'),
    (r'\bp[oô]m\.?\s*derm\.?\b','Pommade dermique'),
    (r'\bp[oô]m\.?\b',         'Pommade'),
    (r'\bcr[eè]me\b',          'Crème'),
    (r'\bgel\b',               'Gel'),
    (r'\bspray\b',             'Spray'),
    (r'\ba[eé]rosol\b',        'Aérosol'),
    (r'\bovule\b',             'Ovule'),
    (r'\bsuppos?\.?\b',        'Suppositoire'),
    (r'\bp[âa]te\b',           'Pâte'),
    (r'\bsachet\b',            'Sachet'),
    (r'\bgranul[eé]s?\b',      'Granulés'),
    (r'\bpoudre?\b',           'Poudre'),
    (r'\bpatch\b',             'Patch'),
    (r'\bgttes?\b',            'Gouttes'),
    (r'\bser?\.?\s*pr[eé]-?rempli\.?\b', 'Seringue pré-remplie'),
    (r'\bstylo\b',             'Stylo'),
    (r'\binhalateur?\b',       'Inhalateur'),
    (r'\bovules?\b',           'Ovule'),
]

# ---------- Présentation patterns (container + count) ----------
PRESENT_PATTERNS = [
    (r'\b(bt|b/?)\s*(\d+)\b',  'Boîte'),
    (r'\bfl\.?\s*(\d+(?:\.\d+)?(?:ml|gr|g|cc)?)?', 'Flacon'),
    (r'\btb\s*(\d+(?:\.\d+)?(?:gr|g|ml)?)', 'Tube'),
    (r'\bplq?\.?\s*(\d+)\b',   'Plaquette'),
    (r'\bsach\.?\s*(\d+)\b',   'Sachet'),
]

DOSE_RE = re.compile(r'(\d+(?:[.,]\d+)?)\s*(mg/ml|mg/\d+ml|mg|g|ml|UI|µg|mcg|%|MG|G|ML)', re.IGNORECASE)
COLISAGE_RE = re.compile(r'\b(?:bt|b/?|fl)\s*(\d+)\b', re.IGNORECASE)


def detect_forme(name: str) -> str:
    n = name.lower()
    for rx, canonical in FORME_PATTERNS:
        if re.search(rx, n):
            return canonical
    return ''


def detect_present(name: str) -> str:
    n = name.lower()
    for rx, canonical in PRESENT_PATTERNS:
        if re.search(rx, n):
            return canonical
    return ''


def detect_dose(name: str) -> tuple[str, str]:
    m = DOSE_RE.search(name)
    if not m:
        return '', ''
    return m.group(1).replace(',', '.'), m.group(2)


def detect_colisage(name: str) -> int:
    m = COLISAGE_RE.search(name)
    return int(m.group(1)) if m else 0


def to_millimes(price_dt: float) -> int:
    """4.900 DT → 4900 millimes (int)."""
    return int(round(price_dt * 1000))


def main() -> int:
    if not XLSX_PATH.exists():
        print(f"ERROR: Excel not found at {XLSX_PATH}", file=sys.stderr)
        return 1

    df = pd.read_excel(XLSX_PATH, sheet_name='VEI')
    print(f"Loaded {len(df)} rows from Excel.")

    # Build derived columns
    df['_forme'] = df['NOM_COMMERCIAL'].apply(detect_forme)
    df['_present'] = df['NOM_COMMERCIAL'].apply(detect_present)
    df['_colisage'] = df['NOM_COMMERCIAL'].apply(detect_colisage)
    df['_dose1'], df['_u1'] = zip(*df['NOM_COMMERCIAL'].apply(detect_dose))

    # Connect
    cn = pymysql.connect(**DB_CFG, autocommit=False)
    try:
        cur = cn.cursor()

        # --- Wipe ---
        cur.execute("SET FOREIGN_KEY_CHECKS = 0")
        for t in WIPE_TABLES:
            cur.execute(f"TRUNCATE TABLE `{t}`")
            print(f"  wiped {t}")
        cur.execute("SET FOREIGN_KEY_CHECKS = 1")

        now = dt.datetime.now()

        # --- Catveic (E/I/V) ---
        veic_map = {
            'E': ('Essentiel',     'Médicament essentiel'),
            'I': ('Intermédiaire', 'Médicament intermédiaire'),
            'V': ('Vital',         'Médicament vital'),
        }
        for code, (name, desc) in veic_map.items():
            cur.execute(
                "INSERT INTO catveic (itemname, code, description, pictogram, addedat, updatedat) "
                "VALUES (%s,%s,%s,%s,%s,%s)",
                (name, code, desc, '', now, now))
        print(f"  inserted {len(veic_map)} catveic")

        # --- DCI ---
        dcis = sorted(set(df['DCI'].dropna().str.strip()))
        cur.executemany(
            "INSERT INTO dci (itemname, subvalue, iteminfo, addedat, updatedat) "
            "VALUES (%s, '', '', %s, %s)",
            [(d, now, now) for d in dcis if d])
        print(f"  inserted {len(dcis)} dci")

        # --- Formes (canonical names from detection) ---
        formes = sorted(set(f for f in df['_forme'] if f))
        cur.executemany(
            "INSERT INTO formes (itemname, subvalue, formgroup, abname, posoform, posoname, addedat, updatedat) "
            "VALUES (%s, '', '', '', '', '', %s, %s)",
            [(f, now, now) for f in formes])
        print(f"  inserted {len(formes)} formes")

        # --- Presents ---
        presents = sorted(set(p for p in df['_present'] if p))
        cur.executemany(
            "INSERT INTO presents (itemname, abname, subvalue, addedat, updatedat) "
            "VALUES (%s, '', '', %s, %s)",
            [(p, now, now) for p in presents])
        print(f"  inserted {len(presents)} presents")

        # --- Empty biblio tables (user fills later) ---
        # voie, family, labos, specialites, poso, interact, stock, specmedic stay empty
        print("  voie/family/labos/specialites/poso/interact/stock/specmedic: empty (to fill later)")

        # --- Medic (the main payload) ---
        medic_cols = (
            'medicno','medicid','barcode','pctcode','amm','itemname','shortname','basename',
            'forme','voie','formgroup','groupid','present','colisage','posology',
            'dci1','dci2','dci3','dci4','dci',
            'fam1','fam2','fam3','family','specialite',
            'pediatric','veic','isap','isic',
            'price','refprice','ictx','icamount','ocamount','pamount',
            'tableau','pctprice','timbrepct','netprice','labo',
            'dose1','dose2','dose3','dose4','dose5',
            'u1','u2','u3','u4','u5','ucol','unite',
            'nameform','indication','mgarde',
            'monogat','ciat','addedat','updatedat','deletedat',
            'isactive','rowtype','itemtype','tatouage','isotc'
        )
        placeholders = ','.join(['%s'] * len(medic_cols))
        sql = f"INSERT INTO medic ({','.join('`'+c+'`' for c in medic_cols)}) VALUES ({placeholders})"

        rows = []
        for i, r in df.iterrows():
            ppv = to_millimes(float(r['PRIX_PUBLIC']))
            ref = to_millimes(float(r['TARIF_REFERENCE']))
            isap = 1 if str(r['AP']).strip().upper() == 'O' else 0
            dci1 = str(r['DCI']).strip()
            row = (
                int(i + 1),                  # medicno
                '',                          # medicid
                '',                          # barcode
                str(r['CODE_PCT']).strip(),  # pctcode
                '',                          # amm
                str(r['NOM_COMMERCIAL']).strip(),  # itemname
                '',                          # shortname
                '',                          # basename
                r['_forme'],                 # forme
                '',                          # voie
                '',                          # formgroup
                0,                           # groupid
                r['_present'],               # present
                int(r['_colisage']),         # colisage
                '',                          # posology
                dci1, '', '', '', dci1,      # dci1..4 + dci aggregate
                '', '', '', '', '',          # fam1..3, family, specialite
                0,                           # pediatric
                str(r['CATEGORIE']).strip(), # veic
                isap, 0,                     # isap, isic
                ppv,                         # price (we map PRIX_PUBLIC here too as default)
                ref,                         # refprice
                0, 0, 0,                     # ictx, icamount, ocamount
                ppv,                         # pamount (PPV)
                '',                          # tableau
                0, 0, 0,                     # pctprice, timbrepct, netprice
                '',                          # labo
                r['_dose1'], '', '', '', '', # dose1..5
                r['_u1'], '', '', '', '',    # u1..5
                '', 0,                       # ucol, unite
                '', '', '',                  # nameform, indication, mgarde
                None, None, now, now, None,  # monogat, ciat, addedat, updatedat, deletedat
                1,                           # isactive
                '', 0, '', 0,                # rowtype, itemtype, tatouage, isotc
            )
            rows.append(row)

        # Batch insert
        BATCH = 500
        for i in range(0, len(rows), BATCH):
            cur.executemany(sql, rows[i:i+BATCH])
            print(f"  inserted medic {min(i+BATCH, len(rows))}/{len(rows)}")

        cn.commit()
        print("\nCOMMIT OK.")

        # Verify counts
        for t in ['medic', 'dci', 'formes', 'presents', 'catveic']:
            cur.execute(f"SELECT COUNT(*) FROM `{t}`")
            print(f"  {t}: {cur.fetchone()[0]} rows")

        return 0
    except Exception:
        cn.rollback()
        raise
    finally:
        cn.close()


if __name__ == '__main__':
    sys.exit(main())
