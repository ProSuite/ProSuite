using ArcGIS.Core.Geometry;

namespace ProSuite.Processing.AGP.Core.Domain
{
	public interface IProcessingSymbology
	{
		// Abstraction over ArcObjects and ArcGIS Pro symbology -- if possible...
		Geometry QueryDrawingOutline(long oid, OutlineType outlineType);
	}

	public enum OutlineType
	{
		Exact, BoundingBox
	}
}
