@cd /d "%~dp0"

set TargetFrameworkVersion=v4.8
set OutputDirectory=..\..\bin

REM Supported ArcGIS Pro Versions: 2.6 - 2.9
set ProAssemblyPath=C:\Program Files\ArcGIS\Pro

REM In case encryption is used, provide your encryptor factory from a custom location:
REM set ProSuiteEncryptorFactoryDir=..\..\..\ProSuite-etc\

REM In order to disable some legacy references in ProSuite.UI, use a new (so far un-used) value:
set VSArcGISProduct=Pro

REM MAKE SURE the Visual Studio folder where devenv.exe is located is in the Path environment variable. Example:
REM C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE

set DEVENV="devenv.exe"
set DEVOPTS=
@REM set DEVOPTS=/ReSharper.LogFile C:\Temp\ReSharper_Log.txt /ReSharper.LogLevel Verbose

%DEVENV% %DEVOPTS% ProSuite.sln
