-- ============================================================
-- SQL Server Full-Text Search Setup for Semsar Real Estate
-- Run this script once on the target database (RealEstateDb).
-- The SearchService checks for catalog 'SemsarCatalog' at startup.
-- ============================================================

-- Step 1: Create Full-Text Catalog (idempotent)
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'SemsarCatalog')
BEGIN
    CREATE FULLTEXT CATALOG SemsarCatalog AS DEFAULT;
    PRINT 'Created full-text catalog: SemsarCatalog';
END
ELSE
    PRINT 'Full-text catalog SemsarCatalog already exists';
GO

-- Step 2: Full-text index on Properties
-- The code uses CONTAINSTABLE on (TitleEn, TitleAr, Location).
-- Requires PK_Properties unique index (the PK on Id).
IF NOT EXISTS (
    SELECT 1 FROM sys.fulltext_indexes
    WHERE object_id = OBJECT_ID('Properties')
)
BEGIN
    CREATE FULLTEXT INDEX ON Properties(
        TitleEn        LANGUAGE 1033,  -- English
        TitleAr        LANGUAGE 1025,  -- Arabic
        Location       LANGUAGE 1033   -- English (location names)
    )
    KEY INDEX PK_Properties
    ON SemsarCatalog
    WITH (CHANGE_TRACKING AUTO);
    PRINT 'Created full-text index on Properties';
END
ELSE
    PRINT 'Full-text index on Properties already exists';
GO

-- Step 3: (Optional) Full-text index on Projects for future use
IF NOT EXISTS (
    SELECT 1 FROM sys.fulltext_indexes
    WHERE object_id = OBJECT_ID('Projects')
)
BEGIN
    CREATE FULLTEXT INDEX ON Projects(
        NameEn         LANGUAGE 1033,
        NameAr         LANGUAGE 1025,
        Location       LANGUAGE 1033,
        Developer      LANGUAGE 1033
    )
    KEY INDEX PK_Projects
    ON SemsarCatalog
    WITH (CHANGE_TRACKING AUTO);
    PRINT 'Created full-text index on Projects';
END
ELSE
    PRINT 'Full-text index on Projects already exists';
GO

-- Verify
SELECT
    OBJECT_NAME(object_id) AS [Table],
    name AS [Catalog],
    is_active AS [IsActive],
    change_tracking_state_desc AS [ChangeTracking]
FROM sys.fulltext_indexes
WHERE object_id IN (OBJECT_ID('Properties'), OBJECT_ID('Projects'));
GO
