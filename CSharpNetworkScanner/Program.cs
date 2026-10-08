// C# Network Scanner (host discovery) — ITHubCoding
// Build it. Break it. Secure it.
//
// AUTHORIZED TESTING ONLY. Only scan networks you own or are allowed to test.
//
// Usage:
//   ./run.sh 192.168.1.0/24
//   ./run.sh 192.0.2.1-192.0.2.20
//   dotnet run -- 192.168.1.0/24

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using CSharpNetworkScanner.Models;
using CSharpNetworkScanner.Services;

const int MaxHosts = 1024;   // safety limit: a /22 at most

Console.WriteLine("C# Network Scanner");
Console.WriteLine("==================");
Console.WriteLine();

// 1) Read the network
if (args.Length < 1)
{
    Console.WriteLine("Usage: dotnet run -- <network>");
    Console.WriteLine("  e.g. dotnet run -- 192.168.1.0/24");
    Console.WriteLine("       dotnet run -- 192.0.2.1-192.0.2.20");
    return 1;
}

string target = args[0].Trim();

// 2) Validate + 3) Generate addresses
var addresses = new List<IPAddress>();
if (!TryExpandTarget(target, addresses, out string error))
{
    Console.WriteLine($"Error: {error}");
    return 1;
}

Console.WriteLine($"Target: {target}");
Console.WriteLine($"Hosts to probe: {addresses.Count}");
Console.WriteLine();
Console.WriteLine("Scanning authorized local network...");
Console.WriteLine();

// 4) Probe each host + 5) Wait for answers
var scanner = new NetworkScanner(timeoutMs: 1000, maxParallel: 64);
var printLock = new object();
var stopwatch = Stopwatch.StartNew();

IReadOnlyList<HostResult> results = await scanner.ScanAsync(addresses, result =>
{
    lock (printLock)
    {
        PrintResult(result);
    }
});

stopwatch.Stop();

// 6) Show reachable
Console.WriteLine();
Console.WriteLine("Scan complete.");
Console.WriteLine($"Hosts discovered: {results.Count}");
Console.WriteLine($"Scan time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
Console.WriteLine();
Console.WriteLine("Note: a host that does not answer is not always offline.");
Console.WriteLine("Firewalls, sleep mode, or ICMP filtering can hide a device.");
return 0;

// ---------------------------------------------------------------------------

static void PrintResult(HostResult r)
{
    string name = r.HostName is null ? "" : $"   {r.HostName}";
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("[+] ");
    Console.ResetColor();
    Console.WriteLine($"{r.Address,-15}  ONLINE  {r.RoundtripMs,6} ms{name}");
}

// Accepts CIDR (192.168.1.0/24), a range (192.0.2.1-192.0.2.20) or one address.
static bool TryExpandTarget(string text, List<IPAddress> into, out string error)
{
    error = "";
    uint start, end;

    if (text.Contains('/'))
    {
        string[] parts = text.Split('/');
        if (parts.Length != 2 || !TryParseV4(parts[0], out IPAddress network)
            || !int.TryParse(parts[1], out int prefix) || prefix < 22 || prefix > 32)
        {
            error = "Use CIDR like 192.168.1.0/24 (prefix /22 to /32).";
            return false;
        }

        uint mask = prefix == 0 ? 0 : uint.MaxValue << (32 - prefix);
        uint net = ToUInt(network) & mask;
        uint broadcast = net | ~mask;

        // Skip the network and broadcast addresses, except for /31 and /32.
        start = prefix >= 31 ? net : net + 1;
        end = prefix >= 31 ? broadcast : broadcast - 1;
    }
    else if (text.Contains('-'))
    {
        string[] parts = text.Split('-');
        if (parts.Length != 2 || !TryParseV4(parts[0].Trim(), out IPAddress a)
            || !TryParseV4(parts[1].Trim(), out IPAddress b))
        {
            error = "Use a range like 192.0.2.1-192.0.2.20.";
            return false;
        }
        start = ToUInt(a);
        end = ToUInt(b);
        if (start > end)
        {
            error = "The range start must come before the range end.";
            return false;
        }
    }
    else
    {
        if (!TryParseV4(text, out IPAddress single))
        {
            error = $"'{text}' is not a valid IPv4 address.";
            return false;
        }
        start = end = ToUInt(single);
    }

    if ((ulong)end - start + 1 > MaxHosts)
    {
        error = $"Too many hosts. The limit is {MaxHosts}.";
        return false;
    }

    for (uint a = start; a <= end && into.Count <= MaxHosts; a++)
    {
        IPAddress ip = FromUInt(a);

        // Only private ranges. Public internet addresses are refused.
        if (!IsAllowed(ip))
        {
            error = $"{ip} is not a private, loopback or link-local address. Public addresses are refused.";
            into.Clear();
            return false;
        }

        into.Add(ip);
        if (a == uint.MaxValue) break;
    }
    return true;
}

// Allow private (RFC1918), loopback, and link-local ranges only.
static bool IsAllowed(IPAddress ip)
{
    byte[] b = ip.GetAddressBytes();
    return b[0] switch
    {
        10 => true,                                     // 10.0.0.0/8
        127 => true,                                    // 127.0.0.0/8 (loopback)
        172 => b[1] is >= 16 and <= 31,                 // 172.16.0.0/12
        192 => b[1] == 168 || (b[1] == 0 && b[2] == 2), // 192.168/16  +  192.0.2/24 (TEST-NET lab)
        169 => b[1] == 254,                             // 169.254.0.0/16 (link-local)
        _ => false,
    };
}

static bool TryParseV4(string text, out IPAddress ip)
{
    ip = IPAddress.None;
    // Require four dotted parts so "80" is not read as an address.
    if (text.Count(c => c == '.') != 3) return false;
    return IPAddress.TryParse(text, out ip!) && ip.AddressFamily == AddressFamily.InterNetwork;
}

static uint ToUInt(IPAddress ip)
{
    byte[] b = ip.GetAddressBytes();
    return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
}

static IPAddress FromUInt(uint value) =>
    new(new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
