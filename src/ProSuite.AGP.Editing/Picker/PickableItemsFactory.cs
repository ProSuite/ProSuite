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
		// NOTE: Hack! This cache doesn't invlidate if layer properties change.
		private static readonly HashSet<string> _layersWithExpression = new();

		public static IEnumerable<IPickableItem> CreateFeatureItems(
			[NotNull] IEnumerable<FeatureSelectionBase> selectionByClasses)
		{
			return selectionByClasses.SelectMany(CreatePickableFeatureItems);
		}

		public static IEnumerable<IPickableItem> CreateFeatureClassItems(
			[NotNull] IEnumerable<FeatureSelectionBase> selectionByClasses)
		{
			var itemsByName = new Dictionary<string, IPickableFeatureClassItem>();

			foreach (FeatureSelectionBase selection in selectionByClasses)
			{
				BasicFeatureLayer layer = selection.BasicFeatureLayer;
				bool isAnnotation = layer is AnnotationLayer;

				FeatureClass featureClass = selection.FeatureClass;
				string name = featureClass.GetName();

				if (itemsByName.ContainsKey(name))
				{
					IPickableFeatureClassItem item = itemsByName[name];
					item.Layers.Add(layer);
				}
				else
				{
					IPickableFeatureClassItem item = CreatePickableClassItem(selection, featureClass, isAnnotation);
					item.Layers.Add(layer);

					itemsByName.Add(name, item);
				}
			}

			return itemsByName.Values;
		}

		private static IPickableFeatureClassItem CreatePickableClassItem(
			FeatureSelectionBase selection, Dataset dataset, bool isAnnotation)
		{
			List<Feature> features = selection.GetFeatures().ToList();
			return isAnnotation
				       ? new PickableAnnotationFeatureClassItem(dataset, features)
				       : new PickableFeatureClassItem(dataset, features);
		}

		private static IEnumerable<IPickableItem> CreatePickableFeatureItems(
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

			if (layer is AnnotationLayer) { }

			return CreatePickableFeatureItems(classSelection, expressionExists);
		}

		private static IEnumerable<IPickableItem> CreatePickableFeatureItems(
			FeatureSelectionBase classSelection,
			bool expressionExists)
		{
			BasicFeatureLayer layer = classSelection.BasicFeatureLayer;
			bool isAnnotation = layer is AnnotationLayer;

			if (expressionExists)
			{
				foreach (var feature in classSelection.GetFeatures())
				{
					long oid = feature.GetObjectID();
					string expr = layer.GetDisplayExpressions(new[] { oid }).FirstOrDefault();

					if (! string.IsNullOrEmpty(expr) && ! string.IsNullOrWhiteSpace(expr))
					{
						yield return CreatePickableFeatureItem(
							layer, feature, oid, expr, isAnnotation);
					}
					else
					{
						yield return CreatePickableFeatureItem(
							layer, feature, feature.GetObjectID(),
							GdbObjectUtils.GetDisplayValue(feature), isAnnotation);
					}
				}
			}
			else
			{
				foreach (var feature in classSelection.GetFeatures())
				{
					yield return CreatePickableFeatureItem(layer, feature, feature.GetObjectID(),
					                                       GdbObjectUtils.GetDisplayValue(feature),
					                                       isAnnotation);
				}
			}
		}

		private static IPickableItem CreatePickableFeatureItem(BasicFeatureLayer layer,
		                                                       Feature feature, long oid,
		                                                       string expr,
		                                                       bool isAnnotation = false)
		{
			return isAnnotation
				       ? new PickableAnnotationFeatureItem(layer, feature, feature.GetShape(), oid,
				                                           expr)
				       : new PickableFeatureItem(layer, feature, feature.GetShape(), oid, expr);
		}
	}
}
