using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources.WireFrame
{
	public class WireFrameTable : PluginTableTemplate
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly string _mapId;
		private readonly string _tableName;
		private readonly IReadOnlyList<PluginField> _fields;

		private IWireFrameSourceLayers _wireFrameSourceLayers;

		public WireFrameTable([NotNull] string mapId, [NotNull] string tableName)
		{
			_mapId = mapId ?? throw new ArgumentNullException(nameof(mapId));
			_tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));

			_fields = CreateSchema();
		}

		/// <summary>
		/// The reference to the source layers to be aggregated by this wire frame table.
		/// </summary>
		private IWireFrameSourceLayers SourceLayers =>
			_wireFrameSourceLayers ??= PluginContextRegistry.WireFrameSourceLayers;

		#region PluginTableTemplate members

		public override string GetName()
		{
			return _tableName;
		}

		public override IReadOnlyList<PluginField> GetFields()
		{
			return _fields;
		}

		public override GeometryType GetShapeType()
		{
			return GeometryType.Polyline;
		}

		public override Envelope GetExtent()
		{
			// Do not return an empty envelope.
			// Pluggable Datasource cannot handle an empty envelope. Null seems fine.
			// Note: Pro also derives SRef and HasZ/HasM from this Envelope!

			if (SourceLayers is null)
			{
				return null; // not initialized, cannot compute extent
			}

			Envelope result = null;

			try
			{
				foreach (IWireFrameSourceLayer wireFrameClass in SourceLayers.Get(_mapId))
				{
					Envelope thisExtent = wireFrameClass.Extent;

					if (result == null)
					{
						result = thisExtent;
					}
					else
					{
						if (thisExtent != null)
						{
							result = result.Union(thisExtent);
						}
					}
				}
			}
			catch (Exception e)
			{
				_msg.Error("Error getting wire frame plugin datasource extent", e);
			}

			return result;
		}

		public override PluginCursorTemplate Search(QueryFilter queryFilter)
		{
			PluginCursor cursor = null;
			Stopwatch watch = _msg.DebugStartTiming();

			// Note: QueryFilter.PrefixClause and QueryFilter.PostfixClause may be ignored
			// Note: QueryFilter.WhereClause will be empty if IsQueryLanguageSupported() returns false
			// Note: QueryFilter.ObjectIDs must be supported (and logically ANDed with WhereClause)
			// Note: QueryFilter.OutputSpatialReference must be supported (must re-project)

			try
			{
				cursor = new PluginCursor(GetRowValues(queryFilter));
			}
			catch (Exception e)
			{
				_msg.Error("Error getting wire frame cursor.", e);
			}

			_msg.DebugStopTiming(
				watch, $"{nameof(WireFrameTable)}.{nameof(Search)}()");

			return cursor;
		}

		public override PluginCursorTemplate Search(SpatialQueryFilter spatialQueryFilter)
		{
			// Note: SpatialQueryFilter.FilterGeometry/SpatialRelationship should be honored
			// Note: SpatialQueryFilter.SearchOrder can be used or ignored

			return Search((QueryFilter) spatialQueryFilter);
		}

		public override bool IsNativeRowCountSupported()
		{
			// First shot: not supported; but we probably could!
			return false;
		}

		public override long GetNativeRowCount()
		{
			throw new NotSupportedException();
		}

		#endregion

		private IEnumerable<object[]> GetRowValues(QueryFilter queryFilter)
		{
			if (SourceLayers is null)
			{
				return Enumerable.Empty<object[]>();
			}

			var cache = WireFrame.Instance.GetWireCache(_mapId);

			// Note: Pro retrieves Attribute Table data for the current map extent
			// by setting the list of ObjectIDs on the QueryFilter and seems to expect
			// the result in the same order (gives "failed to retrieve a page of rows"
			// otherwise). We could implement that, but at a performance penalty.

			if (queryFilter.ObjectIDs is { Count: > 0 })
			{
				// Special case: retrieval by OID, which happens when Pro renders
				// the attribute table. Must yield results in the order of the OIDs
				// given or Pro reports "Error: failed to retrieve a page of rows"!

				return GetRowValues(cache, queryFilter.ObjectIDs);
			}

			// Normal case: retrieval by spatial filter (never an attribute filter,
			// because this plugin datasource states IsQueryLanguageSupported false):

			return GetRowValues(cache, queryFilter);
		}

		private IEnumerable<object[]> GetRowValues(WireFrame.IWireCache cache, QueryFilter queryFilter)
		{
			foreach (IWireFrameSourceLayer wireFrameClass in SourceLayers.Get(_mapId))
			{
				if (wireFrameClass == null || ! wireFrameClass.Visible)
				{
					continue;
				}

				// Query filter's WhereClause is known to be empty, because this
				// plugin datasource declares IsQueryLanguageSupported false:

				queryFilter.WhereClause = wireFrameClass.DefinitionQuery;

				using (RowCursor cursor = wireFrameClass.Search(queryFilter))
				{
					if (cursor == null)
					{
						// No feature class (invalid layer) or not supported:
						continue;
					}

					string className = Assert.NotNull(wireFrameClass.FeatureClassName);
					string shapeType = wireFrameClass.GeometryType.ToString();

					while (cursor.MoveNext())
					{
						Feature feature = (Feature) cursor.Current;
						var shape = feature.GetShape();
						long oid = feature.GetObjectID();
						long wireId = cache.GetID(className, oid);

						yield return GetFieldValues(wireId, shape, oid, className, shapeType);
					}
				}
			}
		}

		private IEnumerable<object[]> GetRowValues(WireFrame.IWireCache cache, IReadOnlyList<long> oids)
		{
			// Yield wire frame features in the order of the OIDs given.
			// The implementation looks up ClassName and OID from the given
			// wire feature IDs, groups by ClassName, retrieves features per
			// ClassName, and pigeonholes them into pre-allocated slots.

			var dict = new Dictionary<string, List<Pair>>();

			// Arrays ("pigeonholes") for result values:

			var wireIds = oids.ToArray();
			var wireShapes = new Geometry[wireIds.Length];
			var sourceOids = new long[wireIds.Length];
			var sourceClasses = new string[wireIds.Length];
			var sourceShapeTypes = new string[wireIds.Length];

			// Group given OIDs by source feature class:

			for (int i = 0; i < wireIds.Length; i++)
			{
				if (cache.GetSource(wireIds[i], out var className, out var oid))
				{
					sourceClasses[i] = className;
					sourceOids[i] = oid;

					if (!dict.TryGetValue(className, out var oidList))
					{
						oidList = new List<Pair>();
						dict.Add(className, oidList);
					}

					oidList.Add(new Pair(i, oid));
				}
				// else: no such wire ID: skip (should not occur)
			}

			var filter = new QueryFilter();
			var layers = SourceLayers.Get(_mapId)
			                         .Where(l => l is { Visible: true })
			                         .ToList();

			// Retrieve features by OID per feature class:

			foreach (var pair in dict)
			{
				var sourceClassName = pair.Key;
				var sourceOidList = pair.Value;

				var layer = layers.First(lyr => lyr.FeatureClassName == sourceClassName);

				var shapeType = layer.GeometryType.ToString();

				filter.ObjectIDs = sourceOidList.Select(p => p.OID).ToList();
				filter.WhereClause = layer.DefinitionQuery;

				using var cursor = layer.Search(filter);

				int i = 0;
				foreach (var row in Enumerate(cursor))
				{
					var feature = (Feature)row;
					var oid = feature.GetObjectID();
					while (i < sourceOidList.Count && sourceOidList[i].OID != oid)
						++i;
					Assert.True(i < sourceOidList.Count && sourceOidList[i].OID == oid,
								"Retrieved feature's OID not amongst requested OIDs or not in requested order");
					int index = sourceOidList[i].Index;

					wireShapes[index] = CreatePolyline(feature.GetShape());
					Assert.AreEqual(oid, sourceOids[index], "oops");
					//sourceOids[index] = oid;
					Assert.AreEqual(sourceClassName, sourceClasses[index], "oops");
					//sourceClasses[index] = sourceClassName;
					sourceShapeTypes[index] = shapeType;
				}
			}

			for (int i = 0; i < wireIds.Length; i++)
			{
				if (wireShapes[i] is null) continue; // filtered away (by Def Query)
				yield return GetFieldValues(wireIds[i], wireShapes[i],
				                            sourceOids[i], sourceClasses[i], sourceShapeTypes[i]);
			}
		}

		private static object[] GetFieldValues(long wireId, Geometry shape,
		                                       long sourceOid, string className, string shapeType)
		{
			try
			{
				return new object[]
				       {
					       wireId,
					       CreatePolyline(shape),
					       sourceOid,
					       className,
					       shapeType
				       };
			}
			catch (Exception e)
			{
				_msg.Error($"Error getting values for feature {className} " +
				           $"<oid> {sourceOid}", e);
			}

			return null;
		}

		private static IReadOnlyList<PluginField> CreateSchema()
		{
			// Note: an OBJECTID field is optional for a plugin datasource, but
			// if missing, cannot snap to wireframe, and attribute table is fragile

			var array = new[]
			            {
				            new PluginField("OBJECTID", "ObjectID", FieldType.OID),
				            new PluginField("SHAPE", "Shape", FieldType.Geometry),
				            new PluginField("SOURCE_FEATURE_OID", "Source Feature OID",
				                            FieldType.Integer),

				            new PluginField("SOURCE_FEATURE_CLASS", "Source Feature Class",
				                            FieldType.String)
				            {
					            Length = 50
				            },

				            new PluginField("SOURCE_SHAPE_TYPE", "Source Shape Type",
				                            FieldType.String)
				            {
					            Length = 15
				            }
			            };

			return new ReadOnlyCollection<PluginField>(array);
		}

		/// <remarks>
		/// Since we aggregate features from many layers, their original OIDs
		/// will no longer be unique. Uniqueness is not mission-critical here,
		/// just shift by some prime and add a hash of the class name.
		/// </remarks>
		private static long ContriveObjectID(string className, long oid)
		{
			const int prime = 1021; // positive, around 1000 should be fine for us
			long hash = className.GetHashCode();
			hash -= int.MinValue; // make non-negative
			long code = hash % prime;
			return oid * prime + code;
		}

		private static Polyline CreatePolyline(Geometry geometry)
		{
			if (geometry == null)
			{
				return null;
			}

			if (geometry is Polyline polyline)
			{
				return polyline;
			}

			if (geometry is Polygon polygon)
			{
				return (Polyline) GeometryEngine.Instance.Boundary(polygon);
			}

			throw new NotImplementedException(
				$"{geometry.GeometryType} geometries are not supported");
		}

		private static IEnumerable<Row> Enumerate(RowCursor cursor)
		{
			if (cursor is null) yield break;
			while (cursor.MoveNext())
			{
				yield return cursor.Current;
			}
		}

		private readonly struct Pair
		{
			public readonly int Index;
			public readonly long OID;

			public Pair(int index, long oid)
			{
				Index = index;
				OID = oid;
			}
		}
	}
}
