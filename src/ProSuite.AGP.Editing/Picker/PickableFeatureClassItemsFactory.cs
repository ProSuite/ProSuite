using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.PickerUI;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Picker;

public class PickableFeatureClassItemsFactory : IPickableItemsFactory
{
	public IEnumerable<IPickableItem> CreateItems(IEnumerable<TableSelection> candidates)
	{
		return CreateFeatureClassItems(candidates.OfType<FeatureSelectionBase>());
	}

	public IPickerViewModel CreateViewModel(Geometry selectionGeometry)
	{
		return new PickerViewModel(selectionGeometry);
	}

	private static IEnumerable<IPickableItem> CreateFeatureClassItems(
		[NotNull] IEnumerable<FeatureSelectionBase> selectionByClasses)
	{
		var itemsByName = new Dictionary<string, IPickableFeatureClassItem>();

		foreach (FeatureSelectionBase selection in selectionByClasses)
		{
			try
			{
				BasicFeatureLayer layer = selection.BasicFeatureLayer;
				bool isAnnotation = layer is AnnotationLayer;

				// todo daro: use layer.Name > FeatureSelectionBase is
				// is it IPickableFeatureClassItem or IPickableLayerItem?
				// if later change IPickableFeatureClassItem.Layers to Layer
				Table featureClass = selection.Table;
				string name = featureClass.GetName();

				if (itemsByName.ContainsKey(name))
				{
					IPickableFeatureClassItem item = itemsByName[name];
					item.Layers.Add(layer);
				}
				else
				{
					IPickableFeatureClassItem item =
						CreatePickableClassItem(selection, isAnnotation);
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
		TableSelection selection, bool isAnnotation)
	{
		using Table table = selection.Table;
		var features = GdbQueryUtils.GetFeatures(table, selection.GetOids(), null, false).ToList();

		string datasetName = table.GetName();
		var oids = features.Select(feature => feature.GetObjectID()).ToList();
		var geometry = GeometryUtils.Union(features.Select(feature => feature.GetShape()).ToList());

		return isAnnotation
			       ? new PickableAnnotationFeatureClassItem(datasetName, oids, geometry)
			       : new PickableFeatureClassItem(datasetName, oids, geometry);
	}
}
