ALTER TABLE "Customers" ADD COLUMN IF NOT EXISTS "IsDefault" boolean NOT NULL DEFAULT FALSE;
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Customers_IsDefault" ON "Customers"("IsDefault") WHERE "IsDefault";
INSERT INTO "Customers"("Name","Ci","Contact","IsDefault")
SELECT 'Consumidor final','','',TRUE WHERE NOT EXISTS(SELECT 1 FROM "Customers" WHERE "IsDefault");
INSERT INTO "GozoSchemaVersions"("Version","AppliedUtc") VALUES('005-default-customer',CURRENT_TIMESTAMP::text) ON CONFLICT("Version") DO NOTHING;
