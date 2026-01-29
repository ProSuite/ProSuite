using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class GeometryConstraint
	{
		[NotNull] private readonly DataView _constraintView;

		[NotNull] private readonly List<ColumnHandler<PropertyCache>> _columnHandlers;

		public GeometryConstraint([NotNull] string constraint)
		{
			Assert.ArgumentNotNullOrEmpty(constraint, nameof(constraint));

			Constraint = constraint;

			var dataTable = new DataTable("table") {CaseSensitive = false};

			_columnHandlers = AddColumns(dataTable, constraint);
			_constraintView = new DataView(dataTable) {RowFilter = constraint};
		}

		public bool IsFulfilled([CanBeNull] IGeometry geometry)
		{
			DataTable dataTable = _constraintView.Table;
			dataTable.Clear();

			DataRow row = dataTable.NewRow();

			var propertyCache = new PropertyCache(geometry);

			var index = 0;
			foreach (ColumnHandler<PropertyCache> columnHandler in _columnHandlers)
			{
				row[index] = columnHandler.GetValue(geometry, propertyCache);
				index++;
			}

			dataTable.Rows.Add(row);

			return _constraintView.Count == 1;
		}

		[NotNull]
		public string FormatValues([CanBeNull] IGeometry geometry,
		                           [NotNull] IFormatProvider formatProvider)
		{
			Assert.ArgumentNotNull(formatProvider, nameof(formatProvider));

			var sb = new StringBuilder();

			var propertyCache = new PropertyCache(geometry);

			foreach (ColumnHandler<PropertyCache> columnHandler in
			         _columnHandlers.OrderBy(c => c.ColumnName))
			{
				if (sb.Length > 0)
				{
					sb.AppendFormat("; ");
				}

				sb.AppendFormat(formatProvider,
				                "{0}={1}",
				                columnHandler.ColumnName,
				                columnHandler.FormatValue(geometry, formatProvider, propertyCache));
			}

			return sb.ToString();
		}

		[NotNull]
		public string Constraint { get; }

		[NotNull]
		private static List<ColumnHandler<PropertyCache>> AddColumns(
			[NotNull] DataTable dataTable,
			[NotNull] string constraint)
		{
			var columnHandlers = new List<ColumnHandler<PropertyCache>>();

			DataColumnCollection columns = dataTable.Columns;

			foreach (ColumnHandler<PropertyCache> columnHandler in GetAvailableColumnHandlers()
			        )
			{
				if (! UsesField(constraint, columnHandler.ColumnName))
				{
					continue;
				}

				columns.Add(columnHandler.CreateColumn());
				columnHandlers.Add(columnHandler);
			}

			return columnHandlers;
		}

		[NotNull]
		private static IEnumerable<ColumnHandler<PropertyCache>> GetAvailableColumnHandlers()
		{
			yield return Get("$Area", typeof(double), GetArea, FormatLinearUnit);
			yield return Get("$Length", typeof(double), GetLength, FormatLinearUnit);
			yield return Get("$VertexCount", typeof(int), GetVertexCount);
			yield return Get("$SliverRatio", typeof(double), GetSliverRatio, "{0:N3}");
			yield return Get("$Dimension", typeof(double), GetGeometryDimension);
			yield return Get("$EllipticArcCount", typeof(int), GetEllipticArcCount);
			yield return Get("$CircularArcCount", typeof(int), GetCircularArcCount);
			yield return Get("$BezierCount", typeof(int), GetBezierCount);
			yield return Get("$LinearSegmentCount", typeof(int), GetLinearSegmentCount);
			yield return Get("$NonLinearSegmentCount", typeof(int), GetNonLinearSegmentCount);
			yield return Get("$SegmentCount", typeof(int), GetSegmentCount);
			yield return Get("$IsClosed", typeof(bool), GetIsClosed);
			yield return Get("$XMin", typeof(double), GetXMin);
			yield return Get("$YMin", typeof(double), GetYMin);
			yield return Get("$XMax", typeof(double), GetXMax);
			yield return Get("$YMax", typeof(double), GetYMax);
			yield return Get("$ZMin", typeof(double), GetZMin);
			yield return Get("$ZMax", typeof(double), GetZMax);
			yield return Get("$MMin", typeof(double), GetMMin);
			yield return Get("$MMax", typeof(double), GetMMax);
			yield return Get("$UndefinedMValueCount", typeof(int), GetUndefinedMValueCount);
			yield return Get("$ControlPointCount", typeof(int), GetPointIdCount);

			// for use on entire shape
			yield return Get("$PartCount", typeof(int), GetPartCount);
			yield return Get("$IsMultipart", typeof(bool), GetIsMultipart);
			yield return Get("$ExteriorRingCount", typeof(int), GetExteriorRingCount);
			yield return Get("$InteriorRingCount", typeof(int), GetInteriorRingCount);

			// for use on shape parts
			yield return Get("$IsExteriorRing", typeof(bool), GetIsExteriorRing);
			yield return Get("$IsInteriorRing", typeof(bool), GetIsInteriorRing);

			// for pointIDs
			yield return Get("$IsPointIdAware", typeof(bool), GetIsPointIdAware);
			yield return Get("$PointIdMin", typeof(int), GetPointIdMin);
			yield return Get("$PointIdMax", typeof(int), GetPointIdMax);
			yield return Get("$PointIdCount", typeof(int), GetPointIdCount);

			// for multipatches
			yield return Get("$RingCount", typeof(int), GetRingCount);
			yield return Get("$TriangleFanCount", typeof(int), GetTriangleFanCount);
			yield return Get("$TriangleStripCount", typeof(int), GetTriangleStripCount);
			yield return Get("$TrianglesPatchCount", typeof(int), GetTrianglesPatchCount);
		}

		[NotNull]
		private static ColumnHandler<PropertyCache> Get(
			[NotNull] string columnName,
			[NotNull] Type type,
			[NotNull] Func<IGeometry, PropertyCache, object> valueFunction,
			[CanBeNull] string valueFormat = null)
		{
			return new ColumnHandler<PropertyCache>(columnName, type, valueFunction,
			                                        valueFormat);
		}

		private static ColumnHandler<PropertyCache> Get(
			[NotNull] string columnName,
			[NotNull] Type type,
			[NotNull] Func<IGeometry, PropertyCache, object> valueFunction,
			[NotNull] Func<IGeometry, object, IFormatProvider, string> formatFunction)
		{
			return new ColumnHandler<PropertyCache>(columnName, type, valueFunction,
			                                        formatFunction);
		}

		[NotNull]
		private static string FormatLinearUnit([CanBeNull] IGeometry geometry,
		                                       [CanBeNull] object value,
		                                       [CanBeNull] IFormatProvider formatProvider)
		{
			if (value == null || value is DBNull)
			{
				return "<NULL>";
			}

			string format = GetLinearUnitFormat(geometry);

			return string.Format(formatProvider, format, value);
		}

		private static string GetLinearUnitFormat([CanBeNull] IGeometry geometry)
		{
			if (geometry?.SpatialReference == null)
			{
				return "{0}";
			}

			ISpatialReference spatialReference = geometry.SpatialReference;

			var projCS = spatialReference as IProjectedCoordinateSystem;

			if (projCS != null)
			{
				// return format for rounding to centimeters
				double cmPerUnit = projCS.CoordinateUnit.MetersPerUnit * 100d;

				var decimalPlaces = (int) Math.Round(Math.Log10(cmPerUnit));

				return "{0:N" + decimalPlaces + "}";
			}

			// unknown or geographic coordinate system
			return "{0:N8}";
		}

		[NotNull]
		private static object GetGeometryDimension([CanBeNull] IGeometry geometry,
		                                           [NotNull] PropertyCache propertyCache)
		{
			return geometry == null ? DBNull.Value : GetDimension(geometry.Dimension);
		}

		[NotNull]
		private static object GetDimension(esriGeometryDimension dimension)
		{
			switch (dimension)
			{
				case esriGeometryDimension.esriGeometry0Dimension:
					return 0;

				case esriGeometryDimension.esriGeometry1Dimension:
					return 1;

				case esriGeometryDimension.esriGeometry2Dimension:
					return 2;

				case esriGeometryDimension.esriGeometry25Dimension:
					return 2.5;

				case esriGeometryDimension.esriGeometry3Dimension:
					return 3;

				case esriGeometryDimension.esriGeometryNoDimension:
					return DBNull.Value;

				default:
					throw new ArgumentOutOfRangeException(nameof(dimension), dimension,
					                                      @"invalid dimension");
			}
		}

		[NotNull]
		private static object GetPartCount([CanBeNull] IGeometry geometry,
		                                   [NotNull] PropertyCache propertyCache)
		{
			if (geometry == null || geometry.IsEmpty)
			{
				return 0;
			}

			var parts = geometry as IGeometryCollection;
			return parts?.GeometryCount ?? 1;
		}

		[NotNull]
		private static object GetVertexCount([CanBeNull] IGeometry geometry,
		                                     [NotNull] PropertyCache propertyCache)
		{
			return GetVertexCountCore(geometry);
		}

		private static int GetVertexCountCore(IGeometry geometry)
		{
			if (geometry == null || geometry.IsEmpty)
			{
				return 0;
			}

			var points = geometry as IPointCollection;
			return points?.PointCount ?? 1;
		}

		private static object GetXMin(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.XMin ?? (object) DBNull.Value;
		}

		private static object GetYMin(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.YMin ?? (object) DBNull.Value;
		}

		private static object GetXMax(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.XMax ?? (object) DBNull.Value;
		}

		private static object GetYMax(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.YMax ?? (object) DBNull.Value;
		}

		private static object GetZMin(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.ZMin ?? (object) DBNull.Value;
		}

		private static object GetZMax(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.ZMax ?? (object) DBNull.Value;
		}

		private static object GetMMin(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.MMin ?? (object) DBNull.Value;
		}

		private static object GetMMax(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.MMax ?? (object) DBNull.Value;
		}

		private static object GetIsPointIdAware(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.IsPointIdAware;
		}

		private static object GetPointIdMin(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.PointIdMin ?? 0;
		}

		private static object GetPointIdMax(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.PointIdMax ?? 0;
		}

		private static object GetPointIdCount(IGeometry geometry, PropertyCache propertyCache)
		{
			return propertyCache.PointIdCount ?? 0;
		}


		[NotNull]
		private static object GetSegmentCount([CanBeNull] IGeometry geometry,
		                                      [NotNull] PropertyCache propertyCache)
		{
			return propertyCache.SegmentCount;
		}

		[NotNull]
		private static object GetNonLinearSegmentCount([CanBeNull] IGeometry geometry,
		                                               [NotNull] PropertyCache propertyCache)
		{
			return propertyCache.NonLinearSegmentCount;
		}

		[NotNull]
		private static object GetUndefinedMValueCount([CanBeNull] IGeometry geometry,
		                                              [NotNull] PropertyCache propertyCache)
		{
			if (geometry == null || geometry.IsEmpty)
			{
				return 0;
			}

			int vertexCount = GetVertexCountCore(geometry);

			if (! GeometryUtils.IsMAware(geometry))
			{
				// all vertices have undefined M values
				return vertexCount;
			}

			return GeometryUtils.GetPoints(geometry, recycle: true)
			                    .Count(point => double.IsNaN(point.M));
		}

		[NotNull]
		private static object GetLinearSegmentCount([CanBeNull] IGeometry geometry,
		                                            [NotNull] PropertyCache propertyCache)
		{
			return propertyCache.LinearSegmentCount;
		}

		[NotNull]
		private static object GetEllipticArcCount([CanBeNull] IGeometry geometry,
		                                          [NotNull] PropertyCache propertyCache)
		{
			return propertyCache.EllipticArcCount;
		}

		[NotNull]
		private static object GetCircularArcCount([CanBeNull] IGeometry geometry,
		                                          [NotNull] PropertyCache propertyCache)
		{
			return propertyCache.CircularArcCount;
		}

		[NotNull]
		private static object GetBezierCount([CanBeNull] IGeometry geometry,
		                                     [NotNull] PropertyCache propertyCache)
		{
			return propertyCache.BezierCount;
		}

		[NotNull]
		private static object GetSliverRatio([CanBeNull] IGeometry geometry,
		                                     [NotNull] PropertyCache propertyCache)
		{
			if (geometry == null)
			{
				return DBNull.Value;
			}

			return GeometryProperties.GetSliverRatio(geometry) ?? (object) DBNull.Value;
		}

		[NotNull]
		private static object GetLength([CanBeNull] IGeometry geometry,
		                                [NotNull] PropertyCache propertyCache)
		{
			return geometry == null
				       ? 0
				       : GeometryProperties.GetLength(geometry);
		}

		[NotNull]
		private static object GetArea([CanBeNull] IGeometry geometry,
		                              [NotNull] PropertyCache propertyCache)
		{
			return geometry == null
				       ? 0
				       : GeometryProperties.GetArea(geometry);
		}

		[NotNull]
		private static object GetIsMultipart([CanBeNull] IGeometry geometry,
		                                     [NotNull] PropertyCache propertyCache)
		{
			return geometry != null && GeometryProperties.IsMultipart(geometry);
		}

		[NotNull]
		private static object GetIsClosed([CanBeNull] IGeometry geometry,
		                                  [NotNull] PropertyCache propertyCache)
		{
			if (geometry == null)
			{
				return DBNull.Value;
			}

			return GeometryProperties.IsClosed(geometry) ?? (object) DBNull.Value;
		}

		[NotNull]
		private static object GetExteriorRingCount([CanBeNull] IGeometry geometry,
		                                           [NotNull] PropertyCache propertyCache)
		{
			return propertyCache.ExteriorRingCount;
		}

		[NotNull]
		private static object GetInteriorRingCount([CanBeNull] IGeometry geometry,
		                                           [NotNull] PropertyCache propertyCache)
		{
			return propertyCache.InteriorRingCount;
		}

		[NotNull]
		private static object GetIsInteriorRing([CanBeNull] IGeometry geometry,
		                                        [NotNull] PropertyCache propertyCache)
		{
			// only works for polygons, multipatch rings don't know on their own (must ask multipatch)
			var ring = geometry as IRing;
			return ring != null && ! ring.IsExterior;
		}

		[NotNull]
		private static object GetIsExteriorRing([CanBeNull] IGeometry geometry,
		                                        [NotNull] PropertyCache propertyCache)
		{
			// only works for polygons, multipatch rings don't know on their own (must ask multipatch)
			var ring = geometry as IRing;
			return ring != null && ring.IsExterior;
		}

		private static object GetRingCount(IGeometry geometry, PropertyCache propertyCache)
		{
			return geometry == null
				       ? 0
				       : GeometryProperties.GetRingCount(geometry);
		}

		private static object GetTrianglesPatchCount(IGeometry geometry,
		                                             PropertyCache propertyCache)
		{
			return geometry == null
				       ? 0
				       : GeometryProperties.GetTrianglesPatchCount(geometry);
		}

		private static object GetTriangleStripCount(IGeometry geometry,
		                                            PropertyCache propertyCache)
		{
			return geometry == null
				       ? 0
				       : GeometryProperties.GetTriangleStripCount(geometry);
		}

		private static object GetTriangleFanCount(IGeometry geometry,
		                                          PropertyCache propertyCache)
		{
			return geometry == null
				       ? 0
				       : GeometryProperties.GetTriangleFanCount(geometry);
		}

		private static bool UsesField([NotNull] string constraint,
		                              [NotNull] string fieldName)
		{
			return constraint.IndexOf(fieldName, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static double? NaNtoNull(double? value)
		{
			return value == null ? null : NaNtoNull(value.Value);
		}

		private static double? NaNtoNull(double value)
		{
			return double.IsNaN(value) ? (double?) null : value;
		}

		private class PropertyCache
		{
			[CanBeNull] private readonly IGeometry _geometry;
			[CanBeNull] private SegmentCounts _segmentCounts;
			[CanBeNull] private readonly ISegmentCollection _segments;
			private int? _interiorRingCount;
			private int? _exteriorRingCount;
			private readonly bool _zAware;
			private readonly bool _mAware;
			private readonly bool _isEmpty;
			private readonly bool _isPointIdAware;
			private int? _pointIdMin;
			private int? _pointIdMax;
			private int? _pointIdCount;
			private IEnvelope _envelope;

			public PropertyCache([CanBeNull] IGeometry geometry)
			{
				_geometry = geometry;
				_segments = geometry as ISegmentCollection;
				_isEmpty = geometry == null || geometry.IsEmpty;
				_zAware = geometry != null && GeometryUtils.IsZAware(geometry);
				_mAware = geometry != null && GeometryUtils.IsMAware(geometry);
				_isPointIdAware = geometry != null && GeometryUtils.IsPointIDAware(geometry);
			}

			public double? XMin => Envelope?.XMin;

			public double? YMin => Envelope?.YMin;

			public double? XMax => Envelope?.XMax;

			public double? YMax => Envelope?.YMax;

			public double? MMin => _mAware ? NaNtoNull(Envelope?.MMin) : null;

			public double? MMax => _mAware ? NaNtoNull(Envelope?.MMax) : null;

			public double? ZMin => _zAware ? NaNtoNull(Envelope?.ZMin) : null;

			public double? ZMax => _zAware ? NaNtoNull(Envelope?.ZMax) : null;

			public bool IsPointIdAware => _isPointIdAware;

			public int? PointIdMin => PointIdMinCore;

			public int? PointIdMax => PointIdMaxCore;

			public int? PointIdCount => PointIdCountCore;

			public int SegmentCount => _segments?.SegmentCount ?? 0;

			public int LinearSegmentCount => SegmentCounts?.LinearSegmentCount ?? 0;

			public int NonLinearSegmentCount => SegmentCount - LinearSegmentCount;

			public int CircularArcCount => SegmentCounts?.CircularArcCount ?? 0;

			public int BezierCount => SegmentCounts?.BezierCount ?? 0;

			public int EllipticArcCount => SegmentCounts?.EllipticArcCount ?? 0;

			[CanBeNull]
			private IEnvelope Envelope
			{
				get
				{
					if (_envelope == null && _geometry != null && ! _isEmpty)
					{
						_envelope = _geometry.Envelope;
					}

					return _envelope;
				}
			}

			[CanBeNull]
			private SegmentCounts SegmentCounts
			{
				get
				{
					if (_segmentCounts == null && _segments != null)
					{
						_segmentCounts = GeometryProperties.GetSegmentCounts(_segments);
					}

					return _segmentCounts;
				}
			}

			public int ExteriorRingCount
			{
				get
				{
					if (_exteriorRingCount == null)
					{
						_exteriorRingCount = _geometry == null
							                     ? 0
							                     : GeometryProperties.GetExteriorRingCount(
								                     _geometry);
					}

					return _exteriorRingCount.Value;
				}
			}

			public int InteriorRingCount
			{
				get
				{
					if (_interiorRingCount == null)
					{
						_interiorRingCount = _geometry == null
							                     ? 0
							                     : GeometryProperties.GetInteriorRingCount(
								                     _geometry);
					}

					return _interiorRingCount.Value;
				}
			}

			private int PointIdMinCore
			{
				get
				{
					if (_pointIdMin == null)
					{
						InitPointIds();
					}

					return Assert.NotNull(_pointIdMin).Value;
				}
			}

			private int PointIdMaxCore
			{
				get
				{
					if (_pointIdMax == null)
					{
						InitPointIds();
					}

					return Assert.NotNull(_pointIdMax).Value;
				}
			}

			private int PointIdCountCore
			{
				get
				{
					if (_pointIdCount == null)
					{
						InitPointIds();
					}

					return Assert.NotNull(_pointIdCount).Value;
				}
			}

			private void InitPointIds()
			{
				if (_geometry == null || _geometry.IsEmpty)
				{
					_pointIdMin = 0;
					_pointIdMax = 0;
					_pointIdCount = 0;
					return;
				}

				if (_geometry is IPointCollection points && points.PointCount > 0)
				{
					int min = int.MaxValue;
					int max = int.MinValue;
					int n = 0;
					IEnumVertex pts = points.EnumVertices;

					do
					{
						pts.Next(out IPoint p, out _, out _);
						if (p == null)
						{
							break;
						}

						int id = p.ID;
						min = Math.Min(min, id);
						max = Math.Max(max, id);
						if (id != 0)
						{
							n++;
						}
					} while (true);


					_pointIdMin = min;
					_pointIdMax = max;
					_pointIdCount = n;
				}
				else if (_geometry is IPoint p)
				{
					_pointIdMin = p.ID;
					_pointIdMax = _pointIdMin;
					_pointIdCount = _pointIdMin == 0 ? 0 : 1;
				}
				else
				{
					_pointIdMin = 0;
					_pointIdMax = 0;
					_pointIdCount = 0;
				}
			}
		}

		private class ColumnHandler<T>
		{
			[NotNull] private readonly Type _type;
			[NotNull] private readonly Func<IGeometry, T, object> _valueFunction;

			[NotNull] private readonly Func<IGeometry, object, IFormatProvider, string>
				_formatFunction;

			public ColumnHandler(
				[NotNull] string columnName,
				[NotNull] Type type,
				[NotNull] Func<IGeometry, T, object> valueFunction,
				[CanBeNull] string valueFormat = null)
				: this(columnName, type, valueFunction, GetFormatFunction(valueFormat)) { }

			public ColumnHandler(
				[NotNull] string columnName,
				[NotNull] Type type,
				[NotNull] Func<IGeometry, T, object> valueFunction,
				[NotNull] Func<IGeometry, object, IFormatProvider, string> formatFunction)
			{
				Assert.ArgumentNotNullOrEmpty(columnName, nameof(columnName));
				Assert.ArgumentNotNull(type, nameof(type));
				Assert.ArgumentNotNull(valueFunction, nameof(valueFunction));
				Assert.ArgumentNotNull(formatFunction, nameof(formatFunction));

				ColumnName = columnName;
				_type = type;
				_valueFunction = valueFunction;
				_formatFunction = formatFunction;
			}

			[NotNull]
			public string ColumnName { get; }

			[NotNull]
			public DataColumn CreateColumn()
			{
				return new DataColumn(ColumnName, _type);
			}

			[NotNull]
			public object GetValue([CanBeNull] IGeometry geometry,
			                       [NotNull] T propertyCache)
			{
				return _valueFunction(geometry, propertyCache);
			}

			[NotNull]
			public string FormatValue([CanBeNull] IGeometry geometry,
			                          [NotNull] IFormatProvider formatProvider,
			                          [NotNull] T propertyCache)
			{
				object value = GetValue(geometry, propertyCache);

				return _formatFunction(geometry, value, formatProvider);
			}

			[NotNull]
			private static Func<IGeometry, object, IFormatProvider, string> GetFormatFunction(
				[CanBeNull] string valueFormat)
			{
				return (geometry, value, formatProvider) =>
				{
					if (value is DBNull)
					{
						return "<NULL>";
					}

					string format;
					if (valueFormat == null)
					{
						format = value is int ? "{0:N0}" : "{0}";
					}
					else
					{
						format = valueFormat;
					}

					return string.Format(
						formatProvider,
						format, value);
				};
			}
		}
	}
}
