using System;
using System.Collections.Generic;
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

		private const GeometryType _geometryType = GeometryType.Polyline;

		private readonly string _mapId;
		private readonly string _tableName;

		private IWireFrameSourceLayers _wireFrameSourceLayers;

		public WireFrameTable([NotNull] string mapId,
		                      [NotNull] string tableName)
		{
			_mapId = mapId;
			_tableName = tableName;
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
			return GetSchema();
		}

		public override Envelope GetExtent()
		{
			// Do not return an empty envelope.
			// Pluggable Datasource cannot handle an empty envelope. Null seems fine.
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

		public override GeometryType GetShapeType()
		{
			return _geometryType;
		}

		public override PluginCursorTemplate Search(QueryFilter queryFilter)
		{
			PluginCursor cursor = null;
			Stopwatch watch = _msg.DebugStartTiming();

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
			if (SourceLayers == null)
			{
				yield break;
			}

			foreach (IWireFrameSourceLayer wireFrameClass in SourceLayers.Get(_mapId))
			{
				if (wireFrameClass == null || ! wireFrameClass.Visible)
				{
					continue;
				}

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
				var values = new object[5];

				// TODO: static ConcurrentDictionary<GdbObjectReference, long> _oidByObjRef;
				//       to avoid duplicate OIDs
				values[0] = sourceFeature.GetObjectID();
				values[1] = CreatePolyline(sourceFeature.GetShape());
				values[2] = sourceFeature.GetObjectID();
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

		private static PluginField[] GetSchema()
		{
			var fields = new List<PluginField>(8)
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

			return fields.ToArray();
		}
	}
}
