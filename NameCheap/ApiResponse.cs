using System.Xml.Serialization;

namespace NameCheap;

[XmlRoot("ApiResponse", Namespace = "http://api.namecheap.com/xml.response")]
public class ApiResponse
{
    [XmlAttribute("Status")]
    public string Status { get; set; } = string.Empty;
}
