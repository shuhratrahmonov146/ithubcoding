// C# Port Scanner - an educational TCP port scanner.
// Only scan computers you own or have permission to test.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

const int TimeoutMs = 500;     // how long we wait for each port
const int MaxParallel = 100;   // how many ports we check at the same time

Console.WriteLine("C# Port Scanner");

// 1. Read the target and the port range.
//    Usage: dotnet run -- <host> <startPort>-<endPort>
//    Example: dotnet run -- 127.0.0.1 1-1024
string host = args.Length > 0 ? args[0] : Ask("Target (host or IP)", "127.0.0.1");
string range = args.Length > 1 ? args[1] : Ask("Port range", "1-1024");

if (!TryParseRange(range, out int startPort, out int endPort))
{
    Console.WriteLine($"Invalid port range: \"{range}\". Use a range like 1-1024 (ports 1 to 65535).");
    return 1;
}

// 2. Turn the host name into an IP address.
IPAddress address;
try
{
    address = IPAddress.TryParse(host, out var ip)
        ? ip
        : (await Dns.GetHostAddressesAsync(host)).First(a => a.AddressFamily == AddressFamily.InterNetwork);
}
catch (Exception)
{
    Console.WriteLine($"Could not find host: \"{host}\".");
    return 1;
}

Console.WriteLine($"Target: {host} ({address})");
Console.WriteLine($"Port range: {startPort}-{endPort}");
Console.WriteLine();
Console.WriteLine("Scanning...");
Console.WriteLine();

// 3. Check the ports in parallel, but not too many at once.
var openPorts = new ConcurrentBag<int>();
using var limiter = new SemaphoreSlim(MaxParallel);
var timer = Stopwatch.StartNew();

var tasks = Enumerable.Range(startPort, endPort - startPort + 1).Select(async port =>
{
    await limiter.WaitAsync();
    try
    {
        if (await IsPortOpenAsync(address, port, TimeoutMs))
        {
            openPorts.Add(port);
            Console.WriteLine($"[OPEN] {port}");
        }
    }
    finally
    {
        limiter.Release();
    }
});

await Task.WhenAll(tasks);
timer.Stop();

// 4. Show the result.
Console.WriteLine();
Console.WriteLine("Scan complete.");
Console.WriteLine($"Open ports: {openPorts.Count}" +
                  (openPorts.IsEmpty ? "" : $" ({string.Join(", ", openPorts.OrderBy(p => p))})"));
Console.WriteLine($"Time: {timer.Elapsed.TotalSeconds:F1} s");
return 0;

// Try to open a TCP connection. If it works before the timeout, the port is open.
static async Task<bool> IsPortOpenAsync(IPAddress address, int port, int timeoutMs)
{
    using var client = new TcpClient();
    using var cts = new CancellationTokenSource(timeoutMs);
    try
    {
        await client.ConnectAsync(address, port, cts.Token);
        return true;      // connected: something is listening on this port
    }
    catch (OperationCanceledException)
    {
        return false;     // no answer in time (filtered or slow)
    }
    catch (SocketException)
    {
        return false;     // connection refused: the port is closed
    }
}

// Parse "20-80" (or a single port like "80") into two numbers.
static bool TryParseRange(string text, out int start, out int end)
{
    start = end = 0;
    var parts = text.Split('-', StringSplitOptions.TrimEntries);
    if (parts.Length is < 1 or > 2) return false;
    if (!int.TryParse(parts[0], out start)) return false;
    end = start;
    if (parts.Length == 2 && !int.TryParse(parts[1], out end)) return false;
    return start >= 1 && end <= 65535 && start <= end;
}

// Ask the user for a value, with a default if they just press Enter.
static string Ask(string question, string defaultValue)
{
    Console.Write($"{question} [{defaultValue}]: ");
    string? answer = Console.ReadLine();
    return string.IsNullOrWhiteSpace(answer) ? defaultValue : answer.Trim();
}
