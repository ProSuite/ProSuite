using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Picker
{
	public static class PickableItemsFactory
	{
		public static IEnumerable<IPickableItem> CreateFeatureItems(
			[NotNull] IEnumerable<FeatureClassSelection> selectionByClasses)
		{
			return selectionByClasses.SelectMany(CreateFeatureItems);
		}

		public static IEnumerable<IPickableFeatureClassItem> CreateFeatureClassItems(
			[NotNull] IEnumerable<FeatureClassSelection> selectionByClasses)
		{
			var itemsByName = new Dictionary<string, IPickableFeatureClassItem>();

			foreach (FeatureClassSelection selection in selectionByClasses)
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
					BasicFeatureLayer layer = selection.BasicFeatureLayer;

					var item = new PickableFeatureClassItem(featureClass,
					                                        selection.ObjectIds,
					                                        layer.ShapeType,
					                                        new List<BasicFeatureLayer> { layer });

					itemsByName.Add(name, item);
				}
			}

			return itemsByName.Values;
		}

		private static IEnumerable<IPickableItem> CreateFeatureItems(
			[NotNull] FeatureClassSelection classSelection)
		{
			foreach (Feature feature in classSelection.GetFeatures())
			{
				string text = GetPickerItemText(feature, classSelection.BasicFeatureLayer);

				yield return new PickableFeatureItem(classSelection.BasicFeatureLayer, feature, text);
			}
		}

		private static string GetPickerItemText([NotNull] Feature feature,
		                                        [CanBeNull] MapMember layer = null)
		{
			// TODO: Alternatively allow using layer.QueryDisplayExpressions. But typically this is just the OID which is not very useful -> Requires configuration
			// string[] displayExpressions = layer.QueryDisplayExpressions(new[] { feature.GetObjectID() });

			string className = layer == null ? feature.GetTable().GetName() : layer.Name;

			return GdbObjectUtils.GetDisplayValue(feature, className);
		}
	}
}
