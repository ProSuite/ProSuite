set Version=1.2.0
set TargetFrameworkVersion=v4.8
set TargetFrameworkVersionShort=net48

set VSArcGISProduct=ArcGIS
set VSArcGISVersion=10.8\
set ArcGISAssemblyPath=..\..\..\EsriDE.Commons\lib\ESRI\ArcGIS

rem set ProSuiteEncryptorFactoryDir=..\..\..\EsriCH.ProSuiteSolution\src\EsriCH.ProSuiteSolution.Core\SymmetricEncryption 

"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\msbuild.exe" "..\src\ProSuite.Commons\ProSuite.Commons.csproj" /property:Configuration=Release
nuget pack ProSuite.Commons.csproj.nuspec -OutputDirectory .\output -IncludeReferencedProjects -version %Version% -Properties target=%TargetFrameworkVersionShort%

pause