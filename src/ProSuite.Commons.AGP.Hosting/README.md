
# Unit Tests with the ArcGIS Pro SDK

From the ArcGIS Pro SDK's point of view, Unit Tests are standalone
code, that is, code not running within the ArcGIS Pro application.

Therefore, you must reference both `ArcGIS.CoreHost` and `ArcGIS.Core`
with Copy Local = True, and call `ArcGIS.Core.Hosting.Host.Initialize`
during the test fixture's one-time setup.

The ArcGIS Pro Extensions NuGet cannot be used because (1) it does not
contain `ArcGIS.CoreHost` and (2) its other DLLs are not “Copy Local”
(empirical).

This project is intended to ease unit testing Pro SDK code: it contains
references to `ArcGIS.CoreHost` and `ArcGIS.Core` with Copy Local semantics
(that is, copying the two DLLs to the output directory) and provides
a single static method to call for initialization. To use, add a
project reference to your unit test project, and in one-time initialization
call `ProSuite.Commons.AGP.Hosting.CoreHostProxy.Initialize()`.

## References

- [Using the ArcGIS Pro Extensions NuGet](https://github.com/esri/arcgis-pro-sdk/wiki/ProGuide-ArcGIS-Pro-Extensions-NuGet#using-the-the-arcgis-pro-extensions-nuget)
- [ProConcepts: CoreHost](https://github.com/esri/arcgis-pro-sdk/wiki/proconcepts-CoreHost)
