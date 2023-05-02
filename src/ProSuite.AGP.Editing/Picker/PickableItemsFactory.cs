using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
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
					                                        selection.GetOids().ToList());

					item.Layers.Add(selection.BasicFeatureLayer);

					itemsByName.Add(name, item);
				}
			}

			return itemsByName.Values;
		}

		private static IEnumerable<IPickableItem> CreateFeatureItems(
			[NotNull] FeatureSelectionBase classSelection)
		{
			return classSelection.GetFeatures()
			                     .Select(feature =>
				                             new PickableFeatureItem(
					                             classSelection.BasicFeatureLayer, feature));
		}
	}
}
