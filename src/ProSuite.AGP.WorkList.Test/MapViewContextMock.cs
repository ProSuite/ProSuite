using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Carto;

namespace ProSuite.AGP.WorkList.Test;

public class MapViewContextMock : IMapViewContext
{
	public Envelope Extent { get; }
}
