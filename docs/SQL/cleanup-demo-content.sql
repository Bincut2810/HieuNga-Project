-- =============================================================
-- Cleanup demo content before going live
-- =============================================================
-- Removes:
--   1. Internal developer-note highlights from motorcycles:
--        "Dữ liệu demo — chỉnh sửa trong CMS"
--        "Ảnh local ổn định (không phụ thuộc CDN)"
--        "Máy tính trả góp bắt mặc định"
--      plus any other highlight whose text contains "demo".
--   2. Trailing " (demo)" annotations on spec values such as
--        "≈ 2.0–2.6 L/100km (demo)" or "3 năm hoặc 30.000 km (demo)".
--
-- Safe to run multiple times. Run in a transaction, review the
-- affected rows, then COMMIT.
-- =============================================================

BEGIN;

-- -------------------------------------------------------------
-- 1) Strip demo highlights out of motorcycles.HighlightsJson
-- -------------------------------------------------------------
UPDATE motorcycles
SET "HighlightsJson" = COALESCE(
        (
            SELECT jsonb_agg(value)
            FROM jsonb_array_elements_text("HighlightsJson"::jsonb) AS value
            WHERE value NOT ILIKE '%demo%'
              AND value NOT ILIKE '%chỉnh sửa trong CMS%'
              AND value NOT ILIKE '%phụ thuộc CDN%'
              AND value NOT ILIKE '%bắt mặc định%'
        ),
        '[]'::jsonb
    ),
    "UpdatedAt" = NOW()
WHERE "HighlightsJson" IS NOT NULL
  AND (
        "HighlightsJson" ILIKE '%demo%'
        OR "HighlightsJson" ILIKE '%chỉnh sửa trong CMS%'
        OR "HighlightsJson" ILIKE '%phụ thuộc CDN%'
        OR "HighlightsJson" ILIKE '%bắt mặc định%'
  );

-- -------------------------------------------------------------
-- 2) Strip trailing " (demo)" from any spec value
-- -------------------------------------------------------------
UPDATE motorcycles
SET "TechnicalSpecsJson" = REPLACE("TechnicalSpecsJson", ' (demo)', ''),
    "UpdatedAt" = NOW()
WHERE "TechnicalSpecsJson" ILIKE '%(demo)%'
   OR "TechnicalSpecsJson" ILIKE '%(DEMO)%'
   OR "TechnicalSpecsJson" ILIKE '%( Demo )%';

-- -------------------------------------------------------------
-- 3) Review what was touched
-- -------------------------------------------------------------
SELECT "Id", "Slug", "Name",
       LEFT("HighlightsJson", 120)      AS highlights,
       LEFT("TechnicalSpecsJson", 200)  AS specs
FROM motorcycles
WHERE "HighlightsJson" ILIKE '%demo%'
   OR "TechnicalSpecsJson" ILIKE '%(demo)%';

-- COMMIT;   -- uncomment after review
ROLLBACK;
