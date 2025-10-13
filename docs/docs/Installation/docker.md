# Run BaGetter on Docker

## Configure BaGetter (optional)

Create a file named `bagetter.env` to store BaGetter's configurations:

```shell
# The following config is the API Key used to publish packages.
# You should change this to a secret value to secure your server.
ApiKey=NUGET-SERVER-API-KEY

Storage__Type=FileSystem
Storage__Path=/data
Database__Type=Sqlite
Database__ConnectionString=Data Source=/data/db/bagetter.db
Search__Type=Database
```

For a full list of configurations, please refer to [BaGetter's configuration](../configuration.md) guide.

:::info

The `bagetter.env` file stores [BaGetter's configuration](../configuration) as environment variables.
Alternatively, the configuration can be injected via environment variables directly, e.g. the `environment` array in a docker compose file or the `--env` flag in a `docker run` command.
To learn how these configurations work, please refer to [ASP.NET Core's configuration documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.1&tabs=basicconfiguration#configuration-by-environment).

:::

If this step is omitted the default mode (unconfigured) will be Sqlite with the sql blobs stored in the path `/data/db/bagetter.db`.

## Run BaGetter

1. Create a folder named `bagetter-data` in the same directory as the `bagetter.env` file. This will be used by BaGetter to persist its state.
2. Pull BaGetter's latest [docker image](https://hub.docker.com/r/bagetter/bagetter):

```shell
docker pull bagetter/bagetter
```

You can now run BaGetter...

- ...with optional `.env` file:

```shell
docker run --rm --name nuget-server -p 5000:8080 --env-file bagetter.env -v "$(pwd)/bagetter-data:/data" bagetter/bagetter:latest
```

- ...or without:

```shell
docker run --rm --name nuget-server -p 5000:8080 -v "$(pwd)/bagetter-data:/data" bagetter/bagetter:latest
```

## Publish packages

Publish your first package with:

```shell
dotnet nuget push -s http://localhost:5000/v3/index.json -k NUGET-SERVER-API-KEY package.1.0.0.nupkg
```

Publish your first [symbol package](https://docs.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg) with:

```shell
dotnet nuget push -s http://localhost:5000/v3/index.json -k NUGET-SERVER-API-KEY symbol.package.1.0.0.snupkg
```

:::warning

The default API Key to publish packages is `NUGET-SERVER-API-KEY`.
You should change this to a secret value to secure your server. See [Configure BaGetter](#configure-bagetter-optional).

:::

## Browse packages

You can browse packages by opening the URL [`http://localhost:5000/`](http://localhost:5000/) in your browser.

## Restore packages

You can restore packages by using the following package source:

`http://localhost:5000/v3/index.json`

Some helpful guides:

- [Visual Studio](https://docs.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio#package-sources)
- [NuGet.config](https://docs.microsoft.com/en-us/nuget/reference/nuget-config-file#package-source-sections)

## Symbol server

You can load symbols by using the following symbol location:

`http://localhost:5000/api/download/symbols`

For Visual Studio, please refer to the [Configure Debugging](https://docs.microsoft.com/en-us/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger?view=vs-2017#configure-symbol-locations-and-loading-options) guide.

## Running BaGetter behind a reverse proxy

BaGetter can be run behind a reverse proxy in order to provide HTTPS, your own domain, and other features. For the API to deliver proper URLs, the proxy needs to forward the `X-Forwarded-Host` header, or the `Host` header iteslf.  
For more information, please refer to the [ASP.NET Core documentation](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer).

Consider binding port 5000 to localhost only using -p 127.0.0.1:5000:8080 to avoid any transmission of non encrypted data even over internal networks

### Apache 2 Configuration
```config
<IfModule mod_ssl.c>
	<VirtualHost *:443>

		ServerName nuget.example.com

		ProxyRequests Off
		ProxyPreserveHost On
		ProxyPass / http://localhost:5000/
		ProxyPassReverse / http://localhost:5000/
		RequestHeader set X-Forwarded-Proto https

		SSLCertificateFile /etc/letsencrypt/live/nuget.example.com/fullchain.pem #managed by cerbot
		SSLCertificateKeyFile /etc/letsencrypt/live/nuget.example.com/privkey.pem #managed by cerbot
		Include /etc/letsencrypt/options-ssl-apache.conf
		Header always set Strict-Transport-Security "max-age=31536000"
		Header always set Content-Security-Policy upgrade-insecure-requests
	</VirtualHost>
</IfModule>
```

