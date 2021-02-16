using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.PolygonGrower
{
	public interface ITopologicalLine
	{
		int PartIndex { get; }

		[NotNull]
		IPoint FromPoint { get; }

		[NotNull]
		IPoint ToPoint { get; }

		double FromAngle { get; }
		double ToAngle { get; }

		double Length { get; }

		[NotNull]
		IPolyline GetLine();

		[NotNull]
		ICurve GetPath();
	}
}
