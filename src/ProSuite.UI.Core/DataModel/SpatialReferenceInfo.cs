using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.UI.Core.DataModel
{
	public class SpatialReferenceInfo
	{
		[NotNull]
		public string Name { get; }

		public double XYResolution { get; }
		public double XYTolerance { get; }
		public double ZResolution { get; }
		public double ZTolerance { get; }

		public bool IsGeographicCoordinateSystem { get; }

		public SpatialReferenceInfo([NotNull] string name,
		                            double xyResolution,
		                            double xyTolerance,
		                            double zResolution,
		                            double zTolerance,
		                            bool isGeographicCoordinateSystem)
		{
			Name = name;
			XYResolution = xyResolution;
			XYTolerance = xyTolerance;
			ZResolution = zResolution;
			ZTolerance = zTolerance;
			IsGeographicCoordinateSystem = isGeographicCoordinateSystem;
		}
	}
}
