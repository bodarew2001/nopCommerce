using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Seed.ProductSync.Models;

public class ConfigurationModel
{
    public string InfigoUrl { get; set; }
    public string ApiToken { get; set; }
    public bool Enabled { get; set; }
    public bool SyncImages { get; set; }
    public bool DeleteProduct { get; set; }
}