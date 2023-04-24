using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;

namespace ProSuite.Commons.AGP.Selection
{
	public class SimpleSelectionScoreComparer : IComparer<SelectionScore>
	{
		public int Compare(SelectionScore x, SelectionScore y)
		{
			if (x == y)
			{
				return 0;
			}

			if (x == null)
			{
				return -1;
			}

			if (y == null)
			{
				return 1;
			}

			if (x.Score < y.Score)
			{
				return -1;
			}

			if (x.Score > y.Score)
			{
				return 1;
			}

			return 0;
		}
	}

	public class DistanceToStartEndpointComparer : IComparer<SelectionScore>
	{
		private readonly Geometry _referenceGeometry;

		public DistanceToStartEndpointComparer(Geometry referenceGeometry)
		{
			_referenceGeometry = referenceGeometry;
		}

		public int Compare(SelectionScore x, SelectionScore y)
		{
			if (x == y)
			{
				return 0;
			}

			if (x == null)
			{
				return -1;
			}

			if (y == null)
			{
				return 1;
			}

			if (!(x.Geometry is Multipart xMultipart))
			{
				return -1;
			}

			if (!(y.Geometry is Multipart yMultipart))
			{
				return 1;
			}

			double xDistance = SumDistancesStartEndPoint(xMultipart, _referenceGeometry);
			double yDistance = SumDistancesStartEndPoint(yMultipart, _referenceGeometry);

			if (xDistance < yDistance)
			{
				return -1;
			}

			if (xDistance > yDistance)
			{
				return 1;
			}

			return 0;
		}

		public static double SumDistancesStartEndPoint(Multipart multipart, Geometry referenceGeometry)
		{
			double distanceToStartPoint = GetDistanceToPoint(referenceGeometry, GeometryUtils.GetStartPoint(multipart));

			double distanceToEndPoint = GetDistanceToPoint(referenceGeometry, GeometryUtils.GetEndPoint(multipart));

			return distanceToStartPoint + distanceToEndPoint;
		}

		public static double GetDistanceToPoint(Geometry referenceGeometry, MapPoint mapPoint)
		{
			double distanceToStartPoint = GeometryEngine.Instance.Distance(referenceGeometry, mapPoint);
			return distanceToStartPoint;
		}
	}
}
