using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;

namespace NameCheap;

internal class Program
{
    private static readonly HttpClient client = new ();
    private static string ApiPrefix => "https://api.namecheap.com/xml.response";

    private static async Task Main(string[] args)
    {
        #region Argument Processing
        
        if (ArgumentsAreValid(args) == false)
            Environment.Exit(1);
        
        var command = args[0].ToLower();
        var name = args[2].ToLower();
        var txtValue = args[3];
        
        #endregion
        
        #region Configuration Processing
        
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        var apiSettings = new NameCheapApiSettings
        {
            ApiKey = config.GetValue<string>("NameCheap:ApiKey"),
            ApiUserName = config.GetValue<string>("NameCheap:ApiUserName"),
            UserName = config.GetValue<string>("NameCheap:UserName"),
            ClientIp = config.GetValue<string>("NameCheap:ClientIp"),
            HostName = args[1].ToLower()
        };

        if (string.IsNullOrEmpty(apiSettings.ApiKey) || string.IsNullOrEmpty(apiSettings.ApiUserName) ||
            string.IsNullOrEmpty(apiSettings.UserName) || string.IsNullOrEmpty(apiSettings.ClientIp))
        {
            Console.WriteLine("Error: appsettings.json is missing one or more property values");
            Environment.Exit(1);
        }
        
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
        client.DefaultRequestHeaders.Add($"User-Agent", $"Argentini.NameCheap/{typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0"}");

        #endregion
        
        #region Retrieve Existing DNS Records
        
        var records = await RetrieveAllRecords(apiSettings);
        
        #endregion
        
        #region Process Command
        
        switch (command)
        {
            case "create":
            {
                RemoveRecord(records, name, txtValue);
                
                var txtRecord = new Record
                {
                    HostName = name,
                    Address = txtValue
                };

                records.Add(txtRecord);
                break;
            }

            case "delete":

                RemoveRecord(records, name, txtValue);
                break;
        }
        
        #endregion

        #region Build and Send Request to NameCheap API
        
        if (records.Count == 0)
        {
            Console.WriteLine("Error: no records to publish");
            Environment.Exit(1);
        }
        
        var cl = new StringBuilder($"{ApiPrefix}?Command=namecheap.domains.dns.setHosts&ApiUser={apiSettings.ApiUserName}&ApiKey={apiSettings.ApiKey}&UserName={apiSettings.UserName}&ClientIp={apiSettings.ClientIp}&SLD={apiSettings.Sld}&TLD={apiSettings.Tld}");
        var index = 1;

        foreach (var record in records)
        {
            cl.Append($"&HostName{index}={record.HostName.Replace(" ", "%20").Replace("+", "%2B")}");
            cl.Append($"&RecordType{index}={record.RecordType}");
            cl.Append($"&Address{index}={record.Address.Replace(" ", "%20").Replace("+", "%2B")}");
            cl.Append($"&MXPref{index}={record.MxPref}");
            cl.Append($"&TTL{index}={record.Ttl}");

            index++;
        }

        var result = await client.GetStringAsync(cl.ToString());
        var document = new XmlDocument();

        document.LoadXml(result);
        
        var m = new XmlNamespaceManager(document.NameTable);
        m.AddNamespace("ns", "http://api.namecheap.com/xml.response");

        if (ResultStatusOk(document, m) == false)
        {
            Environment.Exit(1);
        }

        Console.WriteLine("SUCCESS");
        
        Environment.Exit(0);
        
        #endregion
    }

    /// <summary>
    /// Performs basic argument validation; number of arguments, format of some.
    /// </summary>
    /// <param name="args"></param>
    private static bool ArgumentsAreValid(IReadOnlyList<string> args)
    {
        if (args.Count != 4)
        {
            Console.WriteLine("Usage:   Argentini.NameCheap [create|delete] [hostname] [name] [value]");
            Console.WriteLine("Example: Argentini.NameCheap create example.com * testrecord=yaddayadda");
            Console.WriteLine("         Argentini.NameCheap create example.com my.api mykey=yaddayadda");
            Console.WriteLine("         Argentini.NameCheap create example.com my.txt \"val1=yadda; val2=yadda\"");
            return false;
        }
        
        var command = args[0].ToLower();

        if (command != "create" && command != "delete")
        {
            Console.WriteLine("Error: supported commands are 'create' and 'delete'");
            return false;
        }

        var hostName = args[1].ToLower();
        
        if (hostName.Contains('.') == false || hostName.LastIndexOf('.') >= hostName.Length - 1)
        {
            Console.WriteLine("Error: hostname invalid (should be root domain, e.g. example.com)");
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// NameCheap does not add or delete individual records. Requests to sethosts replace all records (Yes, very dangerous!)
    /// That is why we first load all records.
    /// Will abort on serious communication errors.
    /// </summary>
    /// <param name="apiSettings"></param>
    /// <returns>List of existing DNS records</returns>
    private static async Task<List<Record>> RetrieveAllRecords(NameCheapApiSettings apiSettings)
    {
        var records = new List<Record>();
        var document = new XmlDocument();
        var result = await client.GetStringAsync(
            $"{ApiPrefix}?Command=namecheap.domains.dns.getHosts&ApiUser={apiSettings.ApiUserName}&ApiKey={apiSettings.ApiKey}&UserName={apiSettings.UserName}&ClientIp={apiSettings.ClientIp}&SLD={apiSettings.Sld}&TLD={apiSettings.Tld}");

        document.LoadXml(result);

        var m = new XmlNamespaceManager(document.NameTable);
        m.AddNamespace("ns", "http://api.namecheap.com/xml.response");

        if (ResultStatusOk(document, m) == false)
        {
            Environment.Exit(1);
        }
        
        var nodes = document.SelectNodes("ns:ApiResponse/ns:CommandResponse/ns:DomainDNSGetHostsResult/ns:host", m);

        if (nodes == null)
        {
            Console.WriteLine("Error: could not retrieve existing hosts");
            Environment.Exit(1);
        }
        
        var hostSerializer = new XmlSerializer(typeof(Host));
        
        foreach (XmlNode node in nodes)
        {
            var host = (Host?)hostSerializer.Deserialize(new StringReader(node.OuterXml));

            if (host != null && host.HostId != 0)
            {
                var record = new Record
                {
                    RecordType = host.Type,
                    HostName = host.Name,
                    Address = host.Address,
                    Ttl = host.Ttl,
                    MxPref = host.MxPref
                };
                
                records.Add(record);
            }

            else
            {
                Console.WriteLine("Error: could not deserialize one or more host nodes");
                Environment.Exit(1);
            }
        }

        return records;
    }

    /// <summary>
    /// Remove a record if it already exists using name and value.
    /// </summary>
    /// <param name="records"></param>
    /// <param name="name"></param>
    /// <param name="txtValue"></param>
    private static void RemoveRecord(ICollection<Record> records, string name, string txtValue)
    {
        foreach (var record in records.ToList())
        {
            if (record.HostName.Equals(name, StringComparison.InvariantCultureIgnoreCase) &&
                record.Address.Equals(txtValue, StringComparison.InvariantCultureIgnoreCase))
            {
                records.Remove(record);
                break;
            }
        }
    }
    
    /// <summary>
    /// Check the ApiResponse for OK result.
    /// Will abort if one or more parameters are null.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="m"></param>
    /// <returns>True if OK, false if not</returns>
    private static bool ResultStatusOk(XmlNode? document, XmlNamespaceManager? m)
    {
        if (document == null || m == null)
        {
            Console.WriteLine("Error: Could not parse XML result");
            Environment.Exit(1);
        }
        
        var apiResponseSerializer = new XmlSerializer(typeof(ApiResponse));
        var apiResponseNode = (ApiResponse?)apiResponseSerializer.Deserialize(new StringReader(document.SelectSingleNode("ns:ApiResponse", m)?.OuterXml ?? string.Empty));

        if (apiResponseNode == null || apiResponseNode.Status.Equals("OK", StringComparison.InvariantCultureIgnoreCase) == false)
        {
            Console.WriteLine("Error: Request for existing records failed:");

            if (apiResponseNode != null)
            {
                var errorNodes = document.SelectNodes("ns:ApiResponse/ns:Errors", m);

                if (errorNodes != null)
                {
                    foreach (XmlNode error in errorNodes)
                    {
                        Console.WriteLine(error.InnerText);
                    }
                }
            }

            return false;
        }

        return true;
    }
}
