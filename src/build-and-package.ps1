# To Run:
# .\build-and-package.ps1 -Version "v1.x.x"

# Use "dev" as the default version label if none is provided (e.g. for local test builds)
param (
    [string]$Version = "dev"
)

# Base output folder for builds
$publishRoot = "publish/$Version"

# Folder for final archives, outside the working build folders
$archiveFolder = "publish/output"

# Define targets and their subfolder names
$targets = @{
    "win-x86"   = "win-x86"
    "win-x64"   = "win-x64"
    "linux-x64" = "linux-x64"
    "osx-x64"   = "osx-x64"
    "osx-arm64" = "osx-arm64"
}

# Clean previous build and outputs
Remove-Item -Recurse -Force $publishRoot, $archiveFolder -ErrorAction SilentlyContinue

# Ensure archive output folder exists
New-Item -ItemType Directory -Force -Path $archiveFolder | Out-Null

foreach ($rid in $targets.Keys) {
    $subfolder = $targets[$rid]

    # Paths for publish temp folder and final renamed folder
    $publishTemp = "$publishRoot/$subfolder/publish-temp"
    $fur2ugeFolder = "$publishRoot/$subfolder/fur2uge"

    # Clean any leftovers from previous runs
    Remove-Item -Recurse -Force $publishTemp, $fur2ugeFolder -ErrorAction SilentlyContinue

    # Publish to temp folder
    dotnet publish -c Release -r $rid --self-contained true -p:PublishSingleFile=true -o $publishTemp

    # Rename publish-temp folder to fur2uge (the folder that will be archived)
    Rename-Item -Path $publishTemp -NewName "fur2uge"

    # Define archive name and full path
    $archiveName = "fur2uge-$Version-$subfolder.7z"
    $archiveFullPath = Join-Path (Resolve-Path $archiveFolder) $archiveName

    # Remove existing archive if it exists
    if (Test-Path $archiveFullPath) {
        Remove-Item $archiveFullPath
    }

    # Archive the fur2uge folder so it is root inside the archive
    Push-Location "$publishRoot/$subfolder"
    & "C:\Program Files\7-Zip\7z.exe" a $archiveFullPath "fur2uge"
    Pop-Location

    Write-Host "Packaged: $archiveName"
}

# Clean up the entire working publish folder after archiving
Remove-Item -Recurse -Force $publishRoot -ErrorAction SilentlyContinue

Write-Host "All platforms packaged with version $Version"
Write-Host "Archives located in: $archiveFolder"
