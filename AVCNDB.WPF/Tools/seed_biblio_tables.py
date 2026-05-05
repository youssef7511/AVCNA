"""
Seed the biblio reference tables that were left empty by the CNAM ingestion.
Idempotent: only inserts rows that don't already exist (by itemname).

Targets: voie, family, labos, specialites, poso
"""
from __future__ import annotations
import sys
import io
import datetime as dt
import pymysql

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

DB_CFG = dict(host='127.0.0.1', port=3307, user='medwin', password='0101',
              database='MEDICDB', charset='utf8mb4')

# ============================================
# VOIE D'ADMINISTRATION
# ============================================
VOIES = [
    ('Orale', 'PO', 'Per os'),
    ('Cutanée', 'CUT', 'Application sur la peau'),
    ('Ophtalmique', 'OPH', 'Instillation oculaire'),
    ('Auriculaire', 'AUR', 'Instillation auriculaire'),
    ('Nasale', 'NAS', 'Pulvérisation/instillation nasale'),
    ('Vaginale', 'VAG', 'Voie vaginale'),
    ('Rectale', 'REC', 'Voie rectale'),
    ('Inhalation', 'INH', 'Voie inhalée'),
    ('Intraveineuse', 'IV', 'Injection intraveineuse'),
    ('Intramusculaire', 'IM', 'Injection intramusculaire'),
    ('Sous-cutanée', 'SC', 'Injection sous-cutanée'),
    ('Buccale', 'BUC', 'Voie buccale (gomme, comprimé à sucer)'),
    ('Sublinguale', 'SL', 'Voie sublinguale'),
    ('Transdermique', 'TD', 'Patch transdermique'),
]

# ============================================
# FAMILLES THÉRAPEUTIQUES
# ============================================
FAMILIES = [
    ('Antalgiques', 'Médicaments contre la douleur'),
    ('Anti-inflammatoires non stéroïdiens (AINS)', 'AINS'),
    ('Antibiotiques', 'Anti-infectieux bactériens'),
    ('Antifongiques', 'Anti-infectieux fongiques'),
    ('Antiviraux', 'Anti-infectieux viraux'),
    ('Antiparasitaires', 'Anti-infectieux parasitaires'),
    ('Antidiabétiques', 'Hypoglycémiants'),
    ('Antihypertenseurs', 'Anti-HTA'),
    ('Antihistaminiques', 'Anti-allergiques'),
    ('Antiulcéreux', 'IPP, antiacides'),
    ('Anticoagulants', 'AVK, AOD, héparines'),
    ('Antiagrégants plaquettaires', 'Aspirine, clopidogrel'),
    ('Antidépresseurs', 'ISRS, IRSN, tricycliques'),
    ('Antipsychotiques', 'Neuroleptiques'),
    ('Antiépileptiques', 'Anticonvulsivants'),
    ('Anxiolytiques', 'Benzodiazépines'),
    ('Bronchodilatateurs', 'Bêta-2-mimétiques'),
    ('Corticoïdes', 'Glucocorticoïdes'),
    ('Diurétiques', 'Anti-HTA, OAP'),
    ('Hypolipémiants', 'Statines, fibrates'),
    ('Vitamines & oligoéléments', 'Suppléments'),
    ('Hormones thyroïdiennes', 'Lévothyroxine'),
    ('Vaccins', 'Immunisation active'),
    ('Anti-asthmatiques', 'Asthme et BPCO'),
    ('Antimigraineux', 'Triptans, dérivés ergot'),
    ('Antinauséeux', 'Anti-émétiques'),
    ('Laxatifs', 'Constipation'),
    ('Antidiarrhéiques', 'Diarrhée aiguë'),
    ('Contraceptifs', 'Contraception orale'),
]

# ============================================
# LABORATOIRES (présents sur le marché tunisien)
# ============================================
LABOS = [
    ('Sanofi', 'France'),
    ('Pfizer', 'USA'),
    ('Novartis', 'Suisse'),
    ('Saidal', 'Algérie'),
    ('Adwya', 'Tunisie'),
    ('Opalia', 'Tunisie'),
    ('Médis', 'Tunisie'),
    ('Galien', 'Tunisie'),
    ('Téva', 'Israël'),
    ('Roche', 'Suisse'),
    ('Bayer', 'Allemagne'),
    ('GSK', 'Royaume-Uni'),
    ('Merck', 'Allemagne'),
    ('AstraZeneca', 'Royaume-Uni'),
    ('ABDI Ibrahim', 'Turquie'),
    ('Pharmaghreb', 'Tunisie'),
    ('Julphar', 'EAU'),
    ('Hikma', 'Jordanie'),
    ('Servier', 'France'),
    ('Boehringer Ingelheim', 'Allemagne'),
    ('Eli Lilly', 'USA'),
    ('Johnson & Johnson', 'USA'),
    ('Abbott', 'USA'),
    ('Lundbeck', 'Danemark'),
    ('Biopharm', 'Tunisie'),
    ('SIPHAT', 'Tunisie'),
    ('Unimed', 'Tunisie'),
]

# ============================================
# SPÉCIALITÉS MÉDICALES
# ============================================
SPECIALITES = [
    ('Cardiologie', 'CARDIO'),
    ('Dermatologie', 'DERM'),
    ('Endocrinologie', 'ENDO'),
    ('Gastro-entérologie', 'GASTRO'),
    ('Gynécologie', 'GYN'),
    ('Hématologie', 'HEMATO'),
    ('Infectiologie', 'INFECT'),
    ('Néphrologie', 'NEPHRO'),
    ('Neurologie', 'NEURO'),
    ('Ophtalmologie', 'OPH'),
    ('ORL', 'ORL'),
    ('Pédiatrie', 'PED'),
    ('Pneumologie', 'PNEUMO'),
    ('Psychiatrie', 'PSY'),
    ('Rhumatologie', 'RHUMA'),
    ('Urologie', 'URO'),
    ('Oncologie', 'ONCO'),
    ('Anesthésie-Réanimation', 'ANES'),
    ('Médecine générale', 'MG'),
    ('Chirurgie', 'CHIR'),
    ('Stomatologie', 'STOMA'),
    ('Allergologie', 'ALLER'),
]

# ============================================
# POSOLOGIES TYPES
# ============================================
POSOS = [
    # (itemname, qty, posoform, prises, periode, conditions, nameformul)
    ('1 cp x 3 / jour',          1, 'Comprimé', 3, 'jour',    'Aux repas',                'Standard'),
    ('1 cp x 2 / jour',          1, 'Comprimé', 2, 'jour',    'Matin et soir',            'Standard'),
    ('1 cp x 1 / jour',          1, 'Comprimé', 1, 'jour',    'Le matin',                 'Standard'),
    ('2 cp x 2 / jour',          2, 'Comprimé', 2, 'jour',    'Aux repas',                'Standard'),
    ('1 gel x 2 / jour',         1, 'Gélule',   2, 'jour',    'Aux repas',                'Standard'),
    ('1 gel x 3 / jour',         1, 'Gélule',   3, 'jour',    'Aux repas',                'Standard'),
    ('5 ml x 3 / jour',          5, 'ml',       3, 'jour',    'Aux repas',                'Pédiatrique'),
    ('10 ml x 3 / jour',        10, 'ml',       3, 'jour',    'Aux repas',                'Adulte'),
    ('1 sachet x 2 / jour',      1, 'Sachet',   2, 'jour',    'Dilué dans un verre d''eau','Standard'),
    ('1 amp x 1 / jour',         1, 'Ampoule',  1, 'jour',    'IM ou IV lente',           'Injectable'),
    ('1 supp x 2 / jour',        1, 'Suppositoire', 2, 'jour','Voie rectale',             'Suppositoire'),
    ('1 goutte x 3 / jour',      1, 'Goutte',   3, 'jour',    'Dans l''oeil atteint',     'Collyre'),
    ('1 application x 2 / jour', 1, 'Application', 2, 'jour', 'Sur la zone atteinte',     'Topique'),
    ('1 inhalation x 2 / jour',  1, 'Bouffée',  2, 'jour',    'Bien rincer la bouche après','Inhalation'),
    ('Selon prescription',       0, '',         0, '',        'Voir ordonnance',          'Personnalisé'),
]


def main():
    cn = pymysql.connect(**DB_CFG, autocommit=False)
    try:
        cur = cn.cursor()
        now = dt.datetime.now()

        # --- voie ---
        cur.execute("SELECT itemname FROM voie")
        existing = {r[0] for r in cur.fetchall()}
        rows = [(name, ab, sub, now, now)
                for name, ab, sub in VOIES if name not in existing]
        if rows:
            cur.executemany(
                "INSERT INTO voie (itemname, abname, subvalue, addedat, updatedat) "
                "VALUES (%s,%s,%s,%s,%s)", rows)
        print(f"voie:        +{len(rows)} (now {len(existing)+len(rows)})")

        # --- family ---
        cur.execute("SELECT itemname FROM family")
        existing = {r[0] for r in cur.fetchall()}
        rows = [(name, sub, now, now)
                for name, sub in FAMILIES if name not in existing]
        if rows:
            cur.executemany(
                "INSERT INTO family (itemname, subvalue, addedat, updatedat) "
                "VALUES (%s,%s,%s,%s)", rows)
        print(f"family:      +{len(rows)} (now {len(existing)+len(rows)})")

        # --- labos ---
        cur.execute("SELECT itemname FROM labos")
        existing = {r[0] for r in cur.fetchall()}
        rows = [(name, sub, now, now)
                for name, sub in LABOS if name not in existing]
        if rows:
            cur.executemany(
                "INSERT INTO labos (itemname, subvalue, addedat, updatedat) "
                "VALUES (%s,%s,%s,%s)", rows)
        print(f"labos:       +{len(rows)} (now {len(existing)+len(rows)})")

        # --- specialites ---
        cur.execute("SELECT itemname FROM specialites")
        existing = {r[0] for r in cur.fetchall()}
        rows = [(name, ab, '', now, now)
                for name, ab in SPECIALITES if name not in existing]
        if rows:
            cur.executemany(
                "INSERT INTO specialites (itemname, abname, subvalue, addedat, updatedat) "
                "VALUES (%s,%s,%s,%s,%s)", rows)
        print(f"specialites: +{len(rows)} (now {len(existing)+len(rows)})")

        # --- poso ---
        cur.execute("SELECT itemname FROM poso")
        existing = {r[0] for r in cur.fetchall()}
        rows = [(itemname, qty, posoform, prises, periode, conditions, nameformul, '', now, now)
                for itemname, qty, posoform, prises, periode, conditions, nameformul in POSOS
                if itemname not in existing]
        if rows:
            cur.executemany(
                "INSERT INTO poso (itemname, qty, posoform, prises, periode, conditions, "
                "nameformul, subvalue, addedat, updatedat) "
                "VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)", rows)
        print(f"poso:        +{len(rows)} (now {len(existing)+len(rows)})")

        cn.commit()
        print("\nCOMMIT OK.")
    except Exception:
        cn.rollback()
        raise
    finally:
        cn.close()


if __name__ == '__main__':
    main()
