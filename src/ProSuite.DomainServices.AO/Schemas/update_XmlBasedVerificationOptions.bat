@set ToolsDir="C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools"

%ToolsDir%\xsd.exe ..\..\..\..\bin\Debug\ProSuite.DomainServices.AO.dll /type:ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options.XmlVerificationOptions 

copy schema0.xsd ProSuite.QA.XmlBasedVerificationOptions-1.0.xsd /Y
del schema0.xsd

pause
