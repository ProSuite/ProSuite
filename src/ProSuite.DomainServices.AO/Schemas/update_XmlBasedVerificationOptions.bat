@set ToolsDir="C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools"

%ToolsDir%\xsd.exe ..\..\..\..\bin\Debug\EsriDE.ProSuite.Services.dll /type:EsriDE.ProSuite.Services.QA.GP.XmlBased.Options.XmlVerificationOptions 

copy schema0.xsd EsriDE.ProSuite.QA.XmlBasedVerificationOptions-1.0.xsd /Y
del schema0.xsd

pause
