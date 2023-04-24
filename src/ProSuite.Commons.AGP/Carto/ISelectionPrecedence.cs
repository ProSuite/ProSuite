using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	public interface ISelectionPrecedence
	{
		IEnumerable<FeatureClassSelection> ReduceByGeometryDimension(
			IEnumerable<FeatureClassSelection> selectionByLayer);

		IEnumerable<FeatureClassSelection> Ignore(
			IEnumerable<FeatureClassSelection> selectionByLayer);

		IEnumerable<FeatureClassSelection> OrderBy(IEnumerable<FeatureClassSelection> selectionByLayer, FeatureClassSelectionComparer comparer);

		IEnumerable<Feature> OrderFeaturesBy(IEnumerable<FeatureClassSelection> selectionByLayer,
		                                     IComparer<Feature> comparer);
	}

	public class StandardSelectionPrecedence : ISelectionPrecedence
	{
		public IList<string> IgnoreList { get; }

		public IEnumerable<FeatureClassSelection> ReduceByGeometryDimension(IEnumerable<FeatureClassSelection> selectionByLayer)
		{
			// Group by shape dimension to make sure {points, multipoints} and {polygons, multipatches} end up in the same group:
			var shapeGroups = selectionByLayer
			                  .GroupBy(classSelection => classSelection.ShapeDimension)
			                  .OrderBy(group => group.Key);

			// Get the first group representing FeatureClassSelections with the same shape dimension:
			return shapeGroups.First().Select(fcs => fcs);
		}

		public IEnumerable<FeatureClassSelection> Ignore(IEnumerable<FeatureClassSelection> selectionByLayer)
		{
			//foreach (FeatureClassSelection selection in selectionByLayer)
			//{
			//	BasicFeatureLayer layer = selection.BasicFeatureLayer;

			//	if (layer == null)
			//	{
			//		continue;
			//	}

			//	// exception?
			//	if (IgnoreList.Any(ignore => layer.Name.StartsWith(ignore)))
			//	{
			//		continue;
			//	}

			//	yield return selection;
			//}

			throw new NotImplementedException();
		}

		public IEnumerable<FeatureClassSelection> OrderBy(IEnumerable<FeatureClassSelection> selectionByLayer, FeatureClassSelectionComparer comparer)
		{
			return selectionByLayer.OrderBy(selection => selection, comparer).Select(item => item);
		}

		public IEnumerable<Feature> OrderFeaturesBy(
			IEnumerable<FeatureClassSelection> selectionByLayer, IComparer<Feature> comparer)
		{
			IEnumerable<Feature> features = selectionByLayer.SelectMany(layer => layer.GetFeatures());

			return features.OrderBy(feature => feature, comparer).Select(item => item);
		}
	}

	public class SelectionScore
	{
		public Feature Feature { get; set; }

		public Geometry Geometry { get; set; }
	}

	public static class MyClass
	{
		public static void Foo()
		{
			var list = new List<FeatureClassSelection>();

			var selectionPrecedence = new StandardSelectionPrecedence()
			                          {
				                          IgnoreList = { "KRM" }
			                          };

			IEnumerable<FeatureClassSelection> v0 = selectionPrecedence.Ignore(list);

			//IComparer<FeatureClassSelection> baseCompare = new DistanceToStartEndpointComparer1(null);
			//IEnumerable<FeatureClassSelection> v1 = selectionPrecedence.OrderBy(v0, new FeatureClassSelectionComparer(baseCompare));

			IEnumerable<Feature> orderedFeatures = selectionPrecedence.OrderFeaturesBy(v0, new DistanceToStartEndpointComparer1(null));
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

			double xDistance = GetDistance(xMultipart, _referenceGeometry);
			double yDistance = GetDistance(yMultipart, _referenceGeometry);

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

		private static double GetDistance(Multipart multipart, Geometry referenceGeometry)
		{
			MapPoint startPoint = GeometryUtils.GetStartPoint(multipart);
			MapPoint endPoint = GeometryUtils.GetEndPoint(multipart);

			double distanceToStartPoint = GeometryEngine.Instance.Distance(referenceGeometry, startPoint);
			double distanceToEndPoint = GeometryEngine.Instance.Distance(referenceGeometry, endPoint);

			return distanceToStartPoint + distanceToEndPoint;
		}
	}

	public class DistanceToStartEndpointComparer1 : IComparer<Feature>
	{
		private readonly Geometry _referenceGeometry;

		public DistanceToStartEndpointComparer1(Geometry referenceGeometry)
		{
			_referenceGeometry = referenceGeometry;
		}

		public int Compare(Feature x, Feature y)
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

			if (! (x.GetShape() is Multipart xMultipart))
			{
				return -1;
			}

			if (! (y.GetShape() is Multipart yMultipart))
			{
				return 1;
			}

			double xDistance = GetDistance(xMultipart, _referenceGeometry);
			double yDistance = GetDistance(yMultipart, _referenceGeometry);

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

		private static double GetDistance(Multipart multipart, Geometry referenceGeometry)
		{
			MapPoint startPoint = GeometryUtils.GetStartPoint(multipart);
			MapPoint endPoint = GeometryUtils.GetEndPoint(multipart);

			double distanceToStartPoint = GeometryEngine.Instance.Distance(referenceGeometry, startPoint);
			double distanceToEndPoint = GeometryEngine.Instance.Distance(referenceGeometry, endPoint);

			return distanceToStartPoint + distanceToEndPoint;
		}
	}

	public class DistanceToStartEndpointComparer0 : IComparer<FeatureClassSelection>
	{
		[NotNull] private readonly Geometry _referenceGeometry;

		public DistanceToStartEndpointComparer0([NotNull] Geometry referenceGeometry)
		{
			_referenceGeometry = referenceGeometry;
		}

		public int Compare(FeatureClassSelection x, FeatureClassSelection y)
		{
			//IEnumerable<Feature> xFeatures = x.GetFeatures();

			//foreach (Feature feature in xFeatures)
			//{
			//	var multipart = (Multipart) feature.GetShape();

			//	MapPoint startPoint = multipart.Points[0];
			//	MapPoint endPoint = multipart.Points[startPoint.PointCount - 1];
			//}

			//foreach (Feature feature in xFeatures)
			//{
			//	var polyline = (Polyline) feature.GetShape();
			//	ReadOnlyPartCollection parts = polyline.Parts;
			//	ReadOnlySegmentCollection firstSegments = parts.First();

			//	Segment first = firstSegments.First();
			//	MapPoint firstStartPoint = first.StartPoint;
			//}

			//foreach (Feature feature in y.GetFeatures())
			//{
			//	var polygon = (Polygon)feature.GetShape();
			//	ReadOnlyPartCollection parts = polygon.Parts;
			//	ReadOnlySegmentCollection lastSegments = parts.Last();
			//	Segment last = lastSegments.Last();
			//	MapPoint lastEndPoint = last.EndPoint;
			//}

			return 0;
		}
	}
}
