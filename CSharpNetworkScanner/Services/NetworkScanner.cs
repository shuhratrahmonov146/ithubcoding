using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using CSharpNetworkScanner.Models;

namespace CSharpNetworkScanner.Services;

// Discovers which hosts are reachable: ICMP ping first, TCP connect fallback second.
public sealed class NetworkScanner
{
    // Common ports used for the TCP "ping" fallback.
    private static readonly int[] CommonPorts = { 80, 443, 22, 445, 3389 };

    private readonly int _timeoutMs;
    private readonly int _maxParallel;
    private readonly bool _resolveNames;

    public NetworkScanner(int timeoutMs = 1000, int maxParallel = 64, bool resolveNames = true)
    {
        _timeoutMs = timeoutMs;
        _maxParallel = maxParallel;
        _resolveNames = resolveNames;
    }

    // Probe every address in the list, at most _maxParallel at a time.
    // onFound is called as soon as a reachable host is confirmed, so the
    // UI can print results live instead of waiting for the whole scan.
    public async Task<IReadOnlyList<HostResult>> ScanAsync(
        IReadOnlyList<IPAddress> addresses,
        Action<HostResult>? onFound = null,
        CancellationToken token = default)
    {
        using var limiter = new SemaphoreSlim(_maxParallel);
        var found = new System.Collections.Concurrent.ConcurrentBag<HostResult>();

        var tasks = addresses.Select(async address =>
        {
            await limiter.WaitAsync(token);
            try
            {
                var result = await ProbeHostAsync(address, token);
                if (result.IsReachable)
                {
                    found.Add(result);
                    onFound?.Invoke(result);
                }
            }
            finally
            {
                limiter.Release();
            }
        });

        await Task.WhenAll(tasks);

        return found.OrderBy(r => r.Address, new IPv4Comparer()).ToList();
    }

    // Ping first; if ping is blocked, try TCP. Then look up the name.
    private async Task<HostResult> ProbeHostAsync(IPAddress address, CancellationToken token)
    {
        var (reachable, rttMs) = await TryPingAsync(address);

        if (!reachable)
        {
            (reachable, rttMs) = await TryTcpAsync(address, token);
        }

        string? name = null;
        if (reachable && _resolveNames)
        {
            name = await TryReverseDnsAsync(address);
        }

        return new HostResult(address, reachable, rttMs, name);
    }

    // Standard ICMP echo request. Success means the host answered our probe.
    private async Task<(bool reachable, long rttMs)> TryPingAsync(IPAddress address)
    {
        try
        {
            using var ping = new Ping();
            PingReply reply = await ping.SendPingAsync(address, _timeoutMs);
            return (reply.Status == IPStatus.Success, reply.RoundtripTime);
        }
        catch (PingException)
        {
            return (false, 0);   // ICMP not available here; caller will try TCP
        }
    }

    // TCP "ping": if any common port accepts a connection, the host is up.
    // Useful when a firewall drops ICMP but still runs services.
    private async Task<(bool reachable, long rttMs)> TryTcpAsync(IPAddress address, CancellationToken token)
    {
        foreach (int port in CommonPorts)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(_timeoutMs);
            try
            {
                await client.ConnectAsync(address, port, cts.Token);
                long ms = (long)System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                return (true, ms);
            }
            catch (OperationCanceledException) { }   // timed out on this port
            catch (SocketException) { }              // refused / unreachable on this port
        }
        return (false, 0);
    }

    // Ask DNS for the name behind the address. Many home devices have none.
    private static async Task<string?> TryReverseDnsAsync(IPAddress address)
    {
        try
        {
            IPHostEntry entry = await Dns.GetHostEntryAsync(address);
            return string.IsNullOrWhiteSpace(entry.HostName) ? null : entry.HostName;
        }
        catch (SocketException)
        {
            return null;   // no PTR record
        }
    }

    // Sort IPv4 addresses in natural numeric order (.2 before .10).
    private sealed class IPv4Comparer : IComparer<IPAddress>
    {
        public int Compare(IPAddress? a, IPAddress? b)
        {
            if (a is null || b is null) return 0;
            byte[] x = a.GetAddressBytes();
            byte[] y = b.GetAddressBytes();
            for (int i = 0; i < 4; i++)
            {
                int diff = x[i].CompareTo(y[i]);
                if (diff != 0) return diff;
            }
            return 0;
        }
    }
}
