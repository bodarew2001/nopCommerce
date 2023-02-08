using System.Text.Json.Serialization;

namespace Nop.Plugin.Seed.ProductSync.Models;

public class ProductModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ShortDescription { get; set; }
    public string LongDescription { get; set; }
    public int Type { get; set; }
    public decimal Price { get; set; }
    public int StockValue { get; set; }
    public string Sku { get; set; }
}