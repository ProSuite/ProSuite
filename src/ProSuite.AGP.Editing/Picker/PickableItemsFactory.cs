using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Picker
{
	public static class PickableItemsFactory
	{
		public static IList<IPickableItem> CreateFeatureClassItems(
			[NotNull] IEnumerable<FeatureClassSelection> selectionByClasses)
		{
			if (selectionByClasses == null)
			{
				throw new ArgumentNullException(nameof(selectionByClasses));
			}

			List<FeatureClassInfo> featureClassInfos =
				GetSelectableFeatureClassInfos(selectionByClasses).ToList();

			return featureClassInfos.Select(
				                        info =>
					                        new PickableFeatureClassItem(
						                        info.FeatureClass, info.ShapeType,
						                        info.BelongingLayers))
			                        .Cast<IPickableItem>()
			                        .ToList();
		}

		public static IEnumerable<IPickableItem> CreateFeatureItems(
			[NotNull] IEnumerable<FeatureClassSelection> selectionByClasses)
		{
			return selectionByClasses.SelectMany(CreateFeatureItems);
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
		                                        [CanBeNull] BasicFeatureLayer layer = null)
		{
			// TODO: Alternatively allow using layer.QueryDisplayExpressions. But typically this is just the OID which is not very useful -> Requires configuration
			// string[] displayExpressions = layer.QueryDisplayExpressions(new[] { feature.GetObjectID() });

			string className = layer == null ? feature.GetTable().GetName() : layer.Name;

			return GdbObjectUtils.GetDisplayValue(feature, className);
		}

		private static IEnumerable<FeatureClassInfo> GetSelectableFeatureClassInfos(
			[NotNull] IEnumerable<FeatureClassSelection> selectionsByLayer)
		{
			IEnumerable<IGrouping<string, FeatureClassSelection>> layerGroupsByClass =
				selectionsByLayer.GroupBy(layerSelection => layerSelection.FeatureClass.GetName());

			var featureClassInfos = new List<FeatureClassInfo>();

			foreach (IGrouping<string, FeatureClassSelection> group in layerGroupsByClass)
			{
				var belongingLayers = new List<BasicFeatureLayer>();

				foreach (FeatureClassSelection layerSelection in group)
				{
					belongingLayers.Add(layerSelection.BasicFeatureLayer);
				}

				FeatureClass fClass = group.First().FeatureClass;
				string featureClassName = fClass.GetName();
				esriGeometryType gType = belongingLayers.First().ShapeType;

				var featureClassInfo = new FeatureClassInfo
				                       {
					                       BelongingLayers = belongingLayers,
					                       FeatureClass = fClass,
					                       FeatureClassName = featureClassName,
					                       ShapeType = gType
				                       };
				featureClassInfos.Add(featureClassInfo);
			}

			return featureClassInfos.OrderBy(info => info.ShapeType);
		}

		private class FeatureClassInfo
		{
			public List<BasicFeatureLayer> BelongingLayers { get; set; }
			public FeatureClass FeatureClass { get; set; }
			public string FeatureClassName { get; set; }
			public esriGeometryType ShapeType { get; set; }
			public List<Feature> SelectionCandidates { get; set; }
		}
	}
}
