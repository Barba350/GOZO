using System.Text.Json;
using static GOZO.Rules;
namespace GOZO;
public class OperationsService(OperationsRepository repo, BranchContext branch, IUnitOfWork unit) {
    public async Task<object> Branches()=>new {current=branch.Id,branches=await repo.Branches()};
    public async Task SelectBranch(int id) { if(!await repo.BranchExists(id))throw new RuleException("Sucursal inválida."); }
    public async Task<Branch> RenameBranch(int id,string name) {
        var item=await repo.Branch(id)??throw new RuleException("Sucursal inexistente.");item.Name=Text(name,"Nombre",100);await unit.Save();return item;
    }
    public Task<object> Dashboard()=>repo.Dashboard();
    public Task<object> Summary()=>repo.Summary(Now().Date,Now().Date.AddDays(1));
    public Task<object> Coupons()=>repo.Coupons();
    public async Task<Coupon> DisableCoupon(int id) {
        await using var tx=await unit.BeginTransaction();
        var item=await repo.CouponById(id)??throw new RuleException("Cupón inexistente.");await unit.Lock("coupon:"+item.Code);
        item.Active=false;item.Revision=Guid.NewGuid().ToString();await unit.Save();await tx.Commit();return item;
    }
    public async Task<object> Sale(int id)=>View(await repo.Sale(id)??throw new RuleException("Venta inexistente en esta sucursal."));
    public async Task<List<SaleCorrection>> Corrections(int id) {
        if(await repo.Sale(id)==null)throw new RuleException("Venta inexistente en esta sucursal.");return await repo.Corrections(id);
    }
    public static long Amount(decimal amount) {
        if(amount<0||amount>1000000||decimal.Round(amount,2)!=amount)throw new RuleException("Importe entre Bs 0 y Bs 1.000.000, con hasta dos decimales.");return Cents(amount);
    }
    private static void VerifyBreakdown(List<Denomination>? breakdown,long amount) {
        if(breakdown!=null&&breakdown.Count>0&&Count(breakdown)!=amount)throw new RuleException("El desglose no coincide con el importe ingresado. Corrige el conteo o quita el desglose opcional.");
    }
    public static readonly int[] Denominations = [20000,10000,5000,2000,1000,500,200,100,50,20,10];
    public static long Count(List<Denomination>? input) {
        if(input==null || input.Count>Denominations.Length || input.Select(x=>x.Cents).Distinct().Count()!=input.Count || input.Any(x=>!Denominations.Contains(x.Cents)||x.Count<0||x.Count>100000))
            throw new RuleException("Desglose inválido. Indica cantidades enteras por billete o moneda.");
        long total=input.Sum(x=>(long)x.Cents*x.Count);
        if(total>100000000) throw new RuleException("El efectivo supera el límite de Bs 1.000.000.");
        return total;
    }
    public async Task<CashSession> RequireCash() {
        if(!await repo.ValidBranch()) throw new RuleException("Selecciona una sucursal activa.");
        if(branch.ActorId<=0)throw new RuleException("Vuelve a iniciar sesión.");
        return await repo.OpenCash() ?? throw new RuleException("Abre tu caja en esta sucursal antes de cobrar.");
    }
    public async Task<object> CashDetail(int id) {
        var cash=await repo.Cash(id)??throw new RuleException("Caja inexistente o sin acceso.");
        var sales=await repo.CashSales(id);
        return new {cash,cashSales=sales.Where(x=>x.Payment=="EFECTIVO").Sum(x=>x.Total),qrSales=sales.Where(x=>x.Payment=="QR").Sum(x=>x.Total),expected=cash.OpeningAmount+sales.Where(x=>x.Payment=="EFECTIVO").Sum(x=>x.Total)};
    }
    public async Task<object> Status() {
        if(!await repo.ValidBranch()) throw new RuleException("Sucursal inválida.");
        var cash=await repo.OpenCash(); var sales=cash==null?new List<Sale>():await repo.CashSales(cash.Id);
        return new { branchId=branch.Id, cash, cashSales=sales.Where(x=>x.Payment=="EFECTIVO").Sum(x=>x.Total), qrSales=sales.Where(x=>x.Payment=="QR").Sum(x=>x.Total), expected=(cash?.OpeningAmount??0)+sales.Where(x=>x.Payment=="EFECTIVO").Sum(x=>x.Total),
            openBoxes=branch.Owner?await repo.OpenBoxes():new List<CashSession>(), history=await repo.CashHistory() };
    }
    public async Task<CashSession> Open(OpenCashInput input) {
        var total=Amount(input.OpeningAmount);
        VerifyBreakdown(input.Denominations,total);
        await using var tx=await unit.BeginTransaction();
        if(!await repo.ValidBranch()) throw new RuleException("Sucursal inválida.");
        await unit.Lock("cashier:"+branch.ActorId);
        if(branch.ActorId<=0)throw new RuleException("Vuelve a iniciar sesión.");
        if(await repo.HasActiveCash()) throw new RuleException("Ya tienes una caja abierta. Ciérrala antes de abrir otra, incluso en otra sucursal.");
        var cash=new CashSession {BranchId=branch.Id,OpenedById=branch.ActorId,OpenedAt=Now(),OpenedBy=branch.Actor,OpeningAmount=total,OpeningDenominations=JsonSerializer.Serialize(input.Denominations??new List<Denomination>())};
        repo.AddCash(cash); await unit.Save(); await tx.Commit(); return cash;
    }
    public async Task<CashSession> Close(int id,CloseCashInput input) {
        var counted=Amount(input.CountedCash);var qr=Amount(input.CountedQr);
        VerifyBreakdown(input.Denominations,counted);
        var notes=(input.Notes??"").Trim(); if(notes.Length>1000)throw new RuleException("Observaciones: máximo 1000 caracteres.");
        await using var tx=await unit.BeginTransaction();
        var cash=await repo.Cash(id)??throw new RuleException("Caja inexistente o sin acceso.");
        if(cash.ClosedAt!=null)throw new RuleException("La caja ya está cerrada.");
        if(cash.Id!=id||cash.Revision!=input.Revision)throw new RuleException("La caja cambió. Actualiza la pantalla.");
        if(cash.OpenedById!=branch.ActorId&&!branch.Owner)throw new RuleException("Solo quien abrió la caja o el dueño puede cerrarla.");
        var sales=await repo.CashSales(cash.Id);
        var expected=cash.OpeningAmount+sales.Where(x=>x.Payment=="EFECTIVO").Sum(x=>x.Total);
        var qrExpected=sales.Where(x=>x.Payment=="QR").Sum(x=>x.Total);
        if((counted!=expected||qr!=qrExpected)&&notes.Length<10)throw new RuleException("Explica la diferencia de efectivo o QR (mínimo 10 caracteres).");
        cash.ClosedAt=Now();cash.ClosedBy=branch.Actor;cash.CountedAmount=counted;cash.ExpectedAmount=expected;cash.Difference=counted-expected;cash.QrTotal=sales.Where(x=>x.Payment=="QR").Sum(x=>x.Total);
        cash.CountedQr=qr;cash.QrDifference=qr-qrExpected;
        cash.ClosingDenominations=JsonSerializer.Serialize(input.Denominations??new List<Denomination>());cash.Notes=notes;cash.Revision=Guid.NewGuid().ToString();
        await unit.Save();await tx.Commit();return cash;
    }
    public async Task<Coupon> CreateCoupon(CouponInput input) {
        var code=Key(Text(input.Code,"Código",40));
        if(!System.Text.RegularExpressions.Regex.IsMatch(code,@"^[A-Z0-9-]{4,40}$"))throw new RuleException("Código: de 4 a 40 letras, números o guiones.");
        if(input.Type is not ("MONTO" or "PORCENTAJE" or "PRODUCTO"))throw new RuleException("Tipo de cupón inválido.");
        if(input.Type=="PRODUCTO") {
            if(!input.GiftProductId.HasValue||!await repo.ActiveProductExists(input.GiftProductId.Value)||input.GiftQuantity<1||input.GiftQuantity>999)throw new RuleException("Selecciona un producto activo y de 1 a 999 unidades de regalo.");
        } else if(input.Value<=0||input.Value>1000000||(input.Type=="PORCENTAJE"&&input.Value>100)||decimal.Round(input.Value,2)!=input.Value)throw new RuleException("Valor de cupón inválido.");
        if(input.Start>=input.End)throw new RuleException("La vigencia está invertida.");
        if(input.CustomerId.HasValue&&!await repo.CustomerExists(input.CustomerId.Value))throw new RuleException("Cliente inexistente.");
        var coupon=new Coupon {Code=code,Description=Text(input.Description,"Descripción",500),Type=input.Type,Value=input.Type=="PRODUCTO"?0:input.Value,GiftProductId=input.Type=="PRODUCTO"?input.GiftProductId:null,GiftQuantity=input.Type=="PRODUCTO"?input.GiftQuantity:1,Start=DateTime.SpecifyKind(input.Start,DateTimeKind.Unspecified),End=DateTime.SpecifyKind(input.End,DateTimeKind.Unspecified),CustomerId=input.CustomerId};
        repo.AddCoupon(coupon);await unit.Save();return coupon;
    }
    public async Task<object> Correct(int id,CorrectSaleInput input) {
        if(!branch.Owner)throw new RuleException("Solo el dueño puede corregir ventas.");
        var reason=Text(input.Reason,"Justificación",1000);if(reason.Length<10)throw new RuleException("Explica la corrección con al menos 10 caracteres.");
        if(input.Payment is not ("EFECTIVO" or "QR" or "CANJE"))throw new RuleException("Selecciona Efectivo o QR.");
        await using var tx=await unit.BeginTransaction();
        var sale=await repo.Sale(id)??throw new RuleException("Venta inexistente en esta sucursal.");
        var cash=sale.CashSessionId.HasValue?await repo.Cash(sale.CashSessionId.Value):null;
        if(cash==null||cash.ClosedAt!=null)throw new RuleException("La caja de esta venta está cerrada. Su cierre e historial no pueden modificarse.");
        if(input.Revision!=sale.Revision)throw new RuleException("La venta fue modificada. Actualiza antes de corregir.");
        if(input.Items==null||input.Items.Count!=sale.Lines.Count||input.Items.Select(x=>x.ProductId).Distinct().Count()!=input.Items.Count||input.Items.Any(x=>x.Quantity<0||x.Quantity>999)||!input.Items.Any(x=>x.Quantity>0)||input.Items.Any(x=>!sale.Lines.Any(l=>l.ProductId==x.ProductId)))
            throw new RuleException("Revisa las cantidades originales; usa cero para retirar una línea y conserva al menos un producto.");
        var before=JsonSerializer.Serialize(View(sale));
        foreach(var line in sale.Lines.ToList()) {
            int qty=input.Items.Single(x=>x.ProductId==line.ProductId).Quantity;
            if(qty==0){sale.Lines.Remove(line);repo.RemoveLine(line);continue;}
            if(!sale.CouponId.HasValue)line.Discount=(line.Discount/line.Quantity)*qty;
            line.Quantity=qty;line.Total=line.UnitPrice*qty-line.Discount;
        }
        CouponService.Reprice(sale);
        if(sale.Total>0&&input.Payment=="CANJE")throw new RuleException("La venta tiene saldo. Selecciona Efectivo o QR.");
        sale.Payment=sale.Total==0&&sale.CouponId.HasValue?"CANJE":input.Payment;sale.Revision=Guid.NewGuid().ToString();
        // Si nació de un pedido, mantener también su detalle entregado consistente.
        var order=await repo.OrderForSale(sale.Id);
        if(order!=null) {
            repo.ReplaceOrderLines(order,sale.Lines.Select(x=>new OrderLine{ProductId=x.ProductId,Name=x.Name,UnitPrice=x.UnitPrice,Quantity=x.Quantity,Discount=x.Discount,Total=x.Total,Promotion=x.Promotion}).ToList());
            order.Subtotal=sale.Subtotal;order.Discount=sale.Discount;order.Total=sale.Total;order.Revision=Guid.NewGuid().ToString();
        }
        repo.AddCorrection(new(){SaleId=sale.Id,CreatedUtc=DateTime.SpecifyKind(DateTime.UtcNow,DateTimeKind.Unspecified),Actor=branch.Actor,Reason=reason,BeforeJson=before,AfterJson=JsonSerializer.Serialize(View(sale))});
        await unit.Save();await tx.Commit();return View(sale);
    }
}
