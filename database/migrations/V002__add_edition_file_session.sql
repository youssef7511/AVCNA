-- =============================================================================
-- V002: Table des sessions d'import de fichiers d'édition
-- Trace l'historique de chaque import de fichier d'édition
-- =============================================================================

CREATE TABLE IF NOT EXISTS edition_file_sessions (
    recordid        INT AUTO_INCREMENT PRIMARY KEY,
    filepath        VARCHAR(500)    NOT NULL,
    sourcetype      VARCHAR(50)     NOT NULL,
    description     VARCHAR(200)    NULL DEFAULT '',
    totalrows       INT             NOT NULL DEFAULT 0,
    unknownrows     INT             NOT NULL DEFAULT 0,
    approvedrows    INT             NOT NULL DEFAULT 0,
    rejectedrows    INT             NOT NULL DEFAULT 0,
    status          VARCHAR(20)     NOT NULL DEFAULT 'InProgress',
    operatedby      VARCHAR(100)    NULL DEFAULT '',
    completedat     DATETIME        NULL,
    addedat         DATETIME        NULL,
    updatedat       DATETIME        NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Index sur le statut pour filtrer les sessions actives
CREATE INDEX IX_EditionFileSession_Status ON edition_file_sessions (status);

-- Index sur la date de création pour tri chronologique
CREATE INDEX IX_EditionFileSession_AddedAt ON edition_file_sessions (addedat);
