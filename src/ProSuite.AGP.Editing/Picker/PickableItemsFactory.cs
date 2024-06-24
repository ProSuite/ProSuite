using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Picker
{
	public static class PickableItemsFactory
	{
		public static IEnumerable<IPickableItem> CreateFeatureItems(
			[NotNull] IEnumerable<FeatureSelectionBase> selectionByClasses)
		{
			return selectionByClasses.SelectMany(CreateFeatureItems);
		}

		public static IEnumerable<IPickableFeatureClassItem> CreateFeatureClassItems(
			[NotNull] IEnumerable<FeatureSelectionBase> selectionByClasses)
		{
			var itemsByName = new Dictionary<string, IPickableFeatureClassItem>();

			foreach (FeatureSelectionBase selection in selectionByClasses)
			{
				FeatureClass featureClass = selection.FeatureClass;
				string name = featureClass.GetName();

				if (itemsByName.ContainsKey(name))
				{
					IPickableFeatureClassItem item = itemsByName[name];

					item.Layers.Add(selection.BasicFeatureLayer);
				}
				else
				{
					var item = new PickableFeatureClassItem(featureClass,
					                                        selection.GetFeatures().ToList());

					item.Layers.Add(selection.BasicFeatureLayer);

					itemsByName.Add(name, item);
				}
			}

			return itemsByName.Values;
		}

		// NOTE: Hack! This cache doesn't invlidate if layer properties change.
		private static readonly HashSet<string> _layersWithExpression = new();

		private static IEnumerable<IPickableItem> CreateFeatureItems(
			[NotNull] FeatureSelectionBase classSelection)
		{
			BasicFeatureLayer layer = classSelection.BasicFeatureLayer;

			bool expressionExists = false;
			if (! _layersWithExpression.Contains(layer.URI))
			{
				var cimLayer = (CIMBasicFeatureLayer) layer.GetDefinition();

				if (cimLayer.FeatureTable.DisplayExpressionInfo != null)
				{
					_layersWithExpression.Add(layer.URI);
					expressionExists = true;
				}
			}
			else if (_layersWithExpression.Contains(layer.URI))
			{
				expressionExists = true;
			}

			foreach (Feature feature in classSelection.GetFeatures())
			{
				if (expressionExists)
				{
					long oid = feature.GetObjectID();
					string expr = layer.GetDisplayExpressions(new[] { oid }).FirstOrDefault();

					if (! string.IsNullOrEmpty(expr) && ! string.IsNullOrWhiteSpace(expr))
					{
						yield return new PickableFeatureItem(layer, feature, feature.GetShape(),
						                                     oid, expr);
						continue;
					}
				}

				yield return new PickableFeatureItem(layer, feature, feature.GetShape(),
				                                     feature.GetObjectID(),
				                                     GdbObjectUtils.GetDisplayValue(feature));
			}
		}
	}
}
