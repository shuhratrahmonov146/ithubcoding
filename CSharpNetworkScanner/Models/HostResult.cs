namespace CSharpNetworkScanner.Models;

// One row of the scan: an address, whether it answered our probe,
// how long it took, and its host name if reverse DNS returned one.
public sealed record HostResult(
    System.Net.IPAddress Address,
    bool IsReachable,
    long RoundtripMs,
    string? HostName
);
