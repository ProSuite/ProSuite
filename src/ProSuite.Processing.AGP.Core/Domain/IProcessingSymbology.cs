using ArcGIS.Core.Geometry;
using ProSuite.Processing.AGP.Core.Utils;
using ProSuite.Processing.Domain;

namespace ProSuite.Processing.AGP.Core.Domain
{
	public interface IProcessingSymbology
	{
		// Abstraction over ArcObjects and ArcGIS Pro symbology -- if possible...
		Geometry QueryDrawingOutline(long oid, OutlineType outlineType);

		Geometry QueryDrawingOutline(PseudoFeature feature, OutlineType outlineType, IMapContext mapContext);
	}

	public enum OutlineType
	{
		Exact, BoundingBox
	}
}
