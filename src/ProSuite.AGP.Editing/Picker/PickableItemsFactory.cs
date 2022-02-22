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
		public static List<IPickableItem> CreateFeatureClassItems(
			[NotNull] IEnumerable<FeatureClassSelection> selectionByClasses)
		{
			var pickableItems = new List<IPickableItem>();

			List<FeatureClassInfo> featureClassInfos = GetFcCandidates(selectionByClasses);

			foreach (FeatureClassInfo info in featureClassInfos)
			{
				pickableItems.Add(
					new PickableFeatureClassItem(info.FeatureClass, info.ShapeType,
					                             info.BelongingLayers));
			}

			return pickableItems;
		}

		public static List<IPickableItem> CreateFeatureItems(
			IEnumerable<FeatureClassSelection> selectionByClasses)
		{
			var pickCandidates = new List<IPickableItem>();

			foreach (FeatureClassSelection classSelection in selectionByClasses)
			{
				pickCandidates.AddRange(CreateFeatureItems(classSelection));
			}

			return pickCandidates;
		}

		public static IEnumerable<IPickableItem> CreateFeatureItems(
			[NotNull] FeatureClassSelection classSelection)
		{
			foreach (Feature feature in classSelection.GetFeatures())
			{
				string text = GetPickerItemText(feature, classSelection.FeatureLayer);

				yield return new PickableFeatureItem(classSelection.FeatureLayer, feature, text);
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

		private static List<FeatureClassInfo> GetFcCandidates(
			[NotNull] IEnumerable<FeatureClassSelection> candidatesOfManyLayers)
		{
			IEnumerable<FeatureClassInfo> featureClassInfos =
				GetSelectableFeatureClassInfos();

			List<BasicFeatureLayer> candidateLayers =
				candidatesOfManyLayers.Select(c => c.FeatureLayer).ToList();

			return featureClassInfos.Where(fcInfo =>
			{
				return fcInfo.BelongingLayers.Any(
					layer => candidateLayers.Contains(layer));
			}).ToList();
		}

		private static IEnumerable<FeatureClassInfo> GetSelectableFeatureClassInfos()
		{
			IEnumerable<FeatureLayer> featureLayers = MapView.Active.Map.GetLayersAsFlattenedList()
			                                                 .OfType<FeatureLayer>()
			                                                 .Where(layer => layer.IsVisible);

			IEnumerable<IGrouping<string, FeatureLayer>> layerGroupsByFcName =
				featureLayers.GroupBy(layer => layer.GetFeatureClass().GetName());

			var featureClassInfos = new List<FeatureClassInfo>();

			foreach (IGrouping<string, FeatureLayer> group in layerGroupsByFcName)
			{
				var belongingLayers = new List<FeatureLayer>();

				foreach (FeatureLayer layer in group)
				{
					belongingLayers.Add(layer);
				}

				FeatureClass fClass = belongingLayers.First().GetFeatureClass();
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
			public List<FeatureLayer> BelongingLayers { get; set; }
			public FeatureClass FeatureClass { get; set; }
			public string FeatureClassName { get; set; }
			public esriGeometryType ShapeType { get; set; }
			public List<Feature> SelectionCandidates { get; set; }
		}
	}
}
