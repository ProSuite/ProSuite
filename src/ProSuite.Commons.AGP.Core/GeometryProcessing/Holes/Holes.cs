using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Holes
{
	/// <summary>
	/// Holds the result of an hole calculation.
	/// </summary>
	public class Holes
	{
		[NotNull]
		public IList<Polygon> HoleGeometries { get; }

		[CanBeNull]
		public GdbObjectReference? FeatureReference { get; }

		public Holes(IList<Polygon> holeGeometries,
		             [CanBeNull] GdbObjectReference? featureReference = null)
		{
			HoleGeometries = holeGeometries;
			FeatureReference = featureReference;
		}

		public int HoleCount => HoleGeometries.Count();

		public bool HasHoles()
		{
			return HoleCount > 0;
		}
	}
}
