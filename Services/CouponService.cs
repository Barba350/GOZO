using static GOZO.Rules;
namespace GOZO;
public class CouponService(OperationsRepository repo, IUnitOfWork unit) {
    public async Task Apply(Sale sale, string? raw) {
        if (string.IsNullOrWhiteSpace(raw)) return;
        var code=Key(raw);
        if (code.Length>40) throw new RuleException("Código de cupón demasiado largo.");
        var coupon=await repo.Coupon(code) ?? throw new RuleException("Cupón inexistente.");
        if (!coupon.Active || coupon.Start>Now() || coupon.End<Now()) throw new RuleException("Cupón inactivo o fuera de vigencia.");
        if (coupon.CustomerId.HasValue && coupon.CustomerId != sale.CustomerId) throw new RuleException("Este cupón pertenece a otro cliente. Selecciona su CI.");
        if (await repo.Redeemed(coupon.Id)) throw new RuleException("Cupón ya canjeado o reservado en un pedido.");
        sale.CouponProductId=coupon.GiftProductId;sale.CouponQuantity=coupon.GiftQuantity;
        sale.CouponId=coupon.Id; sale.CouponCode=coupon.Code; sale.CouponType=coupon.Type; sale.CouponValue=coupon.Value;
        Reprice(sale);
        if(sale.Discount<=0)throw new RuleException("El cupón no genera descuento para este importe; no se ha canjeado.");
        sale.PromotionNotice=coupon.Type=="PRODUCTO" ? $"Cupón de regalo: {coupon.GiftQuantity} unidad(es) del producto elegido. Las unidades extra se cobran. No se acumula con promociones." : "Cupón de un solo uso aplicado al total. No se acumula con promociones por unidad.";
    }
    public static void Reprice(Sale sale) {
        sale.Subtotal=sale.Lines.Sum(x=>x.UnitPrice*x.Quantity);
        if(sale.CouponId.HasValue&&sale.CouponType=="PRODUCTO") {
            var gift=sale.Lines.FirstOrDefault(x=>x.ProductId==sale.CouponProductId);
            if(gift==null||gift.Quantity<sale.CouponQuantity||sale.CouponQuantity<1)throw new RuleException($"Agrega al ticket al menos {sale.CouponQuantity} unidad(es) del producto de regalo. El cupón aún no se canjeó.");
            foreach(var line in sale.Lines) {line.Discount=0;line.Total=line.UnitPrice*line.Quantity;line.Promotion="";}
            gift.Discount=gift.UnitPrice*sale.CouponQuantity;gift.Total-=gift.Discount;gift.Promotion="Regalo · cupón "+sale.CouponCode;
        }
        else if (sale.CouponId.HasValue) {
            var discount=Math.Min(sale.Subtotal, sale.CouponType=="PORCENTAJE" ? (long)decimal.Round(sale.Subtotal*sale.CouponValue/100m,0,MidpointRounding.AwayFromZero) : Cents(sale.CouponValue));
            long remaining=discount;
            for(int i=0;i<sale.Lines.Count;i++) {
                var l=sale.Lines[i]; var gross=l.UnitPrice*l.Quantity;
                l.Discount=Math.Min(gross,(long)decimal.Floor((decimal)discount*gross/sale.Subtotal));
                remaining-=l.Discount; l.Total=gross-l.Discount; l.Promotion="Cupón " + sale.CouponCode;
            }
            foreach(var line in sale.Lines) {
                var extra=Math.Min(remaining,line.Total);line.Discount+=extra;line.Total-=extra;remaining-=extra;
            }
        }
        sale.Discount=sale.Lines.Sum(x=>x.Discount); sale.Total=sale.Subtotal-sale.Discount;
    }
    public async Task Lock(string? raw) { if(!string.IsNullOrWhiteSpace(raw)) await unit.Lock("coupon:"+Key(raw)); }
    public async Task Reserve(Sale quote, int? saleId, int? orderId) {
        if(quote.CouponId is not int id) return;
        if(await repo.Redeemed(id)) throw new RuleException("Otro cobro utilizó este cupón. Actualiza el ticket.");
        repo.AddRedemption(new(){CouponId=id,SaleId=saleId,OrderId=orderId,CreatedAt=Now()});
        await unit.Save();
    }
}
