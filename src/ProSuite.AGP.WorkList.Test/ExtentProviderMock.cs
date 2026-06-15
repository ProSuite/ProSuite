using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Carto;

namespace ProSuite.AGP.WorkList.Test;

public class ExtentProviderMock : IExtentProvider
{
	public Envelope Extent { get; }
}
