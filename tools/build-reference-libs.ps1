<#
Builds Stance Overhaul + Realism Common Lib from their source repos into lib\ so this addon can compile
against them, WITHOUT touching those repos or your game folder.

Why: their csproj files hard-code another machine's game path (F:\SP EFT\SPT-41X) and copy the DLL into
that game folder after every build. This script builds a patched temp copy instead.

Usage:  pwsh tools\build-reference-libs.ps1
        pwsh tools\build-reference-libs.ps1 -SptRoot 'F:\SPT_4.1 - Dev'
#>
param(
    [string]$SoRepo = 'D:\Git Repo\spt-stance-overhaul-main',
    [string]$ClRepo = 'D:\Git Repo\spt-realism-common-lib-main',
    [string]$SptRoot = 'F:\SPT_4.1',
    [string]$Lib = (Join-Path $PSScriptRoot '..\lib')
)

$ErrorActionPreference = 'Stop'
$work = Join-Path ([IO.Path]::GetTempPath()) ('so-ref-build-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $work | Out-Null

try {
    $projects = @(
        @{ Src = $ClRepo; Name = 'spt-realism-common-lib' },
        @{ Src = $SoRepo; Name = 'spt-stance-overhaul' }
    )

    foreach ($p in $projects) {
        $dst = Join-Path $work $p.Name
        robocopy $p.Src $dst /E /XD .git bin obj .vs .idea /NFL /NDL /NJH /NJS /NP | Out-Null

        $csproj = Join-Path $dst "$($p.Name).csproj"
        $text = Get-Content $csproj -Raw
        $text = $text -replace [regex]::Escape('F:\SP EFT\SPT-41X'), $SptRoot
        $text = [regex]::Replace($text, '<Target Name="PostBuildCopy".*?</Target>', '', 'Singleline')
        Set-Content $csproj $text -Encoding utf8
    }

    $out = Join-Path $work 'out'
    dotnet build (Join-Path $work 'spt-stance-overhaul\spt-stance-overhaul.csproj') -c Release -o $out --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'build failed' }

    New-Item -ItemType Directory -Force $Lib | Out-Null
    Copy-Item (Join-Path $out 'spt-stance-overhaul.dll') $Lib -Force
    Copy-Item (Join-Path $out 'spt-realism-common-lib.dll') $Lib -Force

    $fika = Join-Path $SptRoot 'BepInEx\plugins\Fika\Fika.Core.dll'
    if (Test-Path $fika) { Copy-Item $fika $Lib -Force }

    Write-Host "reference libs written to $((Resolve-Path $Lib).Path)"
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
