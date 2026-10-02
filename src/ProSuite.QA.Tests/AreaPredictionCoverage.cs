using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;

namespace ProSuite.QA.Tests
{
	internal static class AreaPredictionCoverage
	{
		public static bool IsCovered(IGeometry target, double targetArea,
		                             IEnumerable<IGeometry> candidates,
		                             double minimumOverlapRatio)
		{
			if (targetArea <= 0 || minimumOverlapRatio <= 0)
			{
				return true;
			}

			IGeometry covered = null;
			try
			{
				foreach (IGeometry candidate in candidates)
				{
					if (candidate == null || candidate.IsEmpty)
					{
						continue;
					}

					IGeometry intersection = IntersectionUtils.Intersect(
						target, candidate, esriGeometryDimension.esriGeometry2Dimension);
					try
					{
						if (intersection.IsEmpty)
						{
							continue;
						}

						IGeometry union = covered == null
							                  ? GeometryFactory.Clone(intersection)
							                  : ((ITopologicalOperator) covered).Union(intersection);
						ComUtils.ReleaseComObject(covered);
						covered = union;
						if (GeometryProperties.GetArea(covered) / targetArea >= minimumOverlapRatio)
						{
							return true;
						}
					}
					finally
					{
						ComUtils.ReleaseComObject(intersection);
					}
				}

				return false;
			}
			finally
			{
				ComUtils.ReleaseComObject(covered);
			}
		}
	}
}
