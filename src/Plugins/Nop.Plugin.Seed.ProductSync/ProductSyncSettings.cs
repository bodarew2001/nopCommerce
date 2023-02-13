using Nop.Core.Configuration;

namespace Nop.Plugin.Seed.ProductSync;

public class ProductSyncSettings:ISettings
{
    public string InfigoUrl { get; set; }
    public string ApiToken { get; set; }
    public bool Enabled { get; set; }
    public bool SyncImages { get; set; }
    public bool DeleteProduct { get; set; }
}