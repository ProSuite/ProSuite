using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
				yield break;
			}

			foreach (IWireFrameSourceLayer wireFrameClass in SourceLayers.Get(_mapId))
			{
				if (wireFrameClass == null || ! wireFrameClass.Visible)
				{
					continue;
				}

				queryFilter.WhereClause = wireFrameClass.DefinitionQuery; // TODO should AND the two!

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

						yield return GetFieldValues(feature, className, shapeType);
					}
				}
			}
		}

		private static object[] GetFieldValues([NotNull] Feature sourceFeature,
		                                       [NotNull] string className,
		                                       [NotNull] string shapeType)
		{
			try
			{
				var sourceOid = sourceFeature.GetObjectID();
				var wireFrameOid = ContriveObjectID(className, sourceOid);

				// TODO Attribute Table shows "Error: Failed to retrieve a page of rows" with all but the original non-unique OIDs!
				//      Same if OID is a constant (non-unique), or 2*sourceOid (non-unique),
				//      or 1+sourceOid (non-unique), 9,8,7,6,5,4, (but increasing 1,2,3,4,5,6 works), ...!

				var values = new object[5];

				values[0] = wireFrameOid;
				values[1] = CreatePolyline(sourceFeature.GetShape());
				values[2] = sourceOid;
				values[3] = className;
				values[4] = shapeType;

				return values;
			}
			catch (Exception e)
			{
				_msg.Error($"Error getting values for feature {className} " +
				           $"<oid> {sourceFeature.GetObjectID()}", e);
			}

			return null;
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

		private static IReadOnlyList<PluginField> CreateSchema()
		{
			// Note: OBJECTID is optional; if missing, cannot snap to wireframe!

			var array = new[]
			            {
				            new PluginField("OBJECTID", "ObjectID", FieldType.OID),
				            new PluginField("SHAPE", "Shape", FieldType.Geometry),
				            new PluginField("SOURCE_FEATURE_OID", "Source Feature OID",
				                            FieldType.Integer),

				            new PluginField("SOURCE_FEATURE_CLASS", "Source FeatureClass",
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
	}
}
