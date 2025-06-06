using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto
{
	public static class LayerUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Given a layer, find the map to which it belongs.
		/// </summary>
		public static Map FindMap(Layer layer)
		{
			var parent = layer.Parent;

			while (parent != null)
			{
				if (parent is Map map) return map;
				if (parent is not Layer other) break;

				parent = other.Parent;
			}

			return null;
		}

		/// <summary>
		/// Returns the Rows or features found by the layer/standalone table search. Honors
		/// definition queries, layer time, etc. defined on the layer. According to the
		/// documentation, valid rows returned by a cursor should be disposed.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="layer"></param>
		/// <param name="filter"></param>
		/// <param name="predicate"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static IEnumerable<T> SearchRows<T>([NotNull] IDisplayTable layer,
		                                           [CanBeNull] QueryFilter filter = null,
		                                           [CanBeNull] Predicate<T> predicate = null,
		                                           CancellationToken cancellationToken = default)
			where T : Row
		{
			if (layer is null)
				throw new ArgumentNullException(nameof(layer));

			if (predicate == null)
			{
				predicate = _ => true;
			}

			// NOTE: An invalid filter (e.g. subfields "*,OBJECTID") can crash the application.
			_msg.VerboseDebug(() => $"Querying layer {((MapMember) layer).Name} using filter: " +
			                        $"{GdbQueryUtils.FilterPropertiesToString(filter)}");

			using RowCursor cursor = layer.Search(filter);
			if (cursor is null) yield break; // no valid data source

			while (cursor.MoveNext())
			{
				if (cancellationToken.IsCancellationRequested)
				{
					yield break;
				}

				T currentRow = (T) cursor.Current;

				if (predicate(currentRow))
				{
					yield return currentRow;
				}
			}
		}

		/// <summary>
		/// Returns the Object IDs of features found by the layer / standalone table search.
		/// Honors definition queries, layer time, etc. defined on the layer.
		/// </summary>
		public static IEnumerable<long> SearchObjectIds(
			[NotNull] IDisplayTable layer,
			[CanBeNull] QueryFilter filter = null,
			[CanBeNull] Predicate<Feature> predicate = null,
			CancellationToken cancellationToken = default)
		{
			using var table = layer.GetTable();
			using var definition = table?.GetDefinition();
			var oidField = definition?.GetObjectIDField();

			if (string.IsNullOrEmpty(oidField))
			{
				yield break; // no OIDs
			}

			if (filter == null)
			{
				filter = new QueryFilter { SubFields = oidField };
			}
			else if (GdbQueryUtils.EnsureSubField(filter.SubFields, oidField,
			                                      out string newSubFields))
			{
				filter.SubFields = newSubFields;
			}

			foreach (Feature row in SearchRows(layer, filter, predicate))
			{
				if (cancellationToken.IsCancellationRequested)
				{
					yield break;
				}

				yield return row.GetObjectID();

				// Documentation: If a valid Row is returned by RowCursor.Current, it should be Disposed.
				row.Dispose();
			}
		}

		/// <summary>
		/// Get the single layer definition of type <typeparamref name="T"/>
		/// from the given <paramref name="layerDocument"/>; return null if
		/// there is no single such layer definition in the document.
		/// </summary>
		public static T GetSingleLayerCIM<T>(LayerDocument layerDocument) where T : CIMBaseLayer
		{
			if (layerDocument is null) return null;
			var cim = layerDocument.GetCIMLayerDocument();
			var definitions = cim?.LayerDefinitions;
			var matches = definitions?.OfType<T>().ToArray();
			return matches?.Length != 1 ? null : matches.Single();
		}

		/// <remarks>
		/// A layer document (.lyrx file) can contain one or more layer definitions!
		/// </remarks>
		[CanBeNull]
		public static CIMRenderer GetRenderer(LayerDocument layerDocument,
		                                      Func<CIMDefinition, bool> predicate = null)
		{
			if (layerDocument is null) return null;

			CIMLayerDocument cim = layerDocument.GetCIMLayerDocument();
			var definitions = cim?.LayerDefinitions;
			if (definitions is null || definitions.Length <= 0) return null;

			var definition = predicate is null
				                 ? definitions.First()
				                 : definitions.First(predicate);

			return definition is CIMFeatureLayer featureLayer
				       ? featureLayer.Renderer
				       : null;
		}

		/// <summary>
		/// Get first renderer from <paramref name="layerDocument"/>
		/// compatible with the given <paramref name="targetLayer"/>.
		/// </summary>
		[CanBeNull]
		public static CIMRenderer GetRenderer(
			LayerDocument layerDocument, FeatureLayer targetLayer)
		{
			return GetRenderer(layerDocument, IsCompatible);

			bool IsCompatible(CIMDefinition cimDefinition)
			{
				if (targetLayer is null) return true;
				return cimDefinition is CIMFeatureLayer cimFeatureLayer &&
				       targetLayer.CanSetRenderer(cimFeatureLayer.Renderer);
			}
		}

		[NotNull]
		public static LayerDocument OpenLayerDocument([NotNull] string filePath)
		{
			if (! File.Exists(filePath))
			{
				throw new ArgumentException($"{filePath} does not exist");
			}

			// todo daro no valid .lyrx file

			return new LayerDocument(filePath);
		}

		/// <summary>
		/// Gets the ObjectIDs of selected features from the given <paramref name="layer"/>.
		/// </summary>
		/// <remarks>
		/// Although a layer data source is broken BasicFeatureLayer.SelectionCount
		/// can return a valid result.
		/// </remarks>
		public static IEnumerable<long> GetSelectionOids(this BasicFeatureLayer layer)
		{
			using (ArcGIS.Core.Data.Selection selection = layer?.GetSelection())
			{
				return selection == null ? Enumerable.Empty<long>() : selection.GetObjectIDs();
			}
		}

		public static bool HasSelection([CanBeNull] BasicFeatureLayer layer)
		{
			return layer?.SelectionCount > 0;
		}

		public static void SetLayerSelectability([NotNull] BasicFeatureLayer layer,
		                                         bool selectable = true)
		{
			var cimDefinition = (CIMFeatureLayer) layer.GetDefinition();
			cimDefinition.Selectable = selectable;
			layer.SetDefinition(cimDefinition);
		}

		/// <summary>
		/// Gets the layer's visibility state.
		/// Works as well for layers nested in group layers.
		/// </summary>
		public static bool IsVisible(Layer layer)
		{
			if (layer is null) return false;

			if (! layer.IsVisible)
			{
				return false;
			}

			if (layer.Parent is Layer parentLayer)
			{
				// ReSharper disable once TailRecursiveCall
				return IsVisible(parentLayer);
			}

			// Version without tail recursion:
			//var parent = layer.Parent;

			//while (parent is Layer parentLayer)
			//{
			//	if (! parentLayer.IsVisible)
			//	{
			//		return false;
			//	}

			//	parent = parentLayer.Parent;
			//}

			return true;
		}

		/// <remarks>A layer is considered valid if it has a non-null data table</remarks>
		public static bool IsLayerValid([CanBeNull] BasicFeatureLayer featureLayer)
		{
			using var table = featureLayer?.GetTable();
			return table != null;
		}

		/// <summary>
		/// Determines whether the specified layer uses the specified feature class.
		/// </summary>
		/// <param name="layer"></param>
		/// <param name="featureClass"></param>
		/// <returns></returns>
		public static bool LayerUsesFeatureClass([NotNull] FeatureLayer layer,
		                                         [NotNull] FeatureClass featureClass)
		{
			FeatureClass layerFeatureClass = layer.GetFeatureClass();

			return ReferencesSameGdbFeatureClass(layerFeatureClass, featureClass);
		}

		/// <summary>
		/// Determines if the specified layers use the same feature class. One or both layers might
		/// have joins. The actual geodatabase feature classes are compared. 
		/// </summary>
		/// <param name="layer1"></param>
		/// <param name="layer2"></param>
		/// <returns></returns>
		public static bool LayersUseSameFeatureClass([NotNull] FeatureLayer layer1,
		                                             [NotNull] FeatureLayer layer2)
		{
			FeatureClass featureClass1 = layer1.GetFeatureClass();
			FeatureClass featureClass2 = layer2.GetFeatureClass();

			return ReferencesSameGdbFeatureClass(featureClass1, featureClass2);
		}

		/// <summary>
		/// Determines if the specified feature classes are the same or, in case one or both
		/// feature classes are joined, the actual geodatabase feature classes are compared. 
		/// </summary>
		/// <param name="featureClass1"></param>
		/// <param name="featureClass2"></param>
		/// <returns></returns>
		private static bool ReferencesSameGdbFeatureClass(FeatureClass featureClass1,
		                                                  FeatureClass featureClass2)
		{
			if (ReferenceEquals(featureClass1, featureClass2))
			{
				return true;
			}

			if (featureClass1 == null || featureClass2 == null)
			{
				return false;
			}

			if (featureClass1.IsJoinedTable())
			{
				featureClass1 = DatasetUtils.GetDatabaseFeatureClass(featureClass1);
			}

			if (featureClass2.IsJoinedTable())
			{
				featureClass2 = DatasetUtils.GetDatabaseFeatureClass(featureClass2);
			}

			return DatasetUtils.IsSameTable(featureClass1, featureClass2);
		}

		/// <summary>
		/// Determines if two layers reference the same data source.
		/// </summary>
		/// <param name="layer1">The first layer.</param>
		/// <param name="layer2">The second layer.</param>
		/// <param name="requireSameVersion">Whether the layers must reference the same version.</param>
		/// <param name="requireSameDefinition">Whether the layers must have the same definition (e.g., same definition query).</param>
		/// <returns>True if the layers reference the same data source; otherwise, false.</returns>
		public static bool DataSourcesAreEqual([NotNull] Layer layer1,
		                                       [NotNull] Layer layer2,
		                                       bool requireSameVersion,
		                                       bool requireSameDefinition = false)
		{
			Assert.ArgumentNotNull(layer1, nameof(layer1));
			Assert.ArgumentNotNull(layer2, nameof(layer2));

			// Handle group layers
			if (layer1 is CompositeLayer groupLayer1 && layer2 is CompositeLayer groupLayer2)
			{
				return DataSourcesAreEqual(groupLayer1, groupLayer2,
				                           requireSameVersion, requireSameDefinition);
			}

			// Handle case where a data layer is compared with a group layer
			if (layer1 is FeatureLayer && layer2 is CompositeLayer groupLayer)
			{
				// Replace the existing layer if any of the sub layers of the
				// new group layer matches
				foreach (Layer subLayer in groupLayer.Layers)
				{
					if (DataSourcesAreEqual(layer1, subLayer, requireSameVersion,
					                        requireSameDefinition))
					{
						return true;
					}
				}

				return false;
			}

			// Handle feature layers and stand-alone tables
			if (layer1 is IDisplayTable displayTable1 && layer2 is IDisplayTable displayTable2)
			{
				return DataSourcesAreEqual(displayTable1, displayTable2,
				                           requireSameVersion, requireSameDefinition);
			}

			// TODO: Other layers, Service layers etc.
			//// Handle raster layers
			//if (layer1 is RasterLayer rasterLayer1 && layer2 is RasterLayer rasterLayer2)
			//{
			//	return CompareRasterLayers(rasterLayer1, rasterLayer2, requireSameVersion);
			//}

			// Different layer types - not equal
			return false;
		}

		private static bool DataSourcesAreEqual([NotNull] CompositeLayer groupLayer1,
		                                        [NotNull] CompositeLayer groupLayer2,
		                                        bool requireSameVersion,
		                                        bool requireSameDefinition)
		{
			// Only equal if all contained layers match
			if (groupLayer1.Layers.Count != groupLayer2.Layers.Count)
			{
				return false;
			}

			// Check if all child layers match (in order)
			for (int i = 0; i < groupLayer1.Layers.Count; i++)
			{
				if (! DataSourcesAreEqual(groupLayer1.Layers[i], groupLayer2.Layers[i],
				                          requireSameVersion, requireSameDefinition))
				{
					return false;
				}
			}

			return true;
		}

		private static bool DataSourcesAreEqual([NotNull] IDisplayTable layer1,
		                                        [NotNull] IDisplayTable layer2,
		                                        bool requireSameVersion,
		                                        bool requireSameDefinition)
		{
			// Check if both layers are valid
			var featureClass1 = layer1.GetTable();
			var featureClass2 = layer2.GetTable();

			if (featureClass1 == null || featureClass2 == null)
			{
				return false;
			}

			// Check if they reference the same dataset
			if (! DatasetUtils.IsSameTable(featureClass1, featureClass2))
			{
				return false;
			}

			// Check versions if required
			if (requireSameVersion)
			{
				// Get connection properties to compare version info
				var gdb1 = featureClass1.GetDatastore() as Geodatabase;
				var gdb2 = featureClass2.GetDatastore() as Geodatabase;

				// Check if versions are specified
				bool hasVersion1 = gdb1?.IsVersioningSupported() == true;
				bool hasVersion2 = gdb2?.IsVersioningSupported() == true;

				// If only one has a version, they're not equal
				if (hasVersion1 != hasVersion2)
				{
					return false;
				}

				// If both have versions, compare them
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				if (hasVersion1 && hasVersion2)
				{
					string version1 = gdb1.GetVersionManager().GetCurrentVersion().GetName();
					string version2 = gdb2.GetVersionManager().GetCurrentVersion().GetName();

					if (! string.Equals(version1, version2, StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
				}
			}

			// Check definitions if required
			if (requireSameDefinition)
			{
				// Compare definition queries
				string defQuery1 = GetDefinitionQuery(layer1);
				string defQuery2 = GetDefinitionQuery(layer2);

				if (! string.Equals(defQuery1, defQuery2, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			return true;
		}

		public static string GetDefinitionQuery(IDisplayTable displayTable)
		{
			if (displayTable is StandaloneTable standaloneTable)
			{
				return GetDefinitionQuery(standaloneTable);
			}

			if (displayTable is BasicFeatureLayer featureLayer)
			{
				return GetDefinitionQuery(featureLayer);
			}

			throw new NotImplementedException("Unsupported type of display table");
		}

		public static string GetDefinitionQuery(BasicFeatureLayer featureLayer)
		{
			return featureLayer.DefinitionQuery;
		}

		public static string GetDefinitionQuery(StandaloneTable standaloneTable)
		{
			return standaloneTable.DefinitionQuery;
		}

		/// <summary>
		/// Returns the feature class which is referenced by the specified layer. In case the
		/// feature class is a joined table and the <see cref="unJoined"/> parameter is true,
		/// the actual geodatabase feature class is returned.
		/// </summary>
		/// <param name="featureLayer"></param>
		/// <param name="unJoined"></param>
		/// <returns></returns>
		public static FeatureClass GetFeatureClass(BasicFeatureLayer featureLayer,
		                                           bool unJoined)
		{
			Assert.ArgumentNotNull(featureLayer, nameof(featureLayer));

			FeatureClass featureClass = GetFeatureClass(featureLayer);

			if (featureClass == null)
			{
				return null;
			}

			return unJoined ? DatasetUtils.GetDatabaseFeatureClass(featureClass) : featureClass;
		}

		/// <summary>
		/// Returns the table referenced by the specified map member. In case the table is a joined
		/// table and the <paramref name="unJoined"/> parameter is true, the actual geodatabase
		/// table is returned.
		/// </summary>
		/// <param name="tableBasedMapMember"></param>
		/// <param name="unJoined"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		[CanBeNull]
		public static Table GetTable([NotNull] IDisplayTable tableBasedMapMember,
		                             bool unJoined)
		{
			Assert.ArgumentNotNull(tableBasedMapMember, nameof(tableBasedMapMember));

			Table table = tableBasedMapMember.GetTable();

			if (table == null)
			{
				return null;
			}

			return unJoined ? DatasetUtils.GetDatabaseTable(table) : table;
		}

		[CanBeNull]
		public static FeatureClass GetFeatureClass(this Layer layer)
		{
			// BasicFeatureLayer is the abstract base class for:
			// FeatureLayer, AnnotationLayer, DimensionLayer;
			// they all have a feature class as their data source, but,
			// sadly, BasicFeatureLayer has no GetFeatureClass() method.

			if (layer is FeatureLayer featureLayer)
			{
				return featureLayer.GetFeatureClass();
			}

			if (layer is AnnotationLayer annotationLayer)
			{
				return annotationLayer.GetFeatureClass();
			}

			if (layer is BasicFeatureLayer basicFeatureLayer)
			{
				return basicFeatureLayer.GetTable() as FeatureClass;
			}

			return null;
		}

		#region Underlying dataset properties

		// Roughly following the "ArcGIS Pro SDK for .NET: Advanced Editing and Edit Operations"
		// from a DevSummit tech session recording at https://youtu.be/U4vcNDEkj1w?t=2729

		/// <summary>
		/// Get how the given layer's dataset is registered with the geodatabase:
		/// non-versioned, versioned, or versioned with the option to move edits to base.
		/// </summary>
		public static RegistrationType GetRegistrationType(this FeatureLayer featureLayer)
		{
			using var featureClass = featureLayer.GetFeatureClass();
			if (featureClass is null) return RegistrationType.Nonversioned;
			return featureClass.GetRegistrationType();
		}

		public static GeodatabaseType? GetGeodatabaseType(this FeatureLayer featureLayer)
		{
			using var featureClass = featureLayer.GetFeatureClass();
			using var workspace = featureClass?.GetDatastore();

			if (workspace is Geodatabase geodatabase)
			{
				return geodatabase.GetGeodatabaseType();
			}

			return null;
		}

		public static bool IsVersioned(this FeatureLayer featureLayer)
		{
			return featureLayer.GetRegistrationType() != RegistrationType.Nonversioned;
		}

		public static bool IsBranchVersioned(this FeatureLayer featureLayer)
		{
			using var featureClass = featureLayer.GetFeatureClass();
			if (featureClass is null) return false;
			using var workspace = featureClass.GetDatastore();
			return IsBranchVersioned(workspace);
		}

		private static bool IsBranchVersioned(Datastore workspace)
		{
			if (workspace is not Geodatabase geodatabase) return false;
			if (! geodatabase.IsVersioningSupported()) return false;
			var props = geodatabase.GetConnector();
			if (props is DatabaseConnectionProperties dcProps)
			{
				// Utility network FC only:
				return ! string.IsNullOrEmpty(dcProps.Branch);
			}

			return props is ServiceConnectionProperties;
		}

		public static bool SupportsUndoRedo(this FeatureLayer featureLayer)
		{
			using var featureClass = featureLayer.GetFeatureClass();
			if (featureClass is null) return false;

			using var workspace = featureClass.GetDatastore();
			if (workspace is not Geodatabase geodatabase)
				return false; // TODO how about shapefiles?

			var gdbType = geodatabase.GetGeodatabaseType();
			if (gdbType == GeodatabaseType.FileSystem)
				return true; // shapefiles
			if (gdbType == GeodatabaseType.LocalDatabase)
				return true; // file gdbs support undo/redo

			var regType = featureClass.GetRegistrationType();
			var isVersioned = regType != RegistrationType.Nonversioned;
			if (gdbType == GeodatabaseType.RemoteDatabase && isVersioned)
				return true;

			if (IsBranchVersioned(workspace))
			{
				// Special case branch versioned: all versions except DEFAULT (no parent)
				var vmgr = geodatabase.GetVersionManager();
				var currentVersion = vmgr.GetCurrentVersion();
				return currentVersion.GetParent() != null;
			}

			return false;
		}

		#endregion

		[NotNull]
		public static string GetMeaningfulDisplayExpression([NotNull] Feature feature,
		                                                    [CanBeNull] string expression)
		{
			if (string.IsNullOrEmpty(expression) || string.IsNullOrWhiteSpace(expression))
			{
				return GdbObjectUtils.GetDisplayValue(feature);
			}

			long oid = feature.GetObjectID();

			// GetDisplayExpressions() returns
			// 1) the OIDs if the display field value is null
			//    e.g. feature["NAME"] == null > layer.GetDisplayExpressions(oid) returns the Object ID
			// 2) 0 if the display expression string is null
			bool fieldValueIsNull = long.TryParse(expression, out long oid1) && oid1 == oid;
			bool displayExpressionIsNull = long.TryParse(expression, out long oid2) && oid2 == 0;

			if (fieldValueIsNull || displayExpressionIsNull)
			{
				return GdbObjectUtils.GetDisplayValue(feature);
			}

			return expression;
		}

		[NotNull]
		public static string GetMeaningfulDisplayExpression([NotNull] BasicFeatureLayer layer,
		                                                    [NotNull] Feature feature)
		{
			long oid = feature.GetObjectID();

			// Many display expressions are possible. We take the first one.
			var expression = layer.GetDisplayExpressions(new List<long> { oid })
			                      .FirstOrDefault();

			if (string.IsNullOrEmpty(expression) || string.IsNullOrWhiteSpace(expression))
			{
				return GdbObjectUtils.GetDisplayValue(feature);
			}

			// GetDisplayExpressions() returns
			// 1) the OIDs if the display field value is null
			//    e.g. feature["NAME"] == null > layer.GetDisplayExpressions(oid) returns the Object ID
			bool fieldValueIsNull = long.TryParse(expression, out long oid1) && oid1 == oid;

			if (fieldValueIsNull)
			{
				return GdbObjectUtils.GetDisplayValue(feature);
			}

			return expression;
		}

		public static void Rename(Layer layer, string name)
		{
			CIMBaseLayer cimLayer = layer.GetDefinition();
			cimLayer.Name = name;
			layer.SetDefinition(cimLayer);
		}
	}
}
