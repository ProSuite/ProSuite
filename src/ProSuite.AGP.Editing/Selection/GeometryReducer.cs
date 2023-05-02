using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Selection
{
	// todo daro refactor
	public static class GeometryReducer
	{
		public static IEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> GetReducedset(
			Dictionary<BasicFeatureLayer, List<long>> featuresPerLayer)
		{
			// todo daro: rename
			var _sorted = featuresPerLayer.GroupBy(kvp => kvp.Key.ShapeType)
			                              .OrderBy(group => group.Key,
			                                       new GeometryTypeComparer());

			//get the first group of kvp's representing layers with the same shapeType..
			return _sorted.First().Select(el => el);
		}

		// todo daro to picker utils?
		public static IEnumerable<FeatureClassSelection> OrderByGeometryDimension(
			IEnumerable<FeatureClassSelection> selectionSets)
		{
			return selectionSets
			       .GroupBy(classSelection => classSelection.ShapeDimension)
			       .OrderBy(group => group.Key).SelectMany(fcs => fcs);
		}

		/// <summary>
		/// Reduces the specified selection to the selection classes with the lowest dimension (the classic picker behaviour).
		/// </summary>
		/// <param name="selectionSets"></param>
		/// <returns></returns>
		public static IEnumerable<FeatureClassSelection> ReduceByGeometryDimension(
			IEnumerable<FeatureClassSelection> selectionSets)
		{
			// Group by shape dimension to make sure {points, multipoints} and {polygons, multipatches} end up in the same group:
			var shapeGroups = selectionSets
			                  .GroupBy(classSelection => classSelection.ShapeDimension)
			                  .OrderBy(group => group.Key);

			// Get the first group representing FeatureClassSelections with the same shape dimension:
			return shapeGroups.First().Select(fcs => fcs);
		}

		// todo daro to SelectionUtils, extension method?
		public static int GetFeatureCount(
			IEnumerable<FeatureClassSelection> selectionSets)
		{
			return selectionSets.Sum(set => set.FeatureCount);
		}

		public static bool ContainsOneFeature(
			IEnumerable<FeatureClassSelection> selectionSets)
		{
			return selectionSets.Sum(set => set.FeatureCount) == 1;
		}

		public static bool ContainsManyFeatures(
			IEnumerable<FeatureClassSelection> selectionSets)
		{
			return selectionSets.Sum(set => set.FeatureCount) > 1;
		}

		public static IEnumerable<Feature> ReduceRelativeToSelectionGeometry(
			IEnumerable<Feature> candidates, [NotNull] Geometry sketchGeometry)
		{
			return candidates
			       .OrderBy(feature => feature, new DistanceToGeometryComparer(sketchGeometry))
			       .Select(candidate => candidate);
		}

		//public static IEnumerable<Feature> ReduceRelativeToSelectionGeometry(
		//	IEnumerable<FeatureClassSelection> candidates, [NotNull] Geometry sketchGeometry)
		//{
		//	IOrderedEnumerable<Feature> orderedFeatures =
		//		candidates.Select(candidate => candidate.GetFeatures())
		//		          .SelectMany(feature => feature)
		//		          .OrderBy(feature => feature, new DistanceToGeometryComparer(sketchGeometry));

		//	return orderedFeatures.Select(o => o);
		//}
	}

	public class DistanceToGeometryComparer : IComparer<Feature>
	{
		private readonly Geometry _sketchGeometry;

		public DistanceToGeometryComparer([NotNull] Geometry sketchGeometry)
		{
			_sketchGeometry = sketchGeometry;
		}

		public int Compare(Feature x, Feature y)
		{
			if (x == null)
			{
				return -1;
			}

			if (y == null)
			{
				return 1;
			}

			double xToSketch = GeometryEngine.Instance.Distance(x.GetShape(), _sketchGeometry);
			double yToSketch = GeometryEngine.Instance.Distance(y.GetShape(), _sketchGeometry);

			if (xToSketch < yToSketch)
			{
				return -1;
			}

			if (xToSketch > yToSketch)
			{
				return 1;
			}

			return 0;
		}
	}
}
