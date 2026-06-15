using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Carto;

public interface IExtentProvider
{
	// TODO: Allow providing multiple maps with descending priority (e.g. for stereoscopic views)
	// TODO: Consider moving to WorkList.Contracts if used there only
	[CanBeNull]
	public Envelope Extent { get; }
}
