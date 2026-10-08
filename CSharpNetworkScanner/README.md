# C# Network Scanner - Host Discovery

A small network scanner built with **C# and .NET 8**. It discovers which devices are reachable on a network you control.

Video: [How Hackers Discover Every Device on a Network (C#)](https://youtu.be/D_ynJr_23YE)

[ITHubCoding](https://www.youtube.com/@ithubcoding) - Build it. Break it. Secure it.

## How it works

1. **Read the network** you pass in (CIDR, a range, or one address).
2. **Validate** it. Only private, loopback and link-local ranges are allowed. Public addresses are refused.
3. **Generate the addresses** to probe (up to 1024 at a time, for safety).
4. **Probe each host** with an ICMP ping. If ping is blocked, fall back to a quick TCP connection on common ports (80, 443, 22, 445, 3389).
5. **Wait for answers**, many hosts at once, limited with `SemaphoreSlim`.
6. **Show reachable hosts** with response time and host name (reverse DNS).

## Project structure

```
CSharpNetworkScanner/
├── CSharpNetworkScanner.csproj   # project file (.NET 8)
├── Program.cs                    # main program: input, validation, output
├── Models/HostResult.cs          # one result row
├── Services/NetworkScanner.cs    # ping, TCP fallback, reverse DNS, concurrency
├── build.sh                      # build helper (Linux/macOS)
├── run.sh                        # run helper (Linux/macOS)
└── README.md
```

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build and run

```bash
cd CSharpNetworkScanner
dotnet build
dotnet run -- 192.168.1.0/24
```

Or with the scripts (Linux/macOS):

```bash
chmod +x build.sh run.sh
./build.sh
./run.sh 192.168.1.0/24
```

## Usage and options

```bash
dotnet run -- <network>
```

| Example | Meaning |
|---|---|
| `192.168.1.0/24` | CIDR (prefix /22 to /32) |
| `192.0.2.1-192.0.2.20` | A range of addresses |
| `192.168.1.1` | One host |

Only private, loopback and link-local addresses are accepted:
`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`, `127.0.0.0/8`, `169.254.0.0/16`,
and `192.0.2.0/24` (a documentation/test range). Public addresses are refused.
The scan is limited to 1024 hosts at a time.

## Example output

This is an example. Your results will be different, because they depend on your own network.

```text
C# Network Scanner
==================

Target: 192.0.2.1-192.0.2.20
Hosts to probe: 20

Scanning authorized local network...

[+] 192.0.2.1        ONLINE       9 ms
[+] 192.0.2.2        ONLINE       0 ms

Scan complete.
Hosts discovered: 2
Scan time: 3.17 seconds
```

## Authorized use only

Only scan networks you own or have written permission to test.
Scanning without permission can be illegal in your country.
You are responsible for how you use this code.

## Limitations

- IPv4 only.
- Private, loopback and link-local ranges only. Public addresses are refused on purpose.
- At most 1024 hosts per scan.
- A scan result is evidence, not absolute truth: a host that does not answer is not always offline. Firewalls, sleep mode, ICMP filtering or a different network segment can hide a device.

## License

[MIT](../LICENSE)
