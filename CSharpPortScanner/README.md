# C# Port Scanner

A small, beginner-friendly TCP port scanner written in C# (.NET 8). It checks which TCP ports are open on a host you are allowed to test.

Video: [How I Built a Port Scanner With C#](https://www.youtube.com/watch?v=mvxcAKBo3fQ)

[ITHubCoding](https://www.youtube.com/@ithubcoding) - Build it. Break it. Secure it.

It tries to open a TCP connection to each port. If the connection succeeds before the timeout, the port is **open**.

## How it works

1. **Read input** - host and port range (from command-line arguments, or by asking you).
2. **Find the IP address** - `Dns.GetHostAddressesAsync` turns a name like `localhost` into an IPv4 address.
3. **Try each port** - `TcpClient.ConnectAsync` with a 500 ms timeout (`CancellationTokenSource`).
   - Connected -> **open**
   - Refused (`SocketException`) or no answer in time -> not open
4. **Run in parallel** - up to 100 ports at the same time (`SemaphoreSlim`), so the scan is fast but polite.
5. **Report** - prints each open port as it is found, then a summary.

Settings at the top of `Program.cs`:

```csharp
const int TimeoutMs = 500;     // how long we wait for each port
const int MaxParallel = 100;   // how many ports we check at the same time
```

## Project structure

```
CSharpPortScanner/
├── CSharpPortScanner.csproj   # project file (.NET 8)
├── Program.cs                 # the whole program: input, scan, output
└── README.md
```

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer)

Check it is installed:

```bash
dotnet --version
```

## Build and run

```bash
cd CSharpPortScanner
dotnet build
dotnet run -- 127.0.0.1 1-1024
```

## Usage and options

```bash
dotnet run -- <host> <port-range>
```

| Argument | Example | Meaning |
|---|---|---|
| host | `127.0.0.1`, `localhost` | Computer to scan |
| port range | `1-1024`, `80` | First and last port (1 to 65535) |

If you run `dotnet run` with no arguments, the program asks for the target and the range.
Press **Enter** to use the defaults (`127.0.0.1` and `1-1024`).

## Example output

This is an example. Your results will be different, because they depend on what is running on your computer.

```text
C# Port Scanner
Target: localhost (127.0.0.1)
Port range: 1-10000

Scanning...

[OPEN] 80
[OPEN] 8080

Scan complete.
Open ports: 2 (80, 8080)
Time: 0.8 s
```

Want something to find? Start a small test web server in another terminal, then scan it:

```bash
python3 -m http.server 8080      # or: python -m http.server 8080 on Windows
dotnet run -- 127.0.0.1 8000-8100
```

Error messages it handles:

```text
Invalid port range: "0-70000". Use a range like 1-1024 (ports 1 to 65535).
Could not find host: "no-such-host.invalid".
```

## Authorized use only

Only scan computers you own or have written permission to test.
Scanning other people's systems without permission can be illegal in your country.
You are responsible for how you use this code.

## Limitations

This is a learning tool. It only checks whether a TCP port accepts a connection.

- IPv4 only (it uses the first IPv4 address of the host).
- No UDP scanning.
- No stealth scanning, no service or version detection, no exploitation, no password guessing.
- A result is not absolute truth: a firewall can make an open port look closed, or hide a host.

## License

[MIT](../LICENSE)
