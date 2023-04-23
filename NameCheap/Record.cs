namespace NameCheap;

public class Record
{
    public string HostName { get; set; } = string.Empty;
    public string RecordType { get; set; } = "TXT";
    public string Address { get; set; } = string.Empty;
    public int MxPref { get; set; }
    public int Ttl { get; set; } = 1799;

}