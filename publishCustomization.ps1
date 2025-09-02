param (
    [string]$versionName  # Accepts version name as an argument
)

if (-not $versionName) {
    Write-Host "Error: versionName parameter is required."
    exit 1
}

# Construct paths safely
$customizationPath = [System.IO.Path]::Combine("Customizations", "AcumaticaUSSFence")
#$zipFilePath = [System.IO.Path]::Combine($customizationPath, "$versionName.zip")
$zipFilePath = [System.IO.Path]::Combine("build", "$versionName.zip")

$packageName = $versionName
$serverUrl = $env:AC_BASE_URL
$username = $env:AC_USERNAME
$password = "Admin@123"
#$password = $env:AC_PASSWORD
$projectDescription = $env:PROJECT_DESCRIPTION
$projectLevel = $env:PROJECT_LEVEL

$missing = @()
#Ensure serverUrl exist
if (-not $serverUrl -or $serverUrl.Trim() -eq "") {
    $missing += "AC_BASE_URL"
}
#Ensure username exist
if (-not $username -or $username.Trim() -eq "") {
    $missing += "AC_USERNAME"
}
#Ensure password exist
if (-not $password -or $password.Trim() -eq "") {
    $missing += "AC_PASSWORD"
}
if ($missing.Count -gt 0) {
    Write-Host "Error: The following required environment variables are missing:`n - $($missing -join "`n - ")"
    exit 1
}
# Ensure the ZIP file exists
if (-not (Test-Path -LiteralPath $zipFilePath)) {
    Write-Host "Error: Customization package '$zipFilePath' not found. Cannot publish."
    exit 1
}

# Set Level to 300 if it's missing or empty
if (-not $projectLevel -or $projectLevel.Trim() -eq "") {
    Write-Host "Warning: 'Level' is missing in env. Defaulting to 300."
    $projectLevel = 300
}
if (-not $projectDescription -or $projectDescription.Trim() -eq "") {
    Write-Host "Warning: 'Description' is missing in env. Defaulting to project name ."
    $projectDescription = $versionName
}

#$cmd = "dlls\CustomizationPackageTools\CustomizationPackageTools.exe"
 $cmd = "dlls\CustomizationPackageTools\bin\Release\net8.0\CustomizationPackageTools.exe"

# Execute the publish command safely
&$cmd publish --packagefilename "$zipFilePath" --packagename "$packageName" --url "$serverUrl" --username "$username" --password "$password" --description "$projectDescription" --level "$projectLevel"