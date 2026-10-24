using System.Text.RegularExpressions;
using static GOZO.Rules;
namespace GOZO;
public class CustomerService(ICustomerRepository repository, IUnitOfWork unit) : ICustomerService
{
    public async Task<List<Customer>> Search(string? search) {
        var list = await repository.List(); var key = Key(search ?? ""); var ci = Ci(search ?? "");
        return list.Where(x => key == "" || Key(x.Name).Contains(key) || Key(x.Contact).Contains(key) || (ci != "" && x.Ci.Contains(ci))).ToList();
    }
    public async Task<Customer> Create(CustomerInput input) {
        var name = Text(input.Name, "Nombre", 120); var ci = Ci(input.Ci ?? "");
        if (!Regex.IsMatch(ci, @"^\d{4,12}(-[A-Z0-9]{1,5})?$")) throw new RuleException("CI: escribe de 4 a 12 dígitos y, si corresponde, complemento con guion (1234567-1A). Sin lugar de expedición.");
        var contact = Text(input.Contact, "Número de celular", 8);
        if (!Regex.IsMatch(contact, @"^[0-9]{8}$")) throw new RuleException("El celular debe contener exactamente 8 dígitos, sin letras, correo ni código de país.");
        if (await repository.CiExists(ci)) throw new RuleException("Ya existe un cliente con ese CI.");
        var customer = new Customer { Name = name, Ci = ci, Contact = contact };
        repository.Add(customer); await unit.Save(); return customer;
    }
}
