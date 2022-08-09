namespace NameCheap;

public class NameCheapApiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUserName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    
    public string Sld => HostName[..HostName.LastIndexOf('.')];
    public string Tld => HostName[(HostName.LastIndexOf('.') + 1)..];
}