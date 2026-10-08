# ITHubCoding

**Build it. Break it. Secure it.**

Source code for the videos on the [ITHubCoding YouTube channel](https://www.youtube.com/@ithubcoding).

Every video has its own folder. Each folder is a complete project: you can build it, run it, and follow along with the video.

---

## Projects

<!-- Projects table: one row per folder. Update it when a new video project is added. -->

| # | Project | Description | Tech | Video | Code |
|---|---|---|---|---|---|
| 01 | C# Port Scanner | Finds which TCP ports are open on a host you are allowed to test. | C# / .NET 8 | [Watch](https://www.youtube.com/watch?v=mvxcAKBo3fQ) | [CSharpPortScanner](./CSharpPortScanner) |
| 02 | C# Network Scanner | Discovers which devices are reachable on a network you control. | C# / .NET 8 | [Watch](https://youtu.be/D_ynJr_23YE) | [CSharpNetworkScanner](./CSharpNetworkScanner) |

New projects are added with each new video.

---

## Series: C# Cybersecurity Tools

In this series we build real security tools in C# and .NET, step by step.
Each episode answers one question:

1. **Port Scanner:** What ports are open on this host?
2. **Network Scanner:** Which devices are reachable on this network?

---

## How to run a project

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Clone this repository:

   ```bash
   git clone https://github.com/shuhratrahmonov146/ithubcoding.git
   cd ithubcoding
   ```

3. Go into the folder of the project you want:

   ```bash
   cd CSharpPortScanner
   ```

4. Follow that folder's `README.md` for the exact commands, options and example output.

---

## Technologies

- C# and .NET 8
- Async / await, `SemaphoreSlim`, `CancellationToken`
- `System.Net`, `System.Net.Sockets`, `System.Net.NetworkInformation`
- Networking basics: IP addresses, CIDR, ICMP, TCP, DNS

---

## Authorized use only

These projects are for **learning**.

- Only scan systems and networks **you own** or have **written permission** to test.
- Scanning without permission can be illegal in your country.

You are responsible for how you use this code.

---

## Questions and ideas

- Found a bug? Open an **Issue**.
- Have an idea for the next video? Leave a comment on YouTube.
- If the code helped you, a star on this repo helps the channel.

---

## License

[MIT](./LICENSE). Free to use, change and share.

---

**ITHubCoding** · Build it. Run it. Understand it. Secure it.
