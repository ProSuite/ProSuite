using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;

namespace ProSuite.AGP.Editing.Selection
{
	public static class GeometryReducer
	{
		public static IEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> GetReducedset(
			Dictionary<BasicFeatureLayer, List<long>> featuresPerLayer)
		{
			var _sorted = featuresPerLayer.GroupBy(kvp => kvp.Key.ShapeType)
			                              .OrderBy(group => group.Key,
			                                       new GeometryTypeComparer());

			//get the first group of kvp's representing layers with the same shapeType..
			return _sorted.First().Select(el => el);
		}

		/// <summary>
		/// Reduces the specified selection to the selection classes with the lowest dimension (the classic picker behaviour).
		/// </summary>
		/// <param name="selectionByClass"></param>
		/// <returns></returns>
		public static IEnumerable<FeatureClassSelection> ReduceByGeometryDimension(
			IEnumerable<FeatureClassSelection> selectionByClass)
		{
			// Group by shape dimension to make sure {points, multipoints} and {polygons, multipatches} end up in the same group:
			var shapeGroups = selectionByClass
			                  .GroupBy(classSelection => classSelection.ShapeDimension)
			                  .OrderBy(group => group.Key);

			// Get the first group representing FeatureClassSelections with the same shape dimension:
			return shapeGroups.First().Select(fcs => fcs);
		}

		public static bool ContainsManyFeatures(
			IEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> candidates)
		{
			//several features of different layers
			if (candidates.Count() > 1)
			{
				return true;
			}

			//several features of the same layer
			if (candidates.First().Value.Count > 1)
			{
				return true;
			}

			return false;
		}
	}
}
