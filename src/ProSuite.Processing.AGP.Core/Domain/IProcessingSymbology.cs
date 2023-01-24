using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.AGP.Core.Utils;
using ProSuite.Processing.Domain;

namespace ProSuite.Processing.AGP.Core.Domain
{
	public interface IProcessingSymbology
	{
		// Abstraction over ArcObjects and ArcGIS Pro symbology -- if possible...
		// Can be null if a feature does not draw
		[CanBeNull]
		Geometry QueryDrawingOutline(Feature feature, OutlineType outlineType, IMapContext mapContext);

		// Get outline of a non-real feature, implemented using our own code
		// Can be null if a feature does not draw
		[CanBeNull]
		Geometry QueryDrawingOutline(PseudoFeature feature, OutlineType outlineType, IMapContext mapContext);
	}

	public enum OutlineType
	{
		Exact, BoundingBox
	}
}
