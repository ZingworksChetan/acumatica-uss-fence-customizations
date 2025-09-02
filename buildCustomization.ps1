param (
    [string]$versionName  # Accepts version name as an argument
)
if (-not $versionName) {
    Write-Host "Error: versionName parameter is required."
    exit 1
}
# Construct paths using System.IO to handle special characters properly
$customizationPath = [System.IO.Path]::Combine("Customizations", "AcumaticaUSSFence")
#$zipFileName = [System.IO.Path]::Combine("acumatica-customization", "Customization", $versionName, "$versionName.zip")
$zipFileName = [System.IO.Path]::Combine("build", "$versionName.zip")

$projectDescription = $env:PROJECT_DESCRIPTION
$projectLevel = $env:PROJECT_LEVEL

Write-Host "Using Description--->: $projectDescription"
Write-Host "Using Level--->: $projectLevel"

# Check if the customization folder exists
if (-not (Test-Path -LiteralPath $customizationPath -PathType Container)) {
    Write-Host "Error: Customization folder does not exist: $customizationPath"
    exit 1
}

# Check if the directory contains files
$files = Get-ChildItem -LiteralPath $customizationPath -Recurse -ErrorAction SilentlyContinue
if (-not $files) {
    Write-Host "Error: Customization files not found in: $customizationPath. Not able to generate ZIP."
    exit 1
}

# Set Level to 300 if it's missing or empty in env
if (-not $projectLevel -or $projectLevel.Trim() -eq "") {
    Write-Host "Warning: 'Level' is missing in env. Defaulting to 300."
    $projectLevel = 300
}

if (-not $projectDescription -or $projectDescription.Trim() -eq "") {
    Write-Host "Warning: 'Description' is missing. Defaulting to project name ."
    $projectDescription = $versionName
}

if (![string]::IsNullOrWhiteSpace($zipFileName)) {
    $buildFolder = [System.IO.Path]::GetDirectoryName($zipFileName)
    if (![string]::IsNullOrWhiteSpace($buildFolder) -and !(Test-Path -LiteralPath $buildFolder)) {
        New-Item -ItemType Directory -Path $buildFolder -Force | Out-Null
    }
}

#$cmd = "dlls\CustomizationPackageTools\CustomizationPackageTools.exe"
$cmd = "dlls\CustomizationPackageTools\bin\Release\net8.0\CustomizationPackageTools.exe"

# Execute the build command safely
try {
    &$cmd build --customizationpath "$customizationPath" --packagefilename "$zipFileName" --description "$projectDescription" --level $projectLevel
    
    if (Test-Path -LiteralPath "$zipFileName") {
        Write-Host "Customization package created successfully: $zipFileName"
    } else {
        Write-Host "Error: Customization package was not created!" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Error occurred while building customization package: $_" -ForegroundColor Red
    exit 1
}