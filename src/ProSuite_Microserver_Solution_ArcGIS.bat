@cd /d "%~dp0"

set TargetFrameworkVersion=v4.8
set VSArcGISProduct=ArcGIS
set VSArcGISVersion=10.8
set OutputDirectory=..\..\bin
set ArcGISAssemblyPath=C:\Program Files (x86)\ArcGIS\DeveloperKit10.8\DotNet\

REM In case encryption is used, provide the encryptor factory from a custom location:
REM set ProSuiteEncryptorFactoryDir=..\..\..\ProSuite-etc\


REM MAKE SURE the Visual Studio folder where devenv.exe is located is in the Path environment variable. For Example
REM C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE

set DEVENV="devenv.exe"
set DEVOPTS=
@REM set DEVOPTS=/ReSharper.LogFile C:\Temp\ReSharper_Log.txt /ReSharper.LogLevel Verbose

%DEVENV% %DEVOPTS% ProSuite.Server.sln
