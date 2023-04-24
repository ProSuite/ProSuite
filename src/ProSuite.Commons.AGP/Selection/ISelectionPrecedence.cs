using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Carto;

namespace ProSuite.Commons.AGP.Selection
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
		public int Score { get; set; }
	}
}
