#
# Usage samples:

# .\build_prosuite.ps1 [-product AddIn|Microserver] [-prosdk 2.6|2.7|2.8|2.9] [-cpu x86|AnyCPU] [-arcgisvers 10.4|10.5|10.6|10.7|10.8] [-arcobjects 10|11] [-release] [-zip] [-info]

# without -product will compile AddIn
# without -prosdk will use binaries from local installation of ArcGIS Pro, otherwise version of EsriDE.Commons\lib\ESRI\ProSDK
# without -cpu will compile AnyCPU configuration
# without -arcgisvers will compile with 10.8 binaries (is relevant only for -product Microserver) 
# without -arcobjects version 10 will be used (environment variable VSArcGISProduct="ArcGIS") -arcobjects 11 means (VSArcGISProduct="Server")
# switch -release will compile release version (will increase version number v in ${Product}.versions.txt x.v.x.x - without is test version: x.x.v.x)
# switch -zip will compress output folder
# switch -info will display environment variables, calculated build configuration and quit without building

# AddIn für ArcGIS Pro 2.9
# .\build_prosuite.ps1 -product AddIn -prosdk 2.9 -release

# Microservice für lokales ArcMap 10.8
# .\build_prosuite.ps1 -product Microserver -arcgisvers 10.8 -cpu x86 -release -zip

# Microservice für ArcGIS Server 10.9
# .\build_prosuite.ps1 -product Microserver -arcgisvers 10.9 -cpu x86 -arcobjects 11 -release -zip

Param(
	$Cpu = 'Any CPU',
	$Product= 'AddIn',
	$ArcGISVers = '10.8',
	$ArcObjects = '10',
	$ProSdk,
	[Switch]$Zip,
	[Switch]$Release,
	[Switch]$Info
)

# Various directories and path variables
$App_BuildDir = $PSScriptRoot
Set-Location -Path $App_BuildDir

function Print-Error($Message)
{
	Write-Host "`n`rError: *****************************************************************"
	Write-Host "${Message}`n`r"
}

# local ArcGIS Pro SDK path is default
$ProSdkPath = "C:\Program Files\ArcGIS\Pro"
if ($ProSdk) {
	$ProSdkPath = "..\..\EsriDE.Commons\lib\ESRI\ProSDK\${ProSdk}\"
	$env:ProAssemblyPath="..\${ProSdkPath}"
}
else {
	$ProSdk = "local"
	$ProSdkPath = "C:\Program Files\ArcGIS\Pro"
	$env:ProAssemblyPath="${ProSdkPath}"
}
if (-Not (Test-Path $ProSdkPath)) {
	Print-Error("ProAssemblyPath = ${ProSdkPath} does not exist!")
	exit(1)
}

# default solution
$Solution = "ProSuite.sln"

# Environment Variables for build (should probably be set by the caller)
$env:TargetFrameworkVersion='v4.8'

# In case encryption is used, provide the encryptor factory from a custom location:
# $env:ProSuiteEncryptorFactoryDir='..\..\..\ProSuite-etc\'
$env:ProSuiteEncryptorFactoryDir=''

if ($Info) {
	Write-Host "`n`rEnvironment variables:"
	Write-Host "--------------------------"
	Write-Host "TargetFrameworkVersion:		${env:TargetFrameworkVersion}"
	Write-Host "ProAssemblyPath:		${env:ProAssemblyPath}"
	Write-Host "ProSuiteEncryptorFactoryDir:		${env:ProSuiteEncryptorFactoryDir}"
}

# microserver specific params
if ($Product -eq 'Microserver') {
	
	$Solution = 'ProSuite.Server.sln'
	$env:VSArcGISVersion="${ArcGISVers}\"
	
	# 
	if ($ArcObjects -eq '10') {
		$env:VSArcGISProduct="ArcGIS"
	}
	else {
		if ($ArcObjects -eq '11') {
			$env:VSArcGISProduct="Server"
			if ($Cpu -eq "x86") {
				Print-Error("ArcObjects 11 supports only x64 build!")
				exit(1)					
			}
		}
		else {
			Print-Error("Version of ArcObjects can be only 10 or 11!")
			exit(1)		
		}
	}
	$ServerAssemblyPath = "..\..\EsriDE.Commons\lib\ESRI\${env:VSArcGISProduct}\"
	$FullArcGISAssemblyPath = "${ServerAssemblyPath}${ArcGISVers}"
	if (-Not (Test-Path $FullArcGISAssemblyPath)) {
		Print-Error("ArcGISAssemblyPath = ${FullArcGISAssemblyPath} does not exist!")
		exit(1)
	}
	
	$env:ArcGISAssemblyPath="..\${ServerAssemblyPath}"		
	if ($Info) {
		Write-Host "VSArcGISVersion:		${env:VSArcGISVersion}"
		Write-Host "VSArcGISProduct:		${env:VSArcGISProduct}"
		Write-Host "ArcGISAssemblyPath:		${env:ArcGISAssemblyPath}"
		Write-Host "ArcObjects version:		${ArcObjects}"
		Write-Host "ESRI DLLs:			${env:ArcGISAssemblyPath}\${env:VSArcGISVersion}ESRI.${env:VSArcGISProduct}.*.dll"
	}
}
Write-Host "`n`rBuild parameters:"
Write-Host "--------------------------"
Write-Host "Building product:	${Product}"
Write-Host "Architecture:		${Cpu}"
Write-Host "ArcGIS Pro SDK:		${ProSdk}"
Write-Host "Solution:		${Solution}"
Write-Host "Release build:		${Release}"
Write-Host "`n`r"

if ($Info) {
	exit(0)
}

Write-Host "`r`nBuilding solution ..."

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
	if ([object]::Equals($msbuildAbsolutePath, $null)) {
		$msbuildAbsolutePath = Get-ChildItem -Path ${env:ProgramFiles(x86)} -File -Recurse -Filter MSBuild.exe -ErrorAction:SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
	}
	return $msbuildAbsolutePath
}

# *** BEGIN MAIN SCRIPT

# Assembly versions
$BuildVersionFilePath = $App_BuildDir + "\${Product}.version.txt"

# Increase versions number 
#	- for release second number 
#	- for test - third number
$BuildVersionFileContent = Get-Content -Path $BuildVersionFilePath -TotalCount 1
$BuildVersions = $BuildVersionFileContent.split('.') | % {iex $_}
if ($Release) {
	$BuildVersions[1] += 1
	$BuildVersions[2] = 0
}
else {
	$BuildVersions[2] += 1	
}
$NewVersion = $BuildVersions -join '.'
Set-Content -Path $BuildVersionFilePath -Value $NewVersion
Write-Host "Version:		${NewVersion} (${BuildVersionFilePath})`r`n"

# Start build actions

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

md -Force $BuildOutputDir | Out-Null

[System.IO.FileInfo] $mapsSolutionFile = $SolutionPath
$msbuildAbsolutePath = Get-MSBuildAbsolutePath 
$msbuildArgumentList = @(
	"`"$($mapsSolutionFile.FullName)`"",
	"/p:Configuration=Release",
	"/p:Platform=""${Cpu}""",
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

$Cpu = $Cpu -replace ' ',''

if ($Zip) {
	Get-ChildItem -Path $BuildOutputDir -Exclude *.dylib, *.so | Compress-Archive -Force -DestinationPath $BuildOutputDir\ProSuite_${Product}_${Cpu}_v${NewVersion}.zip
	Write-Host "`r`nArchive $BuildOutputDir\ProSuite_${Product}_${Cpu}_v${NewVersion}.zip created"
}

Write-Host "`r`nBuild process finished`r`n"
