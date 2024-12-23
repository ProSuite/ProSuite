using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.PickerUI;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Picker;

public class PickableFeatureItemsFactory : IPickableItemsFactory
{
	// NOTE: Hack! This cache doesn't invalidate if layer properties change.
	private static readonly HashSet<string> _layersWithExpression = new();

	public IEnumerable<IPickableItem> CreateItems(IEnumerable<TableSelection> candidates)
	{
		return candidates.OfType<FeatureSelectionBase>()
		                 .SelectMany(CreatePickableFeatureItems);
	}

	public IPickerViewModel CreateViewModel(Geometry selectionGeometry)
	{
		return new PickerViewModel(selectionGeometry);
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
				string expr = layer.GetDisplayExpressions([oid]).FirstOrDefault();

				string displayValue = LayerUtils.GetMeaningfulDisplayExpression(feature, expr);

				if (! string.IsNullOrEmpty(expr) && ! string.IsNullOrWhiteSpace(expr))
				{
					yield return CreatePickableFeatureItem(
						layer, feature, oid, displayValue, isAnnotation);
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
