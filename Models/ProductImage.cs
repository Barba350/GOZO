namespace GOZO;
public class ProductImage
{
    public int ProductId { get; set; }
    public byte[] Bytes { get; set; } = [];
    public string Version { get; set; } = Guid.NewGuid().ToString("N");
}
