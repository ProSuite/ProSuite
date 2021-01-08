using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	internal interface IGeometryEngine
	{
		void SetSourceGeometry([NotNull] IGeometry geometry);

		void SetTargetGeometry([NotNull] IGeometry geometry);

		bool EvaluateRelation([NotNull] ISpatialFilter spatialFilter);

		bool AssumeEnvelopeIntersects { get; set; }
	}
}
