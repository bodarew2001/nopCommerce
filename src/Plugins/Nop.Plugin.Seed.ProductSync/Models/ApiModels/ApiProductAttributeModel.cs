using System.Collections.Generic;

namespace Nop.Plugin.Seed.ProductSync.Models.ApiModels;

public class ApiProductAttributeModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int AttributeControlType { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }
    public List<ApiProductAttributeValueModel> ProductAttributeValues { get; set; }
    
}