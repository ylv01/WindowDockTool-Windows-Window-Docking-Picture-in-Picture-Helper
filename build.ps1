param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDir = Join-Path $projectDir "bin\$Configuration"
$compilerCandidates = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$compiler = $compilerCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1

if (-not $compiler) {
    throw "The .NET Framework C# compiler (csc.exe) was not found."
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

$arguments = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/debug:pdbonly",
    "/codepage:65001",
    "/out:$outputDir\WindowDockTool-v1.2.exe",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "$projectDir\src\WindowDockTool.cs"
)

& $compiler $arguments
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Build completed: $outputDir\WindowDockTool-v1.2.exe"
