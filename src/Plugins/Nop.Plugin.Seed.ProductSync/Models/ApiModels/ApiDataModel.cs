using System.Collections.Generic;

namespace Nop.Plugin.Seed.ProductSync.Models.ApiModels;

public class ApiDataModel
{
    public int Id { get; set; }
    public int StockValue { get; set; }
    public int Type { get; set; }
    public decimal Price { get; set; }
    public string Name { get; set; }
    public string ShortDescription { get; set; }
    public string LongDescription { get; set; }
    public string Sku { get; set; }
    public List<string> ThumbnailUrls { get; set; }
    public List<ApiProductAttributeModel> ProductAttributes { get; set; }
    public List<string> Tags { get; set; }
    public List<ApiSpecificationAttributeModel> SpecificationAttributes { get; set; }
}