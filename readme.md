# BaGetter 🥖🛒


BaGetter is a lightweight [NuGet] and [symbol] server, written in C#.
It's forked from [BaGet] for progressive and community driven development.

<p align="center">
  <img width="100%" src="https://user-images.githubusercontent.com/737941/50140219-d8409700-0258-11e9-94c9-dad24d2b48bb.png">
</p>

![Build status] [![Discord][Discord image]][Discord link]

## 🚀 Getting Started
With Docker:
1. `docker run -p 5000:8080 -v ./bagetter-data:/data bagetter/bagetter:latest`
2. Browse `http://localhost:5000/` in your browser

With .NET:
1. Install the [.NET SDK]
2. Download and extract [BaGetter's latest release]
3. Start the service with `dotnet BaGetter.dll`
4. Browse `http://localhost:5000/` in your browser

For more information, please refer to the [documentation].

## 📦 Features

* **Cross-platform**: runs on Windows, macOS, and Linux!
* **Cloud native**: supports [Docker], [Azure], [AWS], [Google Cloud], [Alibaba Cloud]
* **Offline support**: [Mirror a NuGet server] to speed up builds and enable offline downloads

## 🤝 Contributing

We welcome contributions! Check out the [Contributing Guide](CONTRIBUTING.md) to get started.

## 📄 License

This project is licensed under the [MIT License](LICENSE).

## 📞 Contact

If you have questions, feel free to open an [issue] or join our [Discord Server] for discussions.

[Build status]: https://img.shields.io/github/actions/workflow/status/bagetter/BaGetter/.github/workflows/main.yml?logo=github&logoColor=fff
[Discord image]: https://img.shields.io/discord/1181167608427450388?logo=discord&logoColor=fff
[Discord link]: https://discord.gg/XsAmm6f2hZ
[Discord Server]: https://discord.gg/XsAmm6f2hZ

[NuGet]: https://learn.microsoft.com/nuget/what-is-nuget
[symbol]: https://docs.microsoft.com/en-us/windows/desktop/debug/symbol-servers-and-symbol-stores
[.NET SDK]: https://www.microsoft.com/net/download
[Node.js]: https://nodejs.org/
[Issue]: https://github.com/bagetter/BaGetter/issues
[BaGet]: https://github.com/loic-sharma/BaGet

[BaGetter's latest release]: https://github.com/bagetter/BaGetter/releases

[Documentation]: https://www.bagetter.com/
[Docker]: https://hub.docker.com/r/bagetter/bagetter
