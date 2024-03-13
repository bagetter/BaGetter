import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Mirror (read-through caching)

Read-through caching lets you index packages from an upstream source. You can use read-through
caching to:

1. Speed up your builds if restores from [nuget.org](https://nuget.org) are slow
2. Enable package restores in offline scenarios

The following `Mirror` setting configures BaGetter to index packages from [nuget.org](https://nuget.org):

<Tabs>
  <TabItem value="None" label="No Authentication" default>
    ```json
    {
        ...

        "Mirror": {
            "Enabled":  true,
            "PackageSource": "https://api.nuget.org/v3/index.json"
        },

        ...
    }
    ```
  </TabItem>

  <TabItem value="Basic" label="Basic Authentication">
    For basic authentication, set `Type` to `Basic` and provide a `Username` and `Password`:

    ```json
    {
        ...

        "Mirror": {
            "Enabled":  true,
            "PackageSource": "https://api.nuget.org/v3/index.json",
            "Authentication": {
                "Type": "Basic",
                "Username": "username",
                "Password": "password"
            }
        },
    
        ...
    }
    ```
  </TabItem>

  <TabItem value="Bearer" label="Bearer Token">
    For bearer authentication, set `Type` to `Bearer` and provide a `Token`:

    ```json
    {
        ...

        "Mirror": {
            "Enabled":  true,
            "PackageSource": "https://api.nuget.org/v3/index.json",
            "Authentication": {
                "Type": "Bearer",
                "Token": "your-token"
            }
        },
    
        ...
    }
    ```
  </TabItem>

  <TabItem value="Custom" label="Custom Authentication">
    With the custom authentication type, you can provide any key-value pairs which will be set as headers in the request:

    ```json
    {
        ...

        "Mirror": {
            "Enabled":  true,
            "PackageSource": "https://api.nuget.org/v3/index.json",
            "Authentication": {
                "Type": "Custom",
                "CustomHeaders": {
                    "My-Auth": "your-value",
                    "Other-Header": "value"
                }
            }
        },
    
        ...
    }
    ```
  </TabItem>
</Tabs>


:::info

`PackageSource` is the value of the [NuGet service index](https://docs.microsoft.com/nuget/api/service-index).

:::