-- 001: pedidos. Migración aditiva e idempotente; no modifica ventas existentes.
CREATE TABLE IF NOT EXISTS "Orders" (
 "Id" INTEGER PRIMARY KEY AUTOINCREMENT, "RequestId" TEXT NOT NULL, "SaleRequestId" TEXT NOT NULL,
 "Revision" TEXT NOT NULL, "CreatedAt" TEXT NOT NULL, "ClosedAt" TEXT NULL,
 "Status" TEXT NOT NULL, "ContactName" TEXT NOT NULL, "ContactPhone" TEXT NOT NULL,
 "Notes" TEXT NOT NULL, "CustomerId" INTEGER NULL,
 "Subtotal" INTEGER NOT NULL, "Discount" INTEGER NOT NULL, "Total" INTEGER NOT NULL,
 "SaleId" INTEGER NULL,
 FOREIGN KEY ("CustomerId") REFERENCES "Customers"("Id") ON DELETE RESTRICT,
 FOREIGN KEY ("SaleId") REFERENCES "Sales"("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Orders_RequestId" ON "Orders"("RequestId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Orders_SaleRequestId" ON "Orders"("SaleRequestId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Orders_SaleId" ON "Orders"("SaleId");
CREATE INDEX IF NOT EXISTS "IX_Orders_CustomerId" ON "Orders"("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_Orders_Status_Id" ON "Orders"("Status","Id");
CREATE TABLE IF NOT EXISTS "OrderLines" (
 "Id" INTEGER PRIMARY KEY AUTOINCREMENT, "OrderId" INTEGER NOT NULL, "ProductId" INTEGER NOT NULL,
 "Name" TEXT NOT NULL, "UnitPrice" INTEGER NOT NULL, "Quantity" INTEGER NOT NULL,
 "Discount" INTEGER NOT NULL, "Total" INTEGER NOT NULL, "Promotion" TEXT NOT NULL,
 FOREIGN KEY ("OrderId") REFERENCES "Orders"("Id") ON DELETE CASCADE,
 FOREIGN KEY ("ProductId") REFERENCES "Products"("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_OrderLines_OrderId" ON "OrderLines"("OrderId");
CREATE INDEX IF NOT EXISTS "IX_OrderLines_ProductId" ON "OrderLines"("ProductId");
CREATE TABLE IF NOT EXISTS "GozoSchemaVersions" ("Version" TEXT PRIMARY KEY, "AppliedUtc" TEXT NOT NULL);
INSERT INTO "GozoSchemaVersions"("Version","AppliedUtc") VALUES ('001-orders', CURRENT_TIMESTAMP) ON CONFLICT("Version") DO NOTHING;
