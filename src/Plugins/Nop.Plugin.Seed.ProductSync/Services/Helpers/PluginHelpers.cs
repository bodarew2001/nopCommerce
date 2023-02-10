using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nop.Plugin.Seed.ProductSync.Services.Helpers;

public class PluginHelpers
{
    public static async Task<byte[]> GetByteDataFromUrl(string url)
    {
        var client = new HttpClient();
        
        var result = await client.GetAsync(url);
        var content = result.Content;

        var memoryStream= new MemoryStream();
        await content.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }
}