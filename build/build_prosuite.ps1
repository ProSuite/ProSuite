#  Possible commanline parameters:
#
# -product Server, AddIn - without is AddIn 
# -proSdk = 2.5,2.6,... (EsriDE.Commons\lib\ESRI\ProSDK\...) without is local ArcGIS Pro
# -zip output will bo compressed into ProSuite_${Product}_${Platform}_v${NewVersion}.zip - without none 
# -arcgisvers = 10.8 - is relevant for Product = Server, without is 10.8 
#
Param(
	$Cpu = 'x64',
	$Product,
	$ArcGISVers = '10.8',
	$ProSdk,
	$Zip
)

# derived startup parameters 

# local ArcGIS Pro SDK path is default
$ProSdkPath = "C:\Program Files\ArcGIS\Pro"
if ($ProSdk) {
	$ProSdkPath = "..\..\..\EsriDE.Commons\lib\ESRI\ProSDK\${ProSdk}\"
}
else {
	$ProSdk = "local"
}

# default solution
$Solution = "ProSuite.sln"

# server specific params
if ($Product) {
	if ($Product = 'Server') {
		$Solution = 'ProSuite.Server.sln'
		$env:VSArcGISVersion="${ArcGISVers}\"
		$env:VSArcGISProduct=$Product
		$env:ArcGISAssemblyPath='..\..\..\Swisstopo.Topgis\EsriDE.Commons\lib\ESRI\Server\'	
		# TODO probably ProSuite build should not depend on Swisstopo.Topgis repository 
		#$env:ArcGISAssemblyPath='..\..\..\EsriDE.Commons\lib\ESRI\Server\'	
	}
}
else {
	$Product = 'AddIn'	
}

# Environment Variables for build (should probably be set by the caller)
$env:TargetFrameworkVersion='v4.8'
$env:ProAssemblyPath=$ProSdkPath

# In case encryption is used, provide the encryptor factory from a custom location:
# $env:ProSuiteEncryptorFactoryDir='..\..\..\ProSuite-etc\'
$env:ProSuiteEncryptorFactoryDir=''

Write-Host "`n`rBuilding product:	${Product}"
Write-Host "Architecture:		${Cpu}"
Write-Host "ArcGIS Pro SDK:		${ProSdk}"


# *** FUNCTION DEFINITIONS

function Set-AssemblyVersion($RootDir, $Version)
{
	Get-ChildItem -Path $RootDir -Include AssemblyInfo.cs, SharedAssemblyInfo.cs -Recurse | 
		ForEach-Object {
			$_.IsReadOnly = $false
			(Get-Content -Path $_) -replace '(?<=Assembly(?:File)?Version\(")[^"]*(?="\))', $Version |
				Set-Content -Path $_ -Encoding utf8
    }
}

function Get-MSBuildAbsolutePath
{
    [OutputType([System.IO.FileInfo])]

    $msbuildAbsolutePath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
    if ([object]::Equals($msbuildAbsolutePath, $null))
    {
        $msbuildAbsolutePath = Get-ChildItem -Path ${env:ProgramFiles(x86)} -File -Recurse -Filter MSBuild.exe -ErrorAction:SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
    }
    return $msbuildAbsolutePath
}

# *** BEGIN MAIN SCRIPT

# Various directories and path variables
$App_BuildDir = $PSScriptRoot
Set-Location -Path $App_BuildDir

# Assembly versions
$BuildVersionFile = $App_BuildDir + "\${Product}.version.txt"
$NewVersion = Get-Content -Path $BuildVersionFile -TotalCount 1
Write-Host "Version:		${NewVersion} (${BuildVersionFile})`r`n"

# Start build actions
Write-Host "Start building .... `r`n"

# Clean previous build
$BuildOutputDir = $App_BuildDir + "\output"

if (Test-Path ${BuildOutputDir}) {
	Write-Host "Cleaning previous output at ${BuildOutputDir}..."
	Remove-Item $BuildOutputDir -Recurse
	}

# Get location of SharedAssemblyInfo.cs 
$Codebase_RootDir = $App_BuildDir + "\..\src\"
Write-Host "Codebase root:		${Codebase_RootDir}"

$SolutionPath = $Codebase_RootDir + $Solution
Write-Host "Solution path:		${SolutionPath}"

# Backup SharedAssemblyInfo 
$SolutionBackupDir = $App_BuildDir + "\backup"
Write-Host "Backup directory:	${SolutionBackupDir}"

$AssemblyVersionFilePath = $Codebase_RootDir + "\SharedAssemblyInfo.cs"
$AssemblyVersionBackupFilePath = $SolutionBackupDir + "\SharedAssemblyInfo.cs.backup"

Write-Host "Backing SharedAssemblyInfo.cs to ${AssemblyVersionBackupFilePath}"
md -Force $SolutionBackupDir | Out-Null
Copy-Item $AssemblyVersionFilePath -Destination $AssemblyVersionBackupFilePath -Force

Set-AssemblyVersion $Codebase_RootDir $NewVersion

Write-Host "`r`nBuilding solution..."

md -Force $BuildOutputDir | Out-Null

[System.IO.FileInfo] $mapsSolutionFile = $SolutionPath
$msbuildAbsolutePath = Get-MSBuildAbsolutePath 
$msbuildArgumentList = @(
	"`"$($mapsSolutionFile.FullName)`"",
	"/p:Configuration=Release",
	"/p:Platform=""Any CPU""",
	"/p:OutputPath=`"$($BuildOutputDir)`"",
	"/p:UseSharedCompilation=false",
	"/restore",
	"/nodeReuse:false"
)
$process = Start-Process -FilePath $msbuildAbsolutePath -ArgumentList $msbuildArgumentList -NoNewWindow -Wait
# TODO $process.ExitCode?

# Reset file versions (TODO)
Set-AssemblyVersion $Codebase_RootDir 1.0.0.0

Write-Host "`r`nRestoring ${AssemblyVersionFilePath} from ${AssemblyVersionBackupFilePath}"
Move-Item $AssemblyVersionBackupFilePath -Destination $AssemblyVersionFilePath -Force

if ($Zip) {
	Get-ChildItem -Path $BuildOutputDir -Exclude *.dylib, *.so | Compress-Archive -Force -DestinationPath $BuildOutputDir\ProSuite_${Product}_${Platform}_v${NewVersion}.zip
	Write-Host "`r`nArchive $BuildOutputDir\ProSuite_${Product}_${Platform}_v${NewVersion}.zip created"
}

Write-Host "`r`nBuild process finished"
