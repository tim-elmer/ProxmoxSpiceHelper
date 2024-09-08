using System.Text;

namespace ProxmoxSpiceHelper;

public class Proxmox(Configuration configuration)
{
    private readonly HttpClient _client = new();
    private string ApiRoot => $"https://{configuration.Host}:{configuration.Port}/api2";

    public async Task<string> GetSpiceCommand()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{ApiRoot}/spiceconfig/nodes/{configuration.Node}/qemu/{configuration.VmId}/spiceproxy");

        request.Content = new ByteArrayContent(Encoding.ASCII.GetBytes($"proxy={configuration.Host}"));
        request.Headers.TryAddWithoutValidation("Authorization",
            $"PVEAPIToken={configuration.TokenId}={configuration.TokenSecret}");

        var response = await _client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}