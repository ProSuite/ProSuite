using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestSupport;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMinSegAngle : ContainerTest
	{
		private const bool _defaultUseTangents = false;
		private const AngleUnit _defaultAngularUnit = DefaultAngleUnit;

		[NotNull] private readonly string _shapeFieldName;
		private readonly esriGeometryType _shapeType;
		[NotNull] private readonly Settings _settings;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string SegmentAngleTooSmall = "SegmentAngleTooSmall";

			public Code() : base("MinimumSegmentAngle") { }
		}

		#endregion

		[Doc(nameof(DocStrings.QaMinSegAngle_0))]
		public QaMinSegAngle(
			[Doc(nameof(DocStrings.QaMinSegAngle_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[Doc(nameof(DocStrings.QaMinSegAngle_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinSegAngle_is3D))]
			bool is3D)
			: base(featureClass)
		{
			_settings = new Settings(limit, _defaultAngularUnit, is3D);
			_shapeFieldName = featureClass.ShapeFieldName;
			_shapeType = featureClass.ShapeType;

			UseTangents = _defaultUseTangents;
			AngularUnit = _defaultAngularUnit;
		}

		[Doc(nameof(DocStrings.QaMinSegAngle_0))]
		public QaMinSegAngle(
				[Doc(nameof(DocStrings.QaMinSegAngle_featureClass))]
				IReadOnlyFeatureClass featureClass,
				[Doc(nameof(DocStrings.QaMinSegAngle_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, limit, false) { }

		[InternallyUsedTest]
		public QaMinSegAngle(
			[NotNull] QaMinSegAngleDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass,
			       definition.Limit, definition.Is3D)
		{
			UseTangents = definition.UseTangents;
			AngularUnit = definition.AngularUnit;
		}

		[TestParameter(_defaultUseTangents)]
		[Doc(nameof(DocStrings.QaMinSegAngle_UseTangents))]
		public bool UseTangents { get; set; }

		[TestParameter(_defaultAngularUnit)]
		[Doc(nameof(DocStrings.QaMinSegAngle_AngularUnit))]
		public AngleUnit AngularUnit
		{
			get { return AngleUnit; }
			set
			{
				AngleUnit = value;
				_settings.AngularUnit = value;
			}
		}

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			IGeometry shape = ((IReadOnlyFeature) row).Shape;

			switch (_shapeType)
			{
				case esriGeometryType.esriGeometryPolyline:
					return CheckPolyline((IPolyline) shape, row);

				case esriGeometryType.esriGeometryPolygon:
					return CheckPolygon((IPolygon) shape, row);

				case esriGeometryType.esriGeometryMultiPatch:
					return CheckMultiPatch((IMultiPatch) shape, row);

				default:
					return NoError;
			}
		}

		private int CheckMultiPatch([NotNull] IMultiPatch shape, [NotNull] IReadOnlyRow row)
		{
			var errorCount = 0;

			foreach (IGeometry part in GeometryUtils.GetParts((IGeometryCollection) shape))
			{
				var ring = part as IRing;
				if (ring != null)
				{
					errorCount += CheckAngles((IPointCollection) ring, row);
				}

				Marshal.ReleaseComObject(part);
			}

			return errorCount;
		}

		private int CheckPolygon([NotNull] IPolygon shape, [NotNull] IReadOnlyRow row)
		{
			var errorCount = 0;

			foreach (IRing ring in GeometryUtils.GetRings(shape))
			{
				errorCount += CheckAngles((IPointCollection) ring, row);

				Marshal.ReleaseComObject(ring);
			}

			return errorCount;
		}

		private int CheckPolyline([NotNull] IPolyline shape, [NotNull] IReadOnlyRow row)
		{
			var errorCount = 0;

			foreach (IPath path in GeometryUtils.GetPaths(shape))
			{
				errorCount += CheckAngles((IPointCollection) path, row);

				Marshal.ReleaseComObject(path);
			}

			return errorCount;
		}

		private int CheckAngles([NotNull] IPointCollection points, [NotNull] IReadOnlyRow row)
		{
			AngleProvider provider = GetAngleProvider(points);

			var errorCount = 0;

			foreach (AngleInfo angleInfo in provider.GetAngles())
			{
				double angleRadians = -1;
				double prod = angleInfo.ScalarProduct;
				double l02 = angleInfo.L02;
				double l12 = angleInfo.L12;

				if (prod < 0) // the two segments build an acute angle
				{
					double cos2 = prod * prod / (l02 * l12);
					if (cos2 > _settings.LimitCos2 || _settings.LimitGtPi2)
					{
						angleRadians = Math.Acos(Math.Sqrt(cos2));
					}
				}
				else if (_settings.LimitGtPi2)
				{
					double cos2 = prod * prod / (l02 * l12);
					if (cos2 < _settings.LimitCos2)
					{
						angleRadians = Math.PI - Math.Acos(Math.Sqrt(cos2));
					}
				}

				if (angleRadians >= 0)
				{
					string format = FormatUtils.CompareFormat(angleRadians, "<", _settings.LimitRad,
					                                          "N2");

					// TODO: use AngleUnits dependent formating once allowed error handling is independent of description
					// --> uncomment
					//string format = 
					//FormatUtils.CompareFormat(
					//FormatUtils.Radians2AngleInUnits(angle, AngleUnit), "<",
					//_settings.LimitInUnits, "N2");

					string description = string.Format("Segment angle {0} < {1}",
					                                   FormatAngle(angleRadians, format),
					                                   FormatAngle(_settings.LimitRad, format));
					errorCount += ReportError(
						description, InvolvedRowUtils.GetInvolvedRows(row),
						CreateErrorPoint(angleInfo), Codes[Code.SegmentAngleTooSmall],
						_shapeFieldName,
						values: new object[] { MathUtils.ToDegrees(angleRadians) });
				}
			}

			return errorCount;
		}

		[NotNull]
		private static IPoint CreateErrorPoint([NotNull] AngleInfo angleInfo)
		{
			IPoint result = new PointClass();

			result.PutCoords(angleInfo.X, angleInfo.Y);
			result.Z = angleInfo.Z;

			return result;
		}

		private static bool IsZeroLength([NotNull] ISegment segment)
		{
			return MathUtils.AreSignificantDigitsEqual(segment.Length, 0d);
		}

		[NotNull]
		private AngleProvider GetAngleProvider([NotNull] IPointCollection points)
		{
			var segments = points as ISegmentCollection;

			if (segments == null || ! UseTangents)
			{
				return new LinearAngleProvider(points, _settings);
			}

			return GeometryUtils.HasNonLinearSegments(segments)
				       ? (AngleProvider) new TangentAngleProvider(segments, _settings)
				       : new LinearAngleProvider(points, _settings);
		}

		#region Nested types

		/// <summary>
		/// use private or by unit tests only
		/// </summary>
		private abstract class AngleProvider
		{
			protected AngleProvider([NotNull] Settings settings)
			{
				Assert.ArgumentNotNull(settings, nameof(settings));

				Settings = settings;
			}

			[NotNull]
			public abstract IEnumerable<AngleInfo> GetAngles();

			[NotNull]
			protected Settings Settings { get; }
		}

		private class LinearAngleProvider : AngleProvider
		{
			[NotNull] private readonly IPointCollection _points;

			public LinearAngleProvider([NotNull] IPointCollection points,
			                           [NotNull] Settings settings)
				: base(settings)
			{
				Assert.ArgumentNotNull(points, nameof(points));

				_points = points;
			}

			public override IEnumerable<AngleInfo> GetAngles()
			{
				int partIndex;
				int vertexIndex;

				double x2 = 0;
				double y2 = 0;
				double z2 = 0;
				double dx1 = 0;
				double dy1 = 0;
				double dz1 = 0;
				double l12 = 0;
				var minVertex = 2;

				if (((ICurve) _points).IsClosed)
				{
					var segments = _points as ISegmentCollection;
					if (segments != null)
					{
						int segmentCount = segments.SegmentCount;
						if (segmentCount == 1)
						{
							ISegment segment = segments.Segment[0];

							if (IsZeroLength(segment))
							{
								// single zero-length segment forming a closed curve, ignore

								// NOTE: closed elliptic arcs also report length = 0, unable to calculate tangents --> ignored here
								yield break;
							}

							AngleInfo info = AngleInfo.Create(segment, segment, Settings.Is3D);
							yield return info;
							yield break;
						}

						if (segmentCount == 2)
						{
							ISegment segment0 = segments.Segment[0];
							ISegment segment1 = segments.Segment[1];

							if (! (segment0 is ILine) || ! (segment1 is ILine))
							{
								// two segment closed curve, at least one of the segments is non-linear
								// --> calculate using tangent
								// otherwise, linearized segment angles would always be 0, resulting in errors 
								bool segment0IsZeroLength = IsZeroLength(segment0);
								bool segment1IsZeroLength = IsZeroLength(segment1);

								if (! segment0IsZeroLength && ! segment1IsZeroLength)
								{
									yield return
										AngleInfo.Create(segment0, segment1, Settings.Is3D);
									yield return
										AngleInfo.Create(segment1, segment0, Settings.Is3D);
								}
								else if (segment0IsZeroLength)
								{
									yield return
										AngleInfo.Create(segment1, segment1, Settings.Is3D);
								}
								else
								{
									// segment 1 is zero length
									yield return
										AngleInfo.Create(segment0, segment0, Settings.Is3D);
								}

								yield break;
							}
						}
					}

					_points.QueryPoint(_points.PointCount - 2, Settings.QueryPoint);

					Settings.QueryPoint.QueryCoords(out x2, out y2);
					z2 = Settings.QueryPoint.Z;
					minVertex = 1;
				}

				IEnumVertex vertexEnum = _points.EnumVertices;
				vertexEnum.QueryNext(Settings.QueryPoint, out partIndex, out vertexIndex);

				while (partIndex >= 0 && vertexIndex >= 0)
				{
					double dx0 = dx1;
					double dy0 = dy1;
					double l02 = l12;

					double x1 = x2;
					double y1 = y2;
					double z1 = z2;

					Settings.QueryPoint.QueryCoords(out x2, out y2);
					z2 = Settings.QueryPoint.Z;

					dx1 = x2 - x1;
					dy1 = y2 - y1;

					l12 = dx1 * dx1 + dy1 * dy1;
					double prod = dx0 * dx1 + dy0 * dy1;

					if (Settings.Is3D)
					{
						double dz0 = dz1;
						dz1 = z2 - z1;

						l12 += dz1 * dz1;

						prod += dz0 * dz1;
					}

					if (vertexIndex < minVertex)
					{
						vertexEnum.QueryNext(Settings.QueryPoint, out partIndex, out vertexIndex);
						continue;
					}

					var angleInfo = new AngleInfo(x1, y1, z1, l02, l12, prod);
					yield return angleInfo;

					vertexEnum.QueryNext(Settings.QueryPoint, out partIndex, out vertexIndex);
				}
			}
		}

		private class TangentAngleProvider : AngleProvider
		{
			[NotNull] private readonly ISegmentCollection _segments;

			public TangentAngleProvider([NotNull] ISegmentCollection segments,
			                            [NotNull] Settings settings)
				: base(settings)
			{
				Assert.ArgumentNotNull(segments, nameof(segments));

				_segments = segments;
			}

			public override IEnumerable<AngleInfo> GetAngles()
			{
				ISegment pre = null;

				if (((ICurve) _segments).IsClosed)
				{
					pre = GetLastNonZeroLengthSegment(_segments);
				}

				IEnumSegment enumSegments = _segments.EnumSegments;
				bool recycling = enumSegments.IsRecycling;

				ISegment segment;
				var partIndex = 0;
				var segmentIndex = 0;

				enumSegments.Next(out segment, ref partIndex, ref segmentIndex);

				while (segment != null)
				{
					// ignore zero-length segments
					if (! IsZeroLength(segment))
					{
						ISegment next = recycling
							                ? GeometryFactory.Clone(segment)
							                : segment;

						if (pre != null)
						{
							yield return AngleInfo.Create(pre, next, Settings.Is3D);
						}

						pre = next;
					}

					if (recycling)
					{
						// release the segment, otherwise "pure virtual function call" occurs 
						// when there are certain circular arcs (IsLine == true ?)
						Marshal.ReleaseComObject(segment);
					}

					enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
				}
			}

			[CanBeNull]
			private static ISegment GetLastNonZeroLengthSegment(
				[NotNull] ISegmentCollection segments)
			{
				int lastSegmentIndex = segments.SegmentCount - 1;

				for (int i = lastSegmentIndex; i >= 0; i--)
				{
					ISegment segment = segments.Segment[i];
					if (! IsZeroLength(segment))
					{
						return segment;
					}
				}

				return null;
			}
		}

		private class Settings
		{
			private AngleUnit _angularUnit;

			public Settings(double limitInUnits, AngleUnit angularUnit, bool is3D)
			{
				LimitInUnits = limitInUnits;
				Is3D = is3D;
				QueryPoint = new PointClass();

				AngularUnit = angularUnit;
			}

			public AngleUnit AngularUnit
			{
				[UsedImplicitly] get { return _angularUnit; }
				set
				{
					_angularUnit = value;
					LimitRad = FormatUtils.AngleInUnits2Radians(LimitInUnits, _angularUnit);
					double limitCos = Math.Cos(LimitRad);
					LimitGtPi2 = limitCos < 0;
					LimitCos2 = limitCos * limitCos;
				}
			}

			public bool Is3D { get; }

			[NotNull]
			public IPoint QueryPoint { get; }

			[UsedImplicitly]
			public double LimitInUnits { get; }

			public double LimitRad { get; private set; }

			public double LimitCos2 { get; private set; }

			public bool LimitGtPi2 { get; private set; }
		}

		private class AngleInfo
		{
			public double X { get; }

			public double Y { get; }

			public double Z { get; }

			public double L02 { get; }

			public double L12 { get; }

			public double ScalarProduct { get; }

			[ThreadStatic] private static ILine2 _tangent;

			public AngleInfo(double x, double y, double z,
			                 double l02, double l12,
			                 double scalarProduct)
			{
				X = x;
				Y = y;
				Z = z;
				L02 = l02;
				L12 = l12;
				ScalarProduct = scalarProduct;
			}

			[NotNull]
			public static AngleInfo Create([NotNull] ISegment preSegment,
			                               [NotNull] ISegment nextSegment,
			                               bool is3D)
			{
				const double tangentLength = 1;
				preSegment.QueryTangent(esriSegmentExtension.esriNoExtension,
				                        DistanceAlongCurve: 1,
				                        asRatio: true,
				                        Length: tangentLength,
				                        tangent: Tangent);
				WKSPoint at;
				WKSPoint preTo;
				Tangent.QueryWKSCoords(out at, out preTo);
				double l02 = tangentLength;

				WKSPoint nextTo;
				nextSegment.QueryTangent(esriSegmentExtension.esriNoExtension,
				                         DistanceAlongCurve: 0,
				                         asRatio: true,
				                         Length: tangentLength,
				                         tangent: Tangent);
				Tangent.QueryWKSCoords(out at, out nextTo);
				double l12 = tangentLength;

				// Tangent points (preTo, nextTo) are always in direction of segment -> always take the differnce to 'at'.
				double scalarProd = (preTo.X - at.X) * (nextTo.X - at.X) +
				                    (preTo.Y - at.Y) * (nextTo.Y - at.Y);

				double nextSegmentFromZ;
				double nextSegmentToZ;
				((ISegmentZ) nextSegment).GetZs(out nextSegmentFromZ, out nextSegmentToZ);

				if (is3D)
				{
					double preLength = preSegment.Length; // Segment.Length = 2D length
					double nextLength = nextSegment.Length;

					// Use directed differences.
					double preSegmentFromZ;
					double preSegmentToZ;
					((ISegmentZ) preSegment).GetZs(out preSegmentFromZ, out preSegmentToZ);

					double preZ = (nextSegmentFromZ - preSegmentFromZ) / preLength;
					double nextZ = (nextSegmentToZ - nextSegmentFromZ) / nextLength;

					scalarProd += preZ * nextZ;
					l02 += preZ * preZ;
					l12 += nextZ * nextZ;
				}

				return new AngleInfo(at.X, at.Y, nextSegmentFromZ, l02, l12, scalarProd);
			}

			[NotNull]
			private static ILine2 Tangent => _tangent ?? (_tangent = new LineClass());
		}

		#endregion
	}
}
