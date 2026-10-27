-- 003: fotos pequeñas persistentes; no altera productos o ventas existentes.
CREATE TABLE IF NOT EXISTS "ProductImages" (
 "ProductId" INTEGER PRIMARY KEY,
 "Bytes" BLOB NOT NULL,
 "Version" TEXT NOT NULL,
 FOREIGN KEY("ProductId") REFERENCES "Products"("Id") ON DELETE CASCADE
);
INSERT INTO "GozoSchemaVersions"("Version","AppliedUtc") VALUES('003-product-images',CURRENT_TIMESTAMP) ON CONFLICT("Version") DO NOTHING;
