@cd /d "%~dp0"

set TargetFrameworkVersion=v4.8
set OutputDirectory=..\..\bin
set ProNuGetVersion=2.5
set ProAssemblyPath=..\..\..\EsriDE.Commons\lib\ESRI\ProSDK\2.5
@REM set ProAssemblyPath=C:\Program Files\ArcGIS\Pro

REM In case encryption is used, provide the encryptor factory from a custom location:
REM set ProSuiteEncryptorFactoryDir=..\..\..\ProSuite-etc\

set DEVENV="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe"
set DEVOPTS=
@REM set DEVOPTS=/ReSharper.LogFile C:\Temp\ReSharper_Log.txt /ReSharper.LogLevel Verbose

%DEVENV% %DEVOPTS% ProSuite.sln
