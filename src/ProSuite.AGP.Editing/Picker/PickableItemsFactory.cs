using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.PickerUI;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Picker
{
	public static class PickableItemsFactory
	{
		// NOTE: Hack! This cache doesn't invlidate if layer properties change.
		private static readonly HashSet<string> _layersWithExpression = new();

		public static IEnumerable<IPickableItem> CreateFeatureClassItems(
			[NotNull] IEnumerable<FeatureSelectionBase> selectionByClasses)
		{
			var itemsByName = new Dictionary<string, IPickableFeatureClassItem>();

			foreach (FeatureSelectionBase selection in selectionByClasses)
			{
				try
				{
					BasicFeatureLayer layer = selection.BasicFeatureLayer;
					bool isAnnotation = layer is AnnotationLayer;

					Table featureClass = selection.Table;
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
				finally
				{
					selection.Dispose();
				}
			}

			return itemsByName.Values;
		}

		private static IPickableFeatureClassItem CreatePickableClassItem(
			TableSelection selection, Dataset dataset, bool isAnnotation)
		{
			using Table table = selection.Table;
			var features = GdbQueryUtils.GetFeatures(table, selection.GetOids(), null, false)
			                            .ToList();

			return isAnnotation
				       ? new PickableAnnotationFeatureClassItem(dataset, features)
				       : new PickableFeatureClassItem(dataset, features);
		}

		public static IEnumerable<IPickableItem> CreatePickableFeatureItems(
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
				foreach (var feature in GetFeatures(classSelection))
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
				foreach (var feature in GetFeatures(classSelection))
				{
					yield return CreatePickableFeatureItem(layer, feature, feature.GetObjectID(),
					                                       GdbObjectUtils.GetDisplayValue(feature),
					                                       isAnnotation);
				}
			}
		}

		private static IEnumerable<Feature> GetFeatures(TableSelection classSelection)
		{
			using Table table = classSelection.Table;
			return GdbQueryUtils.GetFeatures(table, classSelection.GetOids(), null, false);
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

	public class PickableFeatureClassItemsFactory : IPickableItemsFactory
	{
		public IEnumerable<IPickableItem> CreateItems(IEnumerable<TableSelection> candidates)
		{
			return PickableItemsFactory.CreateFeatureClassItems(
				candidates.OfType<FeatureSelectionBase>());
		}

		public IPickerViewModel CreateViewModel(Geometry selectionGeometry)
		{
			return new PickerViewModel(selectionGeometry);
		}
	}

	public class PickableFeatureItemsFactory : IPickableItemsFactory
	{
		public IEnumerable<IPickableItem> CreateItems(IEnumerable<TableSelection> candidates)
		{
			return candidates.OfType<FeatureSelectionBase>().SelectMany(PickableItemsFactory.CreatePickableFeatureItems);
		}

		public IPickerViewModel CreateViewModel(Geometry selectionGeometry)
		{
			return new PickerViewModel(selectionGeometry);
		}
	}
}
