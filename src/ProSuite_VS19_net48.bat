@cd /d "%~dp0"

set TargetFrameworkVersion=v4.8
set OutputDirectory=..\..\bin

REM In case encryption is used, provide the encryptor factory from a custom location:
REM set ProSuiteEncryptorFactoryDir=..\..\..\ProSuite-etc\

"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\devenv.exe" ProSuite.sln
