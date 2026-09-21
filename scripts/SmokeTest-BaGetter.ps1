param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,
    [string]$PackageId = "TestData",
    [string]$PackageVersion = "1.2.3"
)

$ErrorActionPreference = "Stop"

function Test-Endpoint {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -UseBasicParsing
        Write-Host "[PASS] $Name => $($response.StatusCode) $Url"
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status) {
            Write-Host "[FAIL] $Name => $status $Url"
        }
        else {
            Write-Host "[FAIL] $Name => request error $Url"
        }
        throw
    }
}

$base = $BaseUrl.TrimEnd('/')

Write-Host "Running BaGetter smoke tests against: $base"

Test-Endpoint -Name "Service Index" -Url "$base/v3/index.json"
Test-Endpoint -Name "Health" -Url "$base/health"
Test-Endpoint -Name "Search" -Url "$base/v3/search?q=$PackageId"
Test-Endpoint -Name "Registration Index" -Url "$base/v3/registration/$PackageId/index.json"
Test-Endpoint -Name "Package Versions" -Url "$base/v3/package/$PackageId/index.json"

$packageUrl = "$base/v3/package/$PackageId/$PackageVersion/$PackageId.$PackageVersion.nupkg"
Test-Endpoint -Name "Package Download" -Url $packageUrl

Write-Host "Smoke tests completed."
