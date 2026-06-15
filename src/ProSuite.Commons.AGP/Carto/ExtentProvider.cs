using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;

namespace ProSuite.Commons.AGP.Carto;

public class ExtentProvider : IExtentProvider
{
	public Envelope Extent => MapView.Active?.Extent;
}
