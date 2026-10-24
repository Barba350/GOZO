using ClosedXML.Excel;
using System.Globalization;
using System.Text;

namespace GOZO;

public static class Exports
{
    public static byte[] Excel(List<Sale> sales)
    {
        using var wb = new XLWorkbook();
        var summary = wb.Worksheets.Add("Resumen");
        summary.Cell(1, 1).Value = "GOZO · Reporte de ventas (Bs)";
        summary.Cell(3, 1).Value = "Cantidad de ventas"; summary.Cell(3, 2).Value = sales.Count;
        summary.Cell(4, 1).Value = "Ingresos"; summary.Cell(4, 2).Value = sales.Sum(x => x.Total) / 100m;
        summary.Cell(5, 1).Value = "Descuentos"; summary.Cell(5, 2).Value = sales.Sum(x => x.Discount) / 100m;
        summary.Cell(6, 1).Value = "Efectivo"; summary.Cell(6, 2).Value = sales.Where(x => x.Payment == "EFECTIVO").Sum(x => x.Total) / 100m;
        summary.Cell(7, 1).Value = "QR"; summary.Cell(7, 2).Value = sales.Where(x => x.Payment == "QR").Sum(x => x.Total) / 100m;
        summary.Range("B4:B7").Style.NumberFormat.Format = "0.00";
        var sheet = wb.Worksheets.Add("Ventas");
        string[] headers = { "Venta", "Fecha (Bolivia)", "Cliente", "CI", "Forma de pago", "Subtotal Bs", "Descuento Bs", "Total Bs" };
        for (var c = 0; c < headers.Length; c++) sheet.Cell(1, c + 1).Value = headers[c];
        var row = 2;
        foreach (var s in sales) {
            sheet.Cell(row, 1).Value = s.Id; sheet.Cell(row, 2).Value = s.Date; sheet.Cell(row, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            sheet.Cell(row, 3).Value = s.Customer?.Name ?? "Consumidor final"; sheet.Cell(row, 4).Value = s.Customer?.Ci ?? "";
            sheet.Cell(row, 5).Value = s.Payment; sheet.Cell(row, 6).Value = s.Subtotal / 100m; sheet.Cell(row, 7).Value = s.Discount / 100m; sheet.Cell(row, 8).Value = s.Total / 100m; row++;
        }
        if (row > 2) sheet.Range(2, 6, row - 1, 8).Style.NumberFormat.Format = "0.00";
        var detail = wb.Worksheets.Add("Detalle");
        string[] dh = { "Venta", "Producto", "Cantidad", "Precio unitario Bs", "Descuento Bs", "Total Bs", "Promoción" };
        for (var c = 0; c < dh.Length; c++) detail.Cell(1, c + 1).Value = dh[c];
        row = 2;
        foreach (var s in sales) foreach (var l in s.Lines) {
            detail.Cell(row, 1).Value = s.Id; detail.Cell(row, 2).Value = l.Name; detail.Cell(row, 3).Value = l.Quantity;
            detail.Cell(row, 4).Value = l.UnitPrice / 100m; detail.Cell(row, 5).Value = l.Discount / 100m; detail.Cell(row, 6).Value = l.Total / 100m; detail.Cell(row, 7).Value = l.Promotion; row++;
        }
        if (row > 2) detail.Range(2, 4, row - 1, 6).Style.NumberFormat.Format = "0.00";
        var rank = wb.Worksheets.Add("Más vendidos");
        rank.Cell(1, 1).Value = "Producto"; rank.Cell(1, 2).Value = "Unidades"; rank.Cell(1, 3).Value = "Ingresos Bs"; row = 2;
        foreach (var g in sales.SelectMany(x => x.Lines).GroupBy(x => x.ProductId).OrderByDescending(x => x.Sum(z => z.Quantity))) {
            rank.Cell(row, 1).Value = g.First().Name; rank.Cell(row, 2).Value = g.Sum(x => x.Quantity); rank.Cell(row, 3).Value = g.Sum(x => x.Total) / 100m; rank.Cell(row, 3).Style.NumberFormat.Format = "0.00"; row++;
        }
        foreach (var ws in wb.Worksheets) {
            ws.Row(1).Style.Font.Bold = true; ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#184A3B"); ws.Row(1).Style.Font.FontColor = XLColor.White;
            ws.Columns().AdjustToContents(minWidth: 5, maxWidth: 65); ws.SheetView.FreezeRows(1);
        }
        using var stream = new MemoryStream(); wb.SaveAs(stream); return stream.ToArray();
    }
    // PDF paginado con fuente estándar WinAnsi, sin servicio externo ni licencia adicional.
    public static byte[] Pdf(List<Sale> sales, string? from, string? to)
    {
        string Money(long n) => (n / 100m).ToString("0.00", CultureInfo.InvariantCulture);
        var lines = new List<string> { $"Periodo: {from ?? Rules.Now().ToString("yyyy-MM-dd")} a {to ?? Rules.Now().ToString("yyyy-MM-dd")}",
            $"Ventas: {sales.Count}   Ingresos: Bs {Money(sales.Sum(x => x.Total))}   Descuentos: Bs {Money(sales.Sum(x => x.Discount))}",
            $"Efectivo: Bs {Money(sales.Where(x => x.Payment == "EFECTIVO").Sum(x => x.Total))}   QR: Bs {Money(sales.Where(x => x.Payment == "QR").Sum(x => x.Total))}", "" };
        lines.Add("PRODUCTOS MAS VENDIDOS");
        foreach (var g in sales.SelectMany(x => x.Lines).GroupBy(x => x.ProductId).OrderByDescending(x => x.Sum(z => z.Quantity))) lines.Add($"{g.First().Name} | {g.Sum(x => x.Quantity)} unidades | Bs {Money(g.Sum(x => x.Total))}");
        lines.Add(""); lines.Add("HISTORIAL Y DETALLE");
        foreach (var s in sales) {
            lines.Add($"Venta #{s.Id} | {s.Date:yyyy-MM-dd HH:mm} | {s.Payment} | Bs {Money(s.Total)}");
            lines.Add($"Cliente: {s.Customer?.Name ?? "Consumidor final"} | CI: {s.Customer?.Ci ?? "-"}");
            foreach (var l in s.Lines) lines.Add($"  {l.Quantity} x {l.Name} | P/U {Money(l.UnitPrice)} | Desc. {Money(l.Discount)} | Bs {Money(l.Total)}");
            lines.Add("");
        }
        if (sales.Count == 0) lines.Add("No hay ventas en este periodo.");
        // Separar textos extensos; nunca recortar información del cliente o producto.
        var wrapped = new List<string>();
        foreach (var line in lines) {
            for (int at = 0; at < Math.Max(1, line.Length); at += 95) wrapped.Add(line.Substring(Math.Min(at, line.Length), Math.Min(95, line.Length - at)));
        }
        var pages = wrapped.Chunk(43).ToArray(); var objects = new List<byte[]>();
        byte[] Bytes(string s) => Encoding.Latin1.GetBytes(s);
        string Esc(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", " ").Replace("\n", " ").Replace("·", "-");
        objects.Add(Bytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objects.Add(Bytes($"<< /Type /Pages /Count {pages.Length} /Kids [{string.Join(" ", Enumerable.Range(0, pages.Length).Select(i => $"{4 + i * 2} 0 R"))}] >>"));
        objects.Add(Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>"));
        for (var i = 0; i < pages.Length; i++) {
            objects.Add(Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {5 + i * 2} 0 R >>"));
            var content = $"BT /F1 17 Tf 40 751 Td (GOZO - Reporte de ventas) Tj /F1 10 Tf 0 -25 Td ({i + 1} / {pages.Length} - Moneda: Bs - Hora Bolivia) Tj 0 -25 Td /F1 9 Tf 14 TL ";
            content += string.Join(" ", pages[i].Select(l => $"({Esc(l)}) Tj T*")) + " ET";
            var data = Bytes(content); objects.Add(Bytes($"<< /Length {data.Length} >>\nstream\n{content}\nendstream"));
        }
        using var output = new MemoryStream(); void Write(string value) { var bytes = Bytes(value); output.Write(bytes); }
        Write("%PDF-1.4\n"); var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++) { offsets.Add(output.Position); Write($"{i + 1} 0 obj\n"); output.Write(objects[i]); Write("\nendobj\n"); }
        var xref = output.Position; Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); return output.ToArray();
    }
}
