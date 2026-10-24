using static GOZO.Rules;
namespace GOZO;
public class ReportService(ISaleRepository repository, ICustomerRepository customers) : IReportService
{
    public async Task<object> Generate(string? from, string? to) => Report(await Sales(from, to, null));
    public async Task<object> History(int id, string? from, string? to) {
        var customer = await customers.Find(id) ?? throw new RuleException("Cliente inexistente.");
        return new { customer, report = Report(await Sales(from, to, id)) };
    }
    public async Task<byte[]> Excel(string? from, string? to) => Exports.Excel(await Sales(from, to, null));
    public async Task<byte[]> Pdf(string? from, string? to) => Exports.Pdf(await Sales(from, to, null), from, to);
    public async Task<List<Sale>> Sales( string? desde, string? hasta, int? customer) {
        var start = Parse(desde, customer != null ? DateTime.MinValue.Date : Now().Date); var end = Parse(hasta, Now().Date);
        if (start > end) throw new RuleException("El rango de fechas está invertido.");
        if (end.Year >= 9999) throw new RuleException("Fecha fuera de rango.");
        end = end.AddDays(1); // Fin exclusivo: incluye todo el último día.
        return await repository.Range(start, end, customer);
    }
}
