using System.Xml.Serialization;

namespace Argentini.NameCheap;

[XmlRoot("host", Namespace = "http://api.namecheap.com/xml.response")]
public class Host
{
    [XmlAttribute("HostId")]
    public long HostId { get; set; }
    [XmlAttribute("Name")]
    public string Name { get; set; } = string.Empty;
    [XmlAttribute("Type")]
    public string Type { get; set; } = string.Empty;
    [XmlAttribute("Address")]
    public string Address { get; set; } = string.Empty;
    [XmlAttribute("MXPref")]
    public int MxPref { get; set; }
    [XmlAttribute("TTL")]
    public int Ttl { get; set; } = 1799;
    [XmlAttribute("AssociatedAppTitle")]
    public string AssociatedAppTitle { get; set; } = string.Empty;
    [XmlAttribute("FriendlyName")]
    public string FriendlyName { get; set; } = string.Empty;
    [XmlAttribute("IsActive")]
    public bool IsActive { get; set; } = true;
    [XmlAttribute("IsDdnsEnabled")]
    public bool IsDdnsEnabled { get; set; }
}