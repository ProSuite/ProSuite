using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Carto;

public interface IMapViewContext
{
	[CanBeNull]
	public Envelope Extent { get; }
}
