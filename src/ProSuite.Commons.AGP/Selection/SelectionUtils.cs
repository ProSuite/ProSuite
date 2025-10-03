using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Selection
{
	public static class SelectionUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static void ClearSelection(Map map)
		{
			map?.ClearSelection();
		}

		/// <summary>Select the given <paramref name="feature"/>
		/// on the given <paramref name="layer"/>, if possible</summary>
		/// <returns>true iff the feature was selected</returns>
		/// <remarks>Must run on MCT</remarks>
		public static bool SelectFeature(BasicFeatureLayer layer, Feature feature)
		{
			if (layer is null) return false;
			if (feature is null) return false;
			if (IsSelected(layer, feature))
				return true; // already selected
			long oid = feature.GetObjectID();
			var filter = new QueryFilter { ObjectIDs = new[] { oid } };
			using var selection = layer.Select(filter);
			return IsSelected(selection, feature);
		}

		/// <summary>Select the given <paramref name="feature"/> the first layer of the given
		/// <paramref name="mapView"/> that is visible, selectable, and actually show the feature
		/// (i.e. the layer has preferably no definition query)</summary>
		/// <returns>true if the feature is selected or was already selected.</returns>
		/// <remarks>Must run on MCT</remarks>
		public static bool SelectFeature(
			[NotNull] MapView mapView,
			[NotNull] Feature feature,
			SelectionCombinationMethod method = SelectionCombinationMethod.Add)
		{
			Assert.ArgumentNotNull(mapView, nameof(mapView));
			Assert.ArgumentNotNull(feature, nameof(feature));

			long oid = feature.GetObjectID();
			var filter = new QueryFilter { ObjectIDs = new[] { oid } };

			FeatureClass featureClass = feature.GetTable();

			Predicate<IDisplayTable> usesSameClass =
				layer => SameFeatureClass(layer.GetTable() as FeatureClass, featureClass);

			long selectionCount = SelectFeatures(mapView, filter, method, usesSameClass);

			return selectionCount > 0;
		}

		private static bool SameFeatureClass(FeatureClass a, FeatureClass b)
		{
			if (a is null || b is null) return false;
			if (ReferenceEquals(a, b)) return true;
			if (a.Handle == b.Handle) return true;
			// A table's Path is a URI of the form C:\Path\To\File.gdb\Dataset or C:\Path\To\Conn.sde\Dataset
			// If two different connection files (.sde) point to the same database and version, our approach
			// here considers it different, a false negative... but probably good enough for now
			Uri aUri = a.GetPath();
			Uri bUri = b.GetPath();
			return aUri.Equals(bUri);
		}

		public static void SelectFeature([NotNull] BasicFeatureLayer basicFeatureLayer,
		                                 SelectionCombinationMethod selectionMethod,
		                                 long objectId)
		{
			SelectRows(basicFeatureLayer, selectionMethod, new[] { objectId });
		}

		/// <summary>
		/// Selects the requested features or rows from the specified layers or stand-alone tables.
		/// Selections are only performed on visible selectable layers, preferably on the first
		/// layer or table without definition query.
		/// </summary>
		/// <returns>The number of actually selected rows.</returns>
		public static long SelectRows([NotNull] MapView mapView,
		                              [NotNull] Predicate<IDisplayTable> mapMemberPredicate,
		                              [NotNull] IReadOnlyList<long> objectIds)
		{
			if (objectIds.Count == 0)
			{
				return 0;
			}

			var queryFilter = new QueryFilter { ObjectIDs = objectIds };

			SelectionCombinationMethod combinationMethod = SelectionCombinationMethod.Add;

			return SelectRows(mapView, queryFilter, combinationMethod, mapMemberPredicate);
		}

		/// <summary>
		/// Selects the requested features or rows from the specified layers or stand-alone tables.
		/// Selections are only performed on visible selectable layers, preferably on the first
		/// layer or table without definition query.
		/// </summary>
		/// <returns>The number of actually selected rows.</returns>
		public static long SelectRows(
			[NotNull] MapView mapView,
			[NotNull] QueryFilter queryFilter,
			SelectionCombinationMethod combinationMethod = SelectionCombinationMethod.Add,
			[CanBeNull] Predicate<IDisplayTable> mapMemberPredicate = null)
		{
			long totalSelected =
				SelectFeatures(mapView, queryFilter, combinationMethod, mapMemberPredicate);

			totalSelected +=
				SelectStandaloneTableRows(mapView, queryFilter, combinationMethod,
				                          mapMemberPredicate);

			return totalSelected;
		}

		private static long SelectFeatures(
			[NotNull] MapView mapView,
			[NotNull] QueryFilter queryFilter,
			SelectionCombinationMethod combinationMethod = SelectionCombinationMethod.Add,
			[CanBeNull] Predicate<IDisplayTable> mapMemberPredicate = null)
		{
			long totalSelected = 0;

			Predicate<BasicFeatureLayer> layerPredicate =
				l => l is IDisplayTable displayTable &&
				     (mapMemberPredicate == null || mapMemberPredicate(displayTable));

			foreach (BasicFeatureLayer featureLayer in
			         MapUtils.GetFeatureLayersForSelection(mapView, layerPredicate))
			{
				totalSelected +=
					SelectRows(featureLayer, combinationMethod, queryFilter);
			}

			return totalSelected;
		}

		private static long SelectStandaloneTableRows(
			[NotNull] MapView mapView,
			[NotNull] QueryFilter queryFilter,
			SelectionCombinationMethod combinationMethod = SelectionCombinationMethod.Add,
			[CanBeNull] Predicate<IDisplayTable> mapMemberPredicate = null)
		{
			long totalSelected = 0;

			Predicate<StandaloneTable> tablePredicate =
				t => t is IDisplayTable displayTable &&
				     (mapMemberPredicate == null || mapMemberPredicate(displayTable));

			foreach (StandaloneTable standaloneTable in
			         MapUtils.GetStandaloneTablesForSelection(mapView.Map, tablePredicate))
			{
				totalSelected +=
					SelectRows(standaloneTable, combinationMethod, queryFilter);
			}

			return totalSelected;
		}

		/// <summary>
		/// Selects the requested features or rows from the specified layer or stand-alone table
		/// and immediately disposes the selection to avoid selection and immediate de-selection
		/// (for selection method XOR) because it is called in 2 threads.
		/// </summary>
		/// <param name="tableBasedMapMember"></param>
		/// <param name="combinationMethod"></param>
		/// <param name="objectIds"></param>
		/// <returns>The number of actually selected rows.</returns>
		public static long SelectRows([NotNull] IDisplayTable tableBasedMapMember,
		                              SelectionCombinationMethod combinationMethod,
		                              [NotNull] IReadOnlyList<long> objectIds)
		{
			if (objectIds.Count == 0)
			{
				return 0;
			}

			var queryFilter = new QueryFilter { ObjectIDs = objectIds };

			return SelectRows(tableBasedMapMember, combinationMethod, queryFilter);
		}

		/// <summary>
		/// Selects the requested features or rows from the specified layer or stand-alone table
		/// and immediately disposes the selection to avoid selection and immediate de-selection
		/// (for selection method XOR) because it is called in 2 threads.
		/// </summary>
		/// <param name="tableBasedMapMember"></param>
		/// <param name="combinationMethod"></param>
		/// <param name="queryFilter"></param>
		/// <returns>The number of actually selected rows.</returns>
		public static long SelectRows([NotNull] IDisplayTable tableBasedMapMember,
		                              SelectionCombinationMethod combinationMethod,
		                              [CanBeNull] QueryFilter queryFilter)
		{
			using var selection =
				tableBasedMapMember.Select(queryFilter, combinationMethod);

			long actualSelectionCount = selection.GetCount();

			LogFeatureSelection(tableBasedMapMember, selection, actualSelectionCount);

			return actualSelectionCount;
		}

		public static long SelectFeatures([NotNull] IEnumerable<Feature> features,
		                                  [NotNull] IList<BasicFeatureLayer> inLayers,
		                                  [CanBeNull] CancelableProgressor progressor = null)
		{
			long result = 0;

			foreach (IGrouping<IntPtr, Feature> featuresByClassHandle in features.GroupBy(
				         f => f.GetTable().Handle))
			{
				if (progressor is { CancellationToken.IsCancellationRequested: true })
				{
					_msg.Debug("Select features canceled");
					break;
				}

				long classHandle = featuresByClassHandle.Key.ToInt64();

				List<long> objectIds = featuresByClassHandle.Select(f => f.GetObjectID()).ToList();

				// Get the layer's DB table and compare to the class handle of the features to be selected:
				foreach (var layer in inLayers.Where(
					         fl =>
					         {
						         FeatureClass layerFeatureClass =
							         LayerUtils.GetFeatureClass(fl, true);

						         return layerFeatureClass != null &&
						                layerFeatureClass.Handle.ToInt64() == classHandle;
					         }))
				{
					if (progressor is { CancellationToken.IsCancellationRequested: true })
					{
						_msg.Debug("Select features canceled");
						break;
					}

					result += SelectRows(layer, SelectionCombinationMethod.Add, objectIds);
				}
			}

			return result;
		}

		public static long SelectFeatures([NotNull] FeatureSelectionBase featuresPerLayer,
		                                  SelectionCombinationMethod selectionCombinationMethod)
		{
			Assert.ArgumentNotNull(featuresPerLayer, nameof(featuresPerLayer));

			return SelectRows(featuresPerLayer.BasicFeatureLayer,
			                  selectionCombinationMethod,
			                  featuresPerLayer.GetOids().ToList());
		}

		public static long SelectFeatures(
			[NotNull] ICollection<FeatureSelectionBase> featuresPerLayers,
			SelectionCombinationMethod selectionCombinationMethod,
			[CanBeNull] CancelableProgressor progressor = null)
		{
			Assert.ArgumentNotNull(featuresPerLayers, nameof(featuresPerLayers));

			long result = 0;

			foreach (FeatureSelectionBase featuresPerLayer in featuresPerLayers)
			{
				if (progressor is { CancellationToken.IsCancellationRequested: true })
				{
					_msg.Debug("Select features canceled");
					break;
				}

				result += SelectRows(featuresPerLayer.BasicFeatureLayer,
				                     selectionCombinationMethod,
				                     featuresPerLayer.GetOids().ToList());
			}

			return result;
		}

		public static IEnumerable<Feature> GetSelectedFeatures([NotNull] MapView activeView)
		{
			const bool withoutJoins = false;
			SpatialReference sref = activeView.Map.SpatialReference;

			foreach (Feature feature in MapUtils.GetFeatures(
				         GetSelection(activeView.Map), withoutJoins, sref))
			{
				yield return feature;
			}
		}

		public static IEnumerable<Feature> GetSelectedFeatures([CanBeNull] BasicFeatureLayer layer)
		{
			ArcGIS.Core.Data.Selection selection = layer?.GetSelection();

			if (selection == null)
			{
				yield break;
			}

			using (RowCursor cursor = selection.Search(null, false))
			{
				while (cursor.MoveNext())
				{
					yield return (Feature) cursor.Current;
				}
			}
		}

		public static Dictionary<MapMember, List<long>> GetSelection([NotNull] Map map)
		{
			SelectionSet selectionSet = map.GetSelection();

			return GetSelection(selectionSet);
		}

		public static Dictionary<T, List<long>> GetSelection<T>([NotNull] Map map)
			where T : MapMember
		{
			SelectionSet selectionSet = map.GetSelection();

			return GetSelection<T>(selectionSet);
		}

		[NotNull]
		public static Dictionary<MapMember, List<long>> GetSelection(
			[NotNull] SelectionSet selectionSet)
		{
			return selectionSet.ToDictionary();
		}

		[NotNull]
		public static Dictionary<T, List<long>> GetSelection<T>(
			[NotNull] SelectionSet selectionSet) where T : MapMember
		{
			return selectionSet.ToDictionary<T>();
		}

		[NotNull]
		public static Dictionary<MapMember, List<long>> GetSelection(
			[NotNull] MapSelectionChangedEventArgs selectionChangedArgs)
		{
			return GetSelection(selectionChangedArgs.Selection);
		}

		[NotNull]
		public static Dictionary<Table, List<long>> GetSelectionByTable(
			[NotNull] Dictionary<MapMember, List<long>> oidsByMapMember)
		{
			var result = new Dictionary<Table, List<long>>(oidsByMapMember.Count);

			var tableByHandle = new Dictionary<IntPtr, Table>(oidsByMapMember.Count);

			var oidsByHandle = new Dictionary<IntPtr, List<long>>();

			foreach (var pair in oidsByMapMember)
			{
				MapMember mapMember = pair.Key;
				IReadOnlyCollection<long> oids = pair.Value;

				if (mapMember is not FeatureLayer featureLayer)
				{
					continue;
				}

				Table table = featureLayer.GetTable();

				if (oidsByHandle.ContainsKey(table.Handle))
				{
					oidsByHandle[table.Handle].AddRange(oids);
				}
				else
				{
					tableByHandle.Add(table.Handle, table);
					oidsByHandle.Add(table.Handle, oids.ToList());
				}
			}

			foreach ((IntPtr handle, Table table) in tableByHandle)
			{
				if (oidsByHandle.TryGetValue(handle, out List<long> oids))
				{
					result.Add(table, oids.Distinct().ToList());
				}
			}

			return result;
		}

		public static int GetFeatureCount(
			[NotNull] IEnumerable<KeyValuePair<MapMember, List<long>>> selection)
		{
			Assert.ArgumentNotNull(selection, nameof(selection));

			return selection.Select(pair => pair.Value).Sum(set => set.Count);
		}

		public static int GetFeatureCount(
			[NotNull] IEnumerable<KeyValuePair<BasicFeatureLayer, List<long>>> selection)
		{
			Assert.ArgumentNotNull(selection, nameof(selection));

			return selection.Select(pair => pair.Value).Sum(set => set.Count);
		}

		public static int GetFeatureCount(
			[NotNull] IDictionary<BasicFeatureLayer, List<long>> selection)
		{
			Assert.ArgumentNotNull(selection, nameof(selection));

			return selection.Values.Sum(set => set.Count);
		}

		public static int GetFeatureCount(
			[NotNull] IDictionary<BasicFeatureLayer, List<Feature>> selection)
		{
			Assert.ArgumentNotNull(selection, nameof(selection));

			return selection.Values.Sum(set => set.Count);
		}

		public static int GetFeatureCount(
			[NotNull] IEnumerable<FeatureSelectionBase> selection)
		{
			Assert.ArgumentNotNull(selection, nameof(selection));

			return selection.Sum(set => set.GetCount());
		}

		private static void LogFeatureSelection([NotNull] IDisplayTable tableBasedMapMember,
		                                        ArcGIS.Core.Data.Selection selection,
		                                        long selectionCount)
		{
			var mapMember = (MapMember) tableBasedMapMember;

			string mapMemberName = mapMember.Name;

			if (_msg.IsVerboseDebugEnabled)
			{
				var tableDefinition = tableBasedMapMember as ITableDefinitionQueries;

				_msg.Debug(
					$"Selected OIDs: {StringUtils.Concatenate(selection.GetObjectIDs(), ", ")} " +
					$"from {mapMemberName}. Definition query: {tableDefinition?.DefinitionQuery}");
			}

			if (selectionCount == 0)
			{
				return;
			}

			string mapMemberType = mapMember is StandaloneTable ? "table" : "layer";

			string format = selectionCount == 1
				                ? "-> {0:N0} feature selected in {1} '{2}'"
				                : "-> {0:N0} features selected in {1} '{2}'";

			_msg.InfoFormat(format, selectionCount, mapMemberType, mapMemberName);
		}

		[NotNull]
		public static Dictionary<BasicFeatureLayer, List<Feature>>
			GetApplicableSelectedFeatures(
				[NotNull] IDictionary<BasicFeatureLayer, List<long>> selectionByLayer,
				[CanBeNull] Predicate<BasicFeatureLayer> predicate = null,
				bool withoutJoins = false)
		{
			Assert.ArgumentNotNull(selectionByLayer, nameof(selectionByLayer));

			var result = new Dictionary<BasicFeatureLayer, List<Feature>>(selectionByLayer.Count);

			SpatialReference mapSpatialReference = MapView.Active.Map.SpatialReference;

			foreach (var oidsByLayer in GetApplicableSelection(selectionByLayer, predicate))
			{
				BasicFeatureLayer layer = oidsByLayer.Key;
				List<long> oids = oidsByLayer.Value;

				var features = MapUtils
				               .GetFeatures(layer, oids, withoutJoins, recycling: false,
				                            mapSpatialReference)
				               .ToList();

				result.Add(layer, features);
			}

			return result;
		}

		public static Dictionary<BasicFeatureLayer, List<long>> GetApplicableSelection(
			[NotNull] IDictionary<BasicFeatureLayer, List<long>> selectionByLayer,
			[CanBeNull] Predicate<BasicFeatureLayer> predicate = null)
		{
			Assert.ArgumentNotNull(selectionByLayer, nameof(selectionByLayer));

			return selectionByLayer.Where(pair => predicate != null && predicate(pair.Key))
			                       .ToDictionary(p => p.Key, p => p.Value);
		}

		/// <returns>true iff the given <paramref name="feature"/> is
		/// selected on the given <paramref name="layer"/></returns>
		/// <remarks>Must run on MCT</remarks>
		public static bool IsSelected(BasicFeatureLayer layer, Feature feature)
		{
			if (layer is null) return false;

			using var selection = layer.GetSelection();

			return IsSelected(selection, feature);
		}

		/// <returns>true iff the given <paramref name="feature"/> is
		/// in the given <paramref name="selection"/></returns>
		/// <remarks>Must run on MCT</remarks>
		public static bool IsSelected(ArcGIS.Core.Data.Selection selection, Feature feature)
		{
			if (selection is null) return false;

			switch (selection.SelectionType)
			{
				case SelectionType.ObjectID:
					var oid = feature.GetObjectID();
					return selection.GetObjectIDs().Contains(oid);

				case SelectionType.GlobalID:
					var guid = feature.GetGlobalID();
					return selection.GetGlobalIDs().Contains(guid);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
