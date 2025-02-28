using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.AO.Properties;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geometry
{
	public static class GeometryUtils
	{
		// private const double _searchRadiusMultiplyer = 5.0;

		/// <summary>
		/// default search radius when a geometry has no spatial reference (1cm)
		/// </summary>
		private const double _defaultSearchRadius = 0.01; // TODO eliminate need for this!

		private const int _maxToStringPartCount = 100;
		private const int _maxToStringPointCount = 1000;

		// The geometry errors's int value do not correspond with the error codes
		private const int _esriGeometryErrorInconsistentSpatialRel = -2147220971;
		private const int _esriGeometryErrorNotSimple = -2147220968;

		private const int _esriGeometryErrorUnknownError = -2147220930;
		// NOTE: empirically determined. The official hex value is no int

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _geometryEnvironmentProgID = "esriGeometry.GeometryEnvironment";

		/// <summary></summary>
		/// <remarks>always access via the corresponding property</remarks>
		[CanBeNull] [ThreadStatic] private static IGeometryBridge _geometryBridge;

		/// <summary></summary>
		/// <remarks>always access via the corresponding property</remarks>
		[CanBeNull] [ThreadStatic] private static IPoint _pointTemplate;

		/// <summary></summary>
		/// <remarks>always access via the corresponding property</remarks>
		[CanBeNull] [ThreadStatic] private static IPoint _simplifyPointTemplate;

		/// <summary></summary>
		/// <remarks>always access via the corresponding property</remarks>
		[CanBeNull] [ThreadStatic] private static IPoint _getZValueFromSegmentFromPoint;

		/// <summary></summary>
		/// <remarks>always access via the corresponding property</remarks>
		[CanBeNull] [ThreadStatic] private static IPoint _getZValueFromSegmentToPoint;

		[NotNull]
		private static IPoint PointTemplate
		{
			get { return _pointTemplate ?? (_pointTemplate = new PointClass()); }
		}

		[NotNull]
		private static IPoint SimplifyPointTemplate
			=> _simplifyPointTemplate ?? (_simplifyPointTemplate = new PointClass());

		[NotNull]
		private static IPoint GetZValueFromSegmentFromPoint
			=> _getZValueFromSegmentFromPoint ??
			   (_getZValueFromSegmentFromPoint = new PointClass());

		[NotNull]
		private static IPoint GetZValueFromSegmentToPoint => _getZValueFromSegmentToPoint ??
		                                                     (_getZValueFromSegmentToPoint =
			                                                      new PointClass());

		public static double GetPointDistance([NotNull] IPoint firstPoint,
		                                      [NotNull] IPoint secondPoint)
		{
			Assert.ArgumentNotNull(firstPoint, nameof(firstPoint));
			Assert.ArgumentNotNull(secondPoint, nameof(secondPoint));

			return ((IProximityOperator) firstPoint).ReturnDistance(secondPoint);
		}

		public static double GetPointDistance3D([NotNull] IPoint firstPoint,
		                                        [NotNull] IPoint secondPoint)
		{
			Assert.ArgumentNotNull(firstPoint, nameof(firstPoint));
			Assert.ArgumentNotNull(secondPoint, nameof(secondPoint));

			return IsZAware(firstPoint) && IsZAware(secondPoint)
				       ? ((IProximityOperator3D) firstPoint).ReturnDistance3D(secondPoint)
				       : GetPointDistance(firstPoint, secondPoint);
		}

		[NotNull]
		public static string Format([CanBeNull] IEnvelope envelope,
		                            bool allSignificantDigits = false)
		{
			if (envelope == null)
			{
				return "<null>";
			}

			if (envelope.IsEmpty)
			{
				return "<empty>";
			}

			double xMin;
			double yMin;
			double xMax;
			double yMax;
			envelope.QueryCoords(out xMin, out yMin, out xMax, out yMax);

			ISpatialReference spatialReference = envelope.SpatialReference;

			string format;
			if (allSignificantDigits ||
			    spatialReference == null ||
			    spatialReference is IUnknownCoordinateSystem)
			{
				format = "XMin: {0} YMin: {1} XMax: {2} YMax: {3}";
			}
			else if (spatialReference is IGeographicCoordinateSystem)
			{
				// 7 decimal places
				format = "XMin: {0:N7} YMin: {1:N7} XMax: {2:N7} YMax: {3:N7}";
			}
			else
			{
				// 2 decimal places for projected coordinate systems
				format = "XMin: {0:N2} YMin: {1:N2} XMax: {2:N2} YMax: {3:N2}";
			}

			return string.Format(format, xMin, yMin, xMax, yMax);
		}

		/// <summary>
		/// Translates the given geometryType enum value to an human readable form.
		/// </summary>
		[NotNull]
		public static string Format(esriGeometryType geometryType)
		{
			switch (geometryType)
			{
				case esriGeometryType.esriGeometryLine:
					return "Line";

				case esriGeometryType.esriGeometryPoint:
					return "Point";

				case esriGeometryType.esriGeometryMultipoint:
					return "Multi-Point";

				case esriGeometryType.esriGeometryPolygon:
					return "Polygon";

				case esriGeometryType.esriGeometryPolyline:
					return "Polyline";

				case esriGeometryType.esriGeometryEnvelope:
					return "Envelope";

				case esriGeometryType.esriGeometryAny:
					return "Any";

				case esriGeometryType.esriGeometryBag:
					return "Bag";

				case esriGeometryType.esriGeometryBezier3Curve:
					return "Bezier-3-curve";

				case esriGeometryType.esriGeometryCircularArc:
					return "Circular-Arc";

				case esriGeometryType.esriGeometryEllipticArc:
					return "Elliptic-Arc";

				case esriGeometryType.esriGeometryMultiPatch:
					return "Multi-Patch";

				case esriGeometryType.esriGeometryNull:
					return "NULL";

				case esriGeometryType.esriGeometryPath:
					return "Path";

				case esriGeometryType.esriGeometryRay:
					return "Ray";

				case esriGeometryType.esriGeometryRing:
					return "Ring";

				case esriGeometryType.esriGeometrySphere:
					return "Sphere";

				case esriGeometryType.esriGeometryTriangleFan:
					return "Triangle-Fan";

				case esriGeometryType.esriGeometryTriangles:
					return "Triangles";

				case esriGeometryType.esriGeometryTriangleStrip:
					return "Triangle-Strip";

				default:
					return $"Unknown geometry type: {geometryType}";
			}
		}

		/// <summary>
		/// Returns a <see cref="System.String"></see> that represents the specified <see cref="IEnvelope"></see>.
		/// </summary>
		/// <param name="envelope">The envelope to convert to a string.</param>
		/// <param name="withoutSpatialReference">If <c>true</c>, the spatial reference information
		/// is not included. Otherwise, full spatial reference information is added to the string.
		/// </param>
		/// <returns>
		/// A <see cref="System.String"></see> that represents the specified <see cref="IEnvelope"></see>.
		/// </returns>
		[NotNull]
		public static string ToString([CanBeNull] IEnvelope envelope,
		                              bool withoutSpatialReference = false)
		{
			try
			{
				var sb = new StringBuilder();

				sb.AppendLine("[Envelope]");
				if (envelope == null)
				{
					sb.AppendLine("Envelope is null");
				}
				else if (envelope.IsEmpty)
				{
					sb.AppendLine("Envelope is empty");
				}
				else
				{
					sb.AppendLine(Format(envelope));

					if (! withoutSpatialReference)
					{
						sb.Append(SpatialReferenceUtils.ToString(envelope.SpatialReference));
					}
				}

				return sb.ToString();
			}
			catch (Exception e)
			{
				return HandleToStringException(e);
			}
		}

		/// <summary>
		/// Returns a <see cref="System.String"></see> that represents the specified <see cref="IGeometry"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"></see> that represents the specified <see cref="IGeometry"></see>.
		/// </returns>
		[NotNull]
		public static string ToString([CanBeNull] IGeometry geometry)
		{
			if (geometry == null)
			{
				return "Geometry is null";
			}

			try
			{
				if (geometry is IEnvelope)
				{
					return ToString((IEnvelope) geometry);
				}

				if (geometry is IPolygon)
				{
					return ToString((IPolygon) geometry);
				}

				if (geometry is IPolyline)
				{
					return ToString((IPolyline) geometry);
				}

				if (geometry is IPoint)
				{
					return ToString((IPoint) geometry);
				}

				if (geometry is IMultipoint)
				{
					return ToString((IMultipoint) geometry);
				}

				if (geometry is IMultiPatch)
				{
					return ToString((IMultiPatch) geometry);
				}

				if (geometry is IPath)
				{
					return ToString((IPath) geometry);
				}

				if (geometry is IPointCollection)
				{
					return ToString((IPointCollection) geometry, _maxToStringPointCount);
				}

				if (geometry is IEnumGeometry) // geometry bag
				{
					return ToString((IEnumGeometry) geometry);
				}

				return string.Format(
					"ToString() not yet implemented for geometry type {0}",
					geometry.GeometryType);
			}
			catch (Exception e)
			{
				return HandleToStringException(e);
			}
		}

		/// <summary>
		/// Returns a <see cref="System.String"></see> that represents the specified <see cref="IPolyline"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"></see> that represents the specified <see cref="IPolyline"></see>.
		/// </returns>
		[NotNull]
		public static string ToString([CanBeNull] IPolyline polyline)
		{
			try
			{
				var sb = new StringBuilder();
				sb.AppendLine("[Polyline]");
				if (polyline == null)
				{
					sb.AppendLine("Polyline is null");
				}
				else if (polyline.IsEmpty)
				{
					sb.AppendLine("Polyline is empty");
				}
				else
				{
					sb.AppendFormat("Length: {0}", polyline.Length);
					sb.AppendLine();

					AppendTotalPointCount(sb, polyline);
					AppendMinimumSegmentLength(sb, polyline);
					AppendIsSimple(sb, polyline);
					AppendZAware(sb, polyline);
					AppendMAware(sb, polyline);
					AppendPointIDAware(sb, polyline);
					AppendHasNonLinearSegments(sb, polyline);

					AppendParts(sb, (IGeometryCollection) polyline);

					sb.Append(SpatialReferenceUtils.ToString(polyline.SpatialReference));
				}

				return sb.ToString();
			}
			catch (Exception e)
			{
				return HandleToStringException(e);
			}
		}

		/// <summary>
		/// Returns a <see cref="System.String"></see> that represents the specified <see cref="IPolygon"></see>.
		/// </summary>
		/// <param name="polygon">The polygon to convert to a string.</param>
		/// <param name="withoutSpatialReference">If <c>true</c>, the spatial reference information
		/// is not included. Otherwise, full spatial reference information is added to the string.
		/// </param>
		/// <returns>
		/// A <see cref="System.String"></see> that represents the specified <see cref="IPolygon"></see>.
		/// </returns>
		[NotNull]
		public static string ToString([CanBeNull] IPolygon polygon,
		                              bool withoutSpatialReference = false)
		{
			try
			{
				var sb = new StringBuilder();
				sb.AppendLine("[Polygon]");
				if (polygon == null)
				{
					sb.AppendLine("Polygon is null");
				}
				else if (polygon.IsEmpty)
				{
					sb.AppendLine("Polygon is empty");
				}
				else
				{
					sb.AppendFormat("Length: {0}", polygon.Length);
					sb.AppendLine();
					sb.AppendFormat("Area: {0}", ((IArea) polygon).Area);
					sb.AppendLine();

					AppendTotalPointCount(sb, polygon);
					AppendMinimumSegmentLength(sb, polygon);
					AppendIsSimple(sb, polygon);
					AppendZAware(sb, polygon);
					AppendMAware(sb, polygon);
					AppendPointIDAware(sb, polygon);
					AppendHasNonLinearSegments(sb, polygon);

					AppendParts(sb, (IGeometryCollection) polygon);

					if (! withoutSpatialReference)
					{
						sb.Append(SpatialReferenceUtils.ToString(polygon.SpatialReference));
					}
				}

				return sb.ToString();
			}
			catch (Exception e)
			{
				return HandleToStringException(e);
			}
		}

		[NotNull]
		public static string ToString([CanBeNull] IMultiPatch multiPatch)
		{
			try
			{
				var sb = new StringBuilder();
				sb.AppendLine("[Multipatch]");

				if (multiPatch == null)
				{
					sb.AppendLine("Multipatch is null");
				}
				else if (multiPatch.IsEmpty)
				{
					sb.AppendLine("Multipatch is empty");
				}
				else
				{
					AppendTotalPointCount(sb, multiPatch);
					AppendIsSimple(sb, multiPatch);
					AppendZAware(sb, multiPatch);
					AppendMAware(sb, multiPatch);
					AppendPointIDAware(sb, multiPatch);

					var collection = (IGeometryCollection) multiPatch;

					int partCount = collection.GeometryCount;
					sb.AppendFormat("Part count: {0}", partCount);
					sb.AppendLine();
					sb.AppendFormat("Beginning ring count: {0}",
					                multiPatch.BeginningRingCount[
						                (int) esriMultiPatchRingType
							                .esriMultiPatchBeginningRingMask]);
					sb.AppendLine();

					for (var i = 0; i < partCount; i++)
					{
						IGeometry part = collection.Geometry[i];

						sb.AppendFormat("Part {0}:", i);
						sb.AppendLine();
						sb.AppendFormat("Geometry type: {0}", part.GeometryType);
						sb.AppendLine();

						if (part is IRing)
						{
							AppendMultiPatchRing(sb, (IRing) part, multiPatch);
						}
						else if (part is ITriangleFan)
						{
							AppendMultiPatchTriangleFan(sb, (ITriangleFan) part);
						}
						else if (part is ITriangleStrip)
						{
							AppendMultiPatchTriangleStrip(sb, (ITriangleStrip) part);
						}
						else if (part is ITriangles)
						{
							AppendMultiPatchTriangles(sb, (ITriangles) part);
						}
					}

					sb.Append(SpatialReferenceUtils.ToString(multiPatch.SpatialReference));

					sb.AppendLine("XY Footprint:");
					sb.AppendLine(ToString(multiPatch.XYFootprint));
				}

				return sb.ToString();
			}
			catch (Exception e)
			{
				return HandleToStringException(e);
			}
		}

		[NotNull]
		public static string ToString([CanBeNull] IMultipoint multipoint)
		{
			try
			{
				var sb = new StringBuilder();
				sb.AppendLine("[Multipoint]");

				if (multipoint == null)
				{
					sb.AppendLine("Multipoint is null");
				}
				else if (multipoint.IsEmpty)
				{
					sb.AppendLine("Multipoint is empty");
				}
				else
				{
					AppendTotalPointCount(sb, multipoint);
					AppendIsSimple(sb, multipoint);
					AppendZAware(sb, multipoint);
					AppendMAware(sb, multipoint);

					sb.Append(ToString((IPointCollection) multipoint,
					                   _maxToStringPointCount));
					sb.Append(SpatialReferenceUtils.ToString(multipoint.SpatialReference));
				}

				return sb.ToString();
			}
			catch (Exception e)
			{
				return HandleToStringException(e);
			}
		}

		[NotNull]
		public static string ToString([CanBeNull] IPoint point)
		{
			try
			{
				var sb = new StringBuilder();
				sb.AppendLine("[Point]");

				if (point == null)
				{
					sb.AppendLine("Point is null");
				}
				else if (point.IsEmpty)
				{
					sb.AppendLine("Point is empty");
				}
				else
				{
					AppendIsSimple(sb, point);
					AppendZAware(sb, point);
					AppendMAware(sb, point);

					sb.AppendFormat("{0} {1} {2} {3}",
					                point.X, point.Y, point.Z, point.M);
					sb.AppendLine();
					sb.Append(SpatialReferenceUtils.ToString(point.SpatialReference));
				}

				return sb.ToString();
			}
			catch (Exception e)
			{
				return HandleToStringException(e);
			}
		}

		[NotNull]
		public static string ToString([CanBeNull] IEnumGeometry geometryBag)
		{
			var sb = new StringBuilder();
			sb.AppendLine("[geometryBag]");

			if (geometryBag == null)
			{
				sb.AppendLine("GeometryBag is null");
			}
			else if (geometryBag.Count == 0)
			{
				sb.AppendLine("GeometryBag is empty");
			}
			else
			{
				sb.AppendFormat("GeometryBag has {0} elements", geometryBag.Count);

				geometryBag.Reset();
				IGeometry geometry;
				var idx = 0;

				while ((geometry = geometryBag.Next()) != null)
				{
					sb.AppendFormat("Geometry <index> {0}: {1}", idx++, ToString(geometry));
				}
			}

			return sb.ToString();
		}

		[NotNull]
		public static string ToString([CanBeNull] IPath path)
		{
			var sb = new StringBuilder();
			sb.AppendLine("[path]");

			if (path == null)
			{
				sb.AppendLine("Path is null");
			}
			else
			{
				AppendPart(sb, path, 0);
				sb.Append(SpatialReferenceUtils.ToString(path.SpatialReference));
			}

			return sb.ToString();
		}

		public static string ToXmlString([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			IXMLSerializer xmlSerializer = new XMLSerializerClass();
			try
			{
				return xmlSerializer.SaveToString(geometry, null, null);
			}
			finally
			{
				Marshal.ReleaseComObject(xmlSerializer);
			}
		}

		[NotNull]
		public static XmlDocument ToXmlDocument([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var doc = new XmlDocument();

			doc.LoadXml(ToXmlString(geometry));

			return doc;
		}

		[NotNull]
		public static IGeometry FromXmlDocument([NotNull] XmlDocument xmlDocument)
		{
			Assert.ArgumentNotNull(xmlDocument, nameof(xmlDocument));

			return FromXmlString(xmlDocument.OuterXml);
		}

		public static void ToXmlFile([NotNull] IGeometry geometry,
		                             [NotNull] string filePath)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

			ToXmlDocument(geometry).Save(filePath);
		}

		[NotNull]
		public static IGeometry FromXmlFile([NotNull] string filePath)
		{
			Assert.ArgumentNotNullOrEmpty(filePath, nameof(filePath));

			var xmlDocument = new XmlDocument();
			xmlDocument.Load(filePath);

			return FromXmlDocument(xmlDocument);
		}

		[NotNull]
		public static IGeometry FromXmlString([NotNull] string xmlGeometryString)
		{
			Assert.ArgumentNotNullOrEmpty(xmlGeometryString, nameof(xmlGeometryString));

			IXMLSerializer xmlSerializer = new XMLSerializerClass();

			try
			{
				return
					(IGeometry)
					xmlSerializer.LoadFromString(xmlGeometryString, null, null);
			}
			finally
			{
				Marshal.ReleaseComObject(xmlSerializer);
			}
		}

		/// <summary>
		/// Converts a geometry to a WKB byte array. This method is very fast but does not support Z/M values.
		/// For Z/M support use <see cref="WkbGeometryWriter"/>.
		/// </summary>
		/// <param name="geometry">The geometry to convert. It must be simple (simplify with allowReorder==true).
		/// Z or M values are lost (in ArcGIS v. 10.2.2)</param>
		/// <remarks>Result is in OGIS OLE/COM simple features Well Known Binary Format, v1.1, little-endian (NDR).
		/// This means that no Z/M values are supported! The polygon winding order in the resulting
		/// wkb is anti-clockwise.</remarks>
		/// <returns></returns>
		[NotNull]
		public static byte[] ToWkb([NotNull] IGeometry geometry)
		{
			// If the geometry is not simple, this throws an exception.

			var wkbGeometry = (IWkb) geometry;

			int requiredBytes = wkbGeometry.WkbSize;

			var byteBuffer = new byte[requiredBytes];

			wkbGeometry.ExportToWkb(ref requiredBytes, out byteBuffer[0]);

			return byteBuffer;
		}

		/// <summary>
		/// Convertes a byte array to a geometry, using the provided geometry as a template.
		/// </summary>
		/// <param name="wkb"></param>
		/// <param name="templateGeometry"></param>
		/// <remarks>The byte array must be OGIS OLE/COM simple features Well Known Binary Format, v1.1, little-endian (NDR).</remarks>
		/// <returns></returns>
		[NotNull]
		public static IGeometry FromWkb([NotNull] byte[] wkb,
		                                [NotNull] IGeometry templateGeometry)
		{
			Assert.ArgumentNotNull(wkb, nameof(wkb));

			var wkbPoly = (IWkb) GeometryFactory.CreateEmptyGeometry(templateGeometry);

			int requiredBytes = wkb.Length;

			wkbPoly.ImportFromWkb(ref requiredBytes, ref wkb[0]);

			return (IGeometry) wkbPoly;
		}

		/// <summary>
		/// Convertes a byte array to a geometry, using the geometry type.
		/// </summary>
		/// <param name="wkb"></param>
		/// <remarks>The byte array must be OGIS OLE/COM simple features Well Known Binary Format, v1.1, little-endian (NDR).</remarks>
		/// <returns></returns>
		[NotNull]
		public static IGeometry FromWkb([NotNull] byte[] wkb)
		{
			esriGeometryType geometryType;

			var wkbType = (wkbGeometryType) BitConverter.ToInt32(wkb, 1);

			if (wkb.Length == 5 && wkbType == 0)
			{
				// in case the original geometry was empty, ExportToWkb does not store byte order nor geometry type.
				throw new ArgumentException(
					"The provided byte array represents an empty geometry with no geometry type information. Unable to create geometry");
			}

			wkbByteOrder osByteOrder = BitConverter.IsLittleEndian
				                           ? wkbByteOrder.wkbNDR
				                           : wkbByteOrder.wkbXDR;

			var inputByteOrder = (wkbByteOrder) wkb[0];

			Assert.AreEqual(inputByteOrder, osByteOrder,
			                "Byte order of OS does not match byte order of wkb");

			switch (wkbType)
			{
				case wkbGeometryType.wkbPoint:
					geometryType = esriGeometryType.esriGeometryPoint;
					break;
				case wkbGeometryType.wkbMultiLinestring:
				case wkbGeometryType.wkbLinestring:
					geometryType = esriGeometryType.esriGeometryPolyline;
					break;
				case wkbGeometryType.wkbMultiPolygon:
				case wkbGeometryType.wkbPolygon:
					geometryType = esriGeometryType.esriGeometryPolygon;
					break;
				case wkbGeometryType.wkbMultiPoint:
					geometryType = esriGeometryType.esriGeometryMultipoint;
					break;
				case wkbGeometryType.wkbMultiPatch:
					geometryType = esriGeometryType.esriGeometryMultiPatch;
					break;
				case wkbGeometryType.wkbGeometryCollection:
					geometryType = esriGeometryType.esriGeometryBag; // experimental
					break;
				default:
					throw new ArgumentOutOfRangeException(
						string.Format("Unsupported geometry type {0}", wkbType));
			}

			var resultGeometry = (IWkb) GeometryFactory.CreateEmptyGeometry(geometryType);

			int requiredBytes = wkb.Length;

			resultGeometry.ImportFromWkb(ref requiredBytes, ref wkb[0]);

			return (IGeometry) resultGeometry;
		}

		public static byte[] ToEsriShapeBuffer(IGeometry geometry)
		{
			var shp = (IESRIShape2) geometry;
			long size = shp.ESRIShapeSize;

			var buffer = new byte[size];
			var count = (int) size;

			shp.ExportToESRIShape(ref count, out buffer[0]);

			return buffer;
		}

		public static IGeometry FromEsriShapeBuffer(byte[] esriShapeBuffer)
		{
			// NOTE: Z/M-awareness is assigned by ImportFromESRIShape

			esriGeometryType geometryType =
				(esriGeometryType) EsriShapeFormatUtils.GetGeometryType(esriShapeBuffer);

			IGeometry result = GeometryFactory.CreateEmptyGeometry(geometryType);

			var shp = (IESRIShape2) result;

			var count = 0;

			try
			{
				shp.ImportFromESRIShape(ref count, ref esriShapeBuffer[0]);
			}
			catch (Exception e)
			{
				_msg.Debug(
					$"Error creating {geometryType} geometry from " +
					$"{Encoding.Default.GetString(esriShapeBuffer)} using new empty geometry {ToString(result)}",
					e);

				// Improve the error message in case this is called on an MTA thread:
				Assert.AreEqual(ApartmentState.STA, Thread.CurrentThread.GetApartmentState(),
				                "Thread is not STA");
				throw;
			}

			return result;
		}

		[NotNull]
		public static Plane FitPlane([NotNull] IPointCollection4 points)
		{
			Assert.ArgumentNotNull(points, nameof(points));

			var zaware = points as IZAware;

			Assert.True(points is IZAware, "geometry must implement IZAware");
			Assert.True(zaware.ZAware, "geometry must be Z aware");
			Assert.True(zaware.ZSimple, "geometry must have only valid z values (no NaN)");

			int count = points.PointCount;

			var x = new double[count];
			var y = new double[count];
			var z = new double[count];

			var wksArray = new WKSPointZ[count];
			QueryWKSPointZs(points, wksArray);

			for (var i = 0; i < wksArray.Length; i++)
			{
				x[i] = wksArray[i].X;
				y[i] = wksArray[i].Y;
				z[i] = wksArray[i].Z;
			}

			return new Plane(x, y, z);
		}

		[NotNull]
		public static IEnvelope UnionFeatureEnvelopes(
			[NotNull] IEnumerable<IFeature> features)
		{
			Assert.ArgumentNotNull(features, nameof(features));

			IEnvelope result = new EnvelopeClass();
			IEnvelope extentTemplate = new EnvelopeClass();

			ISpatialReference spatialReference = null;

			foreach (IFeature feature in features)
			{
				IGeometry shape = feature.Shape;
				if (shape == null || shape.IsEmpty)
				{
					continue;
				}

				shape.QueryEnvelope(extentTemplate);

				if (spatialReference == null)
				{
					var geoDataset = feature.Class as IGeoDataset;
					spatialReference = geoDataset != null
						                   ? geoDataset.SpatialReference
						                   : shape.SpatialReference;

					result.SpatialReference = spatialReference;
				}
				else
				{
					EnsureSpatialReference(extentTemplate, spatialReference, true);
				}

				result.Union(extentTemplate);
			}

			return result;
		}

		public static bool IsGeometryPartValid(
			[CanBeNull] IGeometry geometryPart,
			[CanBeNull] NotificationCollection notifications = null)
		{
			if (geometryPart == null)
			{
				NotificationUtils.Add(notifications, "Geometry is null");
				_msg.Debug("Geometry is not valid: Geometry is null");
				return false;
			}

			if (geometryPart.IsEmpty)
			{
				NotificationUtils.Add(notifications, "Geometry is empty");
				_msg.Debug("Geometry is not valid: Geometry is empty");
				return false;
			}

			var curve = geometryPart as ICurve;
			if (curve != null && curve.Length <= 0)
			{
				NotificationUtils.Add(notifications, "Geometry is not valid: Length <= 0");
				_msg.Debug("Geometry is not valid: Length <= 0");
				return false;
			}

			// Interior rings have negative Area, so test for zero, not negative!
			var area = geometryPart as IArea;
			if (area != null && Math.Abs(area.Area) < double.Epsilon)
			{
				NotificationUtils.Add(notifications, "Geometry is not valid: Area == 0");
				_msg.Debug("Geometry is not valid: Area == 0");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the number of parts in the geometry.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		public static int GetPartCount([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var geometryCollection = geometry as IGeometryCollection;

			return geometryCollection?.GeometryCount ?? 1;
		}

		/// <summary>
		/// Determines the number of vertices for each part of the specified polycurve.
		/// This method is roughly 3 orders of magnitude faster than getting each geometry from
		/// the geometry collection (at least for large geometries).
		/// </summary>
		/// <param name="polycurve">The input polycurve</param>
		/// <returns></returns>
		public static int[] GetPointCountPerPart([NotNull] IPolycurve polycurve)
		{
			IPointCollection4 pointCollection = (IPointCollection4) polycurve;

			IPoint point = new PointClass();

			IEnumVertex2 enumVertex = (IEnumVertex2) pointCollection.EnumVertices;

			// Get the number of parts, vertex count of last part:
			enumVertex.ResetToEnd();

			enumVertex.QueryPrevious(point, out int partIndex, out int vertexIndex);

			int geometryCount = partIndex + 1;

			int[] pointCountPerPart = new int[geometryCount];

			pointCountPerPart[partIndex] = vertexIndex + 1;

			for (int i = 0; i < geometryCount - 1; i++)
			{
				// Set to index 0 of the next part, move backward to last index of current part:
				enumVertex.SetAt(i + 1, 0);
				enumVertex.Skip(-1);
				enumVertex.QueryPrevious(point, out partIndex, out vertexIndex);

				pointCountPerPart[i] = vertexIndex + 1;
			}

			return pointCountPerPart;
		}

		/// <summary>
		/// Gets the exterior ring count regardless of the state of the geometry. If
		/// necessary, the label point is calculated or it is even simplified to get the ring count.
		/// </summary>
		/// <param name="polygon"></param>
		/// <param name="allowSimplify">Whether a simplify may be performed on the input polygn in order 
		/// to get the ring count.</param>
		/// <returns></returns>
		public static int GetExteriorRingCount([NotNull] IPolygon polygon,
		                                       bool allowSimplify = true)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			try
			{
				return polygon.ExteriorRingCount;
			}
			catch (COMException comException)
			{
				// WORK-AROUND for COMException: The operation cannot be performed on a non-simple geometry.
				//             despite the geometry being reported as simple by topoOp3.get_IsSimpleEx
				//             when accessing ofResultPoly.ExteriorRingCount.
				//             This has been observed when reshaping boundary loops
				//			   or when the reshape line has intermediate touch points (same as in TOP-4484)
				_msg.DebugFormat(
					"Caught semi-expected error when accessing exterior ring count: {0}",
					comException.Message);

				return GetRingCountablePolygon(polygon, allowSimplify).ExteriorRingCount;
				// END WORK-AROUND
			}
		}

		public static int GetPointCount([NotNull] IEnumerable<IGeometry> geometries)
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));

			var result = 0;

			foreach (IGeometry geometry in geometries)
			{
				if (geometry == null)
				{
					continue;
				}

				result += GetPointCount(geometry);
			}

			return result;
		}

		public static int GetPointCount([CanBeNull] IGeometry geometry)
		{
			// NOTE: shape properties may return null (observed for annotation features)
			// -> accept null values to simplify calls based on feature.Shape

			if (geometry == null)
			{
				return 0;
			}

			var points = geometry as IPointCollection;

			if (points == null)
			{
				return geometry.IsEmpty
					       ? 0
					       : 1;
			}

			return points.PointCount;
		}

		public static bool IsSelfIntersecting([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var topoOp = (ITopologicalOperator3) geometry;

			topoOp.IsKnownSimple_2 = false;

			esriNonSimpleReasonEnum nonSimpleReason;
			topoOp.get_IsSimpleEx(out nonSimpleReason);

			return nonSimpleReason ==
			       esriNonSimpleReasonEnum.esriNonSimpleSelfIntersections;
		}

		public static bool IsGeometryValid(IGeometry geometry)
		{
			const bool prohibitMultipart = false;
			return IsGeometryValid(geometry, prohibitMultipart);
		}

		public static bool IsGeometryValid([NotNull] IGeometry geometry,
		                                   bool prohibitMultipart,
		                                   [CanBeNull] NotificationCollection notifications
			                                   = null)
		{
			if (! IsGeometryPartValid(geometry, notifications))
			{
				return false;
			}

			var geometryCollection = geometry as IGeometryCollection;
			if (geometryCollection != null)
			{
				int partCount = geometryCollection.GeometryCount;

				if (prohibitMultipart && partCount > 1)
				{
					NotificationUtils.Add(notifications, "Geometry is multipart");
					_msg.Debug("Geometry is not valid: is multipart");
					return false;
				}

				for (var index = 0; index < partCount; index++)
				{
					IGeometry geometryPart = geometryCollection.Geometry[index];
					if (! IsGeometryPartValid(geometryPart, notifications))
					{
						_msg.DebugFormat(
							"Geometry part {0} is not valid (see previous message).",
							index);
						return false;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Checks if the given geometry has only one part that hold
		/// a positive area.
		/// If not a polygon is given, TRUE is returned
		/// </summary>
		/// <param name="geometry">Geometry to test</param>
		/// <returns>TRUE if polygon geometry has only one positive area part
		/// or the geoemtry is not a polygon, FALSE otherwise</returns>
		public static bool HasOnlyOnePositiveAreaPart([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			if (geometry.GeometryType != esriGeometryType.esriGeometryPolygon &&
			    ! (geometry is IArea))
			{
				return true;
			}

			// TODO use direct method for counting external rings

			var positiveParts = 0;
			var collection = geometry as IGeometryCollection;

			if (collection != null)
			{
				int geometryCount = collection.GeometryCount;

				for (var index = 0; index < geometryCount; index++)
				{
					IGeometry part = collection.Geometry[index];
					if (part is IArea area && area.Area > 0)
					{
						positiveParts++;
					}
				}

				if (positiveParts > 1)
				{
					_msg.DebugFormat(
						"The tested geometry holds {0} positive parts.\n" +
						"Only 1 positive part is allowed", positiveParts);
					return false;
				}

				return true;
			}

			return ((IArea) geometry).Area > 0;
		}

		// NOTE: this is slow for many geometries to subtract
		[NotNull]
		public static IGeometry SubtractGeometries(
			[NotNull] IGeometry geometry,
			[NotNull] IEnumerable<IGeometry> geometriesToSubtract,
			bool adjustZ)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(geometriesToSubtract, nameof(geometriesToSubtract));

			var result = (IGeometry) ((IClone) geometry).Clone();
			var topoOperator = (ITopologicalOperator2) result;

			ICollection<IGeometry> collection =
				CollectionUtils.GetCollection(geometriesToSubtract);

			foreach (IGeometry geometryToSubtract in collection)
			{
				result = topoOperator.Difference(geometryToSubtract);
				topoOperator = (ITopologicalOperator2) result;
			}

			topoOperator.IsKnownSimple_2 = false;
			topoOperator.Simplify();

			if (adjustZ)
			{
				result = ApplyTargetZs(result, collection,
				                       MultiTargetSubMode.Average);
			}

			return result;
		}

		[NotNull]
		public static IGeometry SubtractGeometries([NotNull] IGeometry geometry,
		                                           [NotNull] IEnumerable<IFeature> features,
		                                           bool adjustZ)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(features, nameof(features));

			List<IGeometry> geometries = features.Select(feature => feature.Shape).ToList();

			return SubtractGeometries(geometry, geometries, adjustZ);
		}

		public static bool EndPointsAreEqual([NotNull] IPolyline firstLine,
		                                     [NotNull] IPolyline secondLine)
		{
			var fromPointRelationOperator = (IRelationalOperator) firstLine.FromPoint;
			var toPointRelationOperator = (IRelationalOperator) firstLine.ToPoint;

			return fromPointRelationOperator.Equals(secondLine.FromPoint) &&
			       toPointRelationOperator.Equals(secondLine.ToPoint);
		}

		/// <summary>
		/// Returns a value indicating if two geometries are equal after they have
		/// been rounded.
		/// </summary>
		/// <param name="firstPoint">The first point.</param>
		/// <param name="secondPoint">The second point.</param>
		/// <param name="significantDigits">The significant digits taken into account.
		/// The coordinates are rounded accordingliy before the comparison (without an
		/// other tolerance) made.</param>
		/// <returns></returns>
		public static bool AreEqualInXY([NotNull] IPoint firstPoint,
		                                [NotNull] IPoint secondPoint,
		                                int significantDigits)
		{
			if (Math.Abs(
				    Math.Round(firstPoint.X, significantDigits) -
				    Math.Round(secondPoint.X, significantDigits)) > double.Epsilon)
			{
				return false;
			}

			return Math.Abs(
				       Math.Round(firstPoint.Y, significantDigits) -
				       Math.Round(secondPoint.Y, significantDigits)) < double.Epsilon;
		}

		/// <summary>
		/// Returns a value indicating if two geometries are equal 
		/// in XY and within the XY tolerance.
		/// </summary>
		/// <param name="firstGeometry">The first geometry.</param>
		/// <param name="secondGeometry">The second geometry.</param>
		/// <returns></returns>
		public static bool AreEqualInXY([CanBeNull] IGeometry firstGeometry,
		                                [CanBeNull] IGeometry secondGeometry)
		{
			if (firstGeometry == null && secondGeometry == null)
			{
				return true;
			}

			if (firstGeometry == null || secondGeometry == null)
			{
				return false;
			}

			AllowIndexing(firstGeometry);
			AllowIndexing(secondGeometry);

			bool areEqual = ((IRelationalOperator) firstGeometry).Equals(secondGeometry);

			if (! areEqual)
			{
				return false;
			}

			// *** WORKAROUND for NIM088082 *** 
			// IRelationalOperator.Equals says that geometries are equal. 
			// ... but at 10.1 this may be incorrect, for polylines with equal envelopes
			// polygons are apparently not affected
			if (RuntimeUtils.Is10_1)
			{
				if (firstGeometry.GeometryType == esriGeometryType.esriGeometryPolyline)
				{
					if (firstGeometry.IsEmpty)
					{
						return true; // trust Equals()
					}

					IGeometry difference = null;
					try
					{
						difference = ((ITopologicalOperator) firstGeometry)
							.SymmetricDifference(secondGeometry);

						// return true only if the difference is really empty
						return difference == null || difference.IsEmpty;
					}
					catch (Exception e)
					{
						_msg.WarnFormat("Error calculating polyline difference: {0} " +
						                "(see log for involved geometries)", e.Message);

						_msg.Debug(ToString(firstGeometry));
						_msg.Debug(ToString(secondGeometry));

						return true; // trust Equals() (?)
					}
					finally
					{
						if (difference != null)
						{
							Marshal.ReleaseComObject(difference);
						}
					}
				}
			}

			return true; // trust Equals() for all other cases
		}

		/// <summary>
		/// Replaces the geometry part at the specified index in the specified geometry with the specified replacement.
		/// </summary>
		/// <param name="inGeometry"></param>
		/// <param name="atIndex"></param>
		/// <param name="replacementPart"></param>
		public static void ReplaceGeometryPart([NotNull] IGeometry inGeometry, int atIndex,
		                                       [NotNull] IGeometry replacementPart)
		{
			var geometryCollection = (IGeometryCollection) inGeometry;

			geometryCollection.InsertGeometries(atIndex, 1, ref replacementPart);

			geometryCollection.RemoveGeometries(atIndex + 1, 1);
		}

		/// <summary>
		/// Replaces the geometry part at the specified index in the specified geometry with the specified replacement.
		/// </summary>
		/// <param name="inGeometry"></param>
		/// <param name="atIndex"></param>
		/// <param name="replacementParts"></param>
		public static void ReplaceGeometryPart(
			[NotNull] IGeometry inGeometry,
			int atIndex,
			[NotNull] IGeometryCollection replacementParts)
		{
			var geometryCollection = (IGeometryCollection) inGeometry;

			int addedParts = replacementParts.GeometryCount;

			geometryCollection.InsertGeometryCollection(atIndex, replacementParts);

			geometryCollection.RemoveGeometries(atIndex + addedParts, 1);
		}

		[NotNull]
		public static IGeometry GetCoincidentPath(
			[NotNull] IGeometryCollection inCongruentGeometry, [NotNull] IPath searchPath,
			double xyTolerance)
		{
			AllowIndexing((IGeometry) inCongruentGeometry);

			IGeometry foundPart = GetHitGeometryPart(searchPath.FromPoint,
			                                         (IGeometry) inCongruentGeometry,
			                                         xyTolerance);

			Assert.NotNull(foundPart, "GetCoincidentPath: No coincident path found.");

			// safety net: if there are touching rings, they often touch in the From-/To-Points
			// TODO: If (non-simple) rings touch along several vertices, this can still result in wrong results 
			//       Consider using GeometryComparison.HaveSameVertices: safer but slower

			// get second opinion (could be optimized by only checking if length is different -> unsafe!
			const int testPointIdx = 1;
			foundPart = GetPartBySecondOpinionHitTest(inCongruentGeometry, searchPath,
			                                          testPointIdx, foundPart, xyTolerance);
			return foundPart;
		}

		[NotNull]
		public static IPointCollection GetIntersectPoints(
			[NotNull] ITopologicalOperator lineTopoOp,
			[NotNull] IGeometry lineOfInterest)
		{
			return
				(IPointCollection) IntersectionUtils.GetIntersectionPoints((IGeometry) lineTopoOp,
					lineOfInterest);
		}

		/// <summary>
		/// Returns a value indicating if two geometries are exactly equal, i.e.
		/// all properties are identical (exact numbers compared, no tolerances applied).
		/// </summary>
		/// <param name="firstGeometry">The first geometry.</param>
		/// <param name="secondGeometry">The second geometry.</param>
		/// <returns></returns>
		public static bool AreEqual([CanBeNull] IGeometry firstGeometry,
		                            [CanBeNull] IGeometry secondGeometry)
		{
			if (ReferenceEquals(firstGeometry, secondGeometry))
			{
				// same instance
				return true;
			}

			if (firstGeometry == null || secondGeometry == null)
			{
				// one is null, the other isn't
				return false;
			}

			return ((IClone) firstGeometry).IsEqual((IClone) secondGeometry);
		}

		public static void Simplify([NotNull] IGeometry geometry)
		{
			const bool allowReorder = false;
			Simplify(geometry, allowReorder);
		}

		public static void Simplify([NotNull] IGeometry geometry, bool allowReorder)
		{
			const bool allowPathSplitAtIntersections = false;
			Simplify(geometry, allowReorder, allowPathSplitAtIntersections);
		}

		public static void Simplify([NotNull] IGeometry geometry,
		                            bool allowReorder,
		                            bool allowPathSplitAtIntersections)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var topoOp2 = geometry as ITopologicalOperator2;
			if (topoOp2 != null)
			{
				topoOp2.IsKnownSimple_2 = false;
			}

			esriGeometryType geometryType = geometry.GeometryType;

			if (! allowPathSplitAtIntersections &&
			    geometryType == esriGeometryType.esriGeometryPolyline)
			{
				// don't split self-intersecting lines in multiple parts
				SimplifyNetworkGeometry(geometry);

				return;
			}

			if (! allowReorder && geometryType == esriGeometryType.esriGeometryPolygon)
			{
				// reorder of polygon vertices not allowed, special handling for polygons required

				var polygon = (IPolygon) geometry;

				try
				{
					polygon.SimplifyPreserveFromTo();
				}
				catch (Exception)
				{
					_msg.DebugFormat("Exception in SimplifyPreserveFromTo() on geometry {0}",
					                 ToString(polygon));
					throw;
				}

				// in special situations involving circular arcs, the result may be non-empty but 
				// contain NO exterior ring (just interior rings). In the known case, the correct 
				// result would have been to empty the polygon. Do that explicitly.
				// see https://issuetracker02.eggits.net/browse/PSM-401
				if (! polygon.IsEmpty && polygon.ExteriorRingCount == 0)
				{
					polygon.SetEmpty();
				}

				return;
			}

			var points = geometry as IPointCollection;

			try
			{
				if (points != null && points.PointCount > 0)
				{
					points.QueryPoint(0, SimplifyPointTemplate);
				}

				((ITopologicalOperator) geometry).Simplify();

				if (geometryType == esriGeometryType.esriGeometryPolygon && ! geometry.IsEmpty)
				{
					// in special situations involving circular arcs, the result may be non-empty but 
					// contain NO exterior ring (just interior rings). In the known case, the correct 
					// result would have been to empty the polygon. Do that explicitly.
					// see https://issuetracker02.eggits.net/browse/PSM-401
					var polygon = (IPolygon) geometry;
					if (polygon.ExteriorRingCount == 0)
					{
						polygon.SetEmpty();
					}
				}
			}
			catch (COMException ex)
			{
				// this does not work with esriGeometryError - use integer value?: -2147220930
				//if (comException.ErrorCode == (int)esriGeometryError.E_GEOMETRY_UNKNOWNERROR)
				//{
				//// WORKAROUND needed due to new bug in ArcGIS 10.0 (SP2) (may be fixed in later SPs?)
				//// To avoid System.Runtime.InteropServices.COMException (0x8004023E): An unknown error has occurred in the geometry system.
				//// at ESRI.ArcGIS.Geometry.ITopologicalOperator.Simplify():

				_msg.DebugFormat(
					"Handling exception '{0}' ({1}) in ITopologicalOperator.Simplify()",
					ex.Message, ex.ErrorCode);

				if (points != null && points.PointCount > 0)
				{
					points.QueryPoint(0, SimplifyPointTemplate);
				}

				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugFormat("Second try. Simplifying {0}", ToString(geometry));
				}

				((ITopologicalOperator) geometry).Simplify();

				//// END WORKAROUND;
				//}
				//else
				//{
				//    throw;
				//}
			}
		}

		public static void SimplifyNetworkGeometry([NotNull] IGeometry geometry)
		{
			var polyline = geometry as IPolyline;

			Assert.NotNull(polyline, "The geometry is not a polyline");

			((ITopologicalOperator2) polyline).IsKnownSimple_2 = false;

			try
			{
				polyline.SimplifyNetwork();
			}
			catch (COMException e)
			{
				// NOTE: For some geometries SimplifyNetwork results in coordinate or measures out of bound exception
				//		 in this case calling ITopologicalOperator.IsSimple on the geometry resolves the issue (observed with IsSimple == true)
				//		 Just getting the FromPoint / ToPoint does not resolve it.
				//		 This happens for example after calling SplitAtPoints when the points originate from a different SR
				//		 Affected are CrackerTool, ReshapeAlong, and most likely others as well
				_msg.Debug(
					"Exception in SimplifyNetwork. Trying again after calling IsSimple...", e);

				try
				{
					var topologicalOperator = (ITopologicalOperator) polyline;
					_msg.DebugFormat("Polyline is simple: {0}", topologicalOperator.IsSimple);

					polyline.SimplifyNetwork();
				}
				catch (COMException)
				{
					_msg.DebugFormat("Error in SimplifyNetwork (2. try) for geometry {0}",
					                 ToString(polyline));
					throw;
				}

				_msg.DebugFormat("2. call to SimplifyNetwork() succeeded.");

				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.VerboseDebug(
						() =>
							$"Geometry that needed 2 SimplifyNetwork calls: {ToString(polyline)}");
				}
			}
		}

		public static void SimplifyZ(IGeometry geometry)
		{
			const double defaultZ = 0d;

			SimplifyZ(geometry, defaultZ);
		}

		/// <summary>
		/// Ensures that the geometry has no NaN Zs if it is Z aware
		/// </summary>
		/// <param name="geometry">The geometry</param>
		/// <param name="defaultZ">The default Z value to assign if not all points can be interpolated</param>
		public static void SimplifyZ(IGeometry geometry, double defaultZ)
		{
			bool isSimple = TrySimplifyZ(geometry);

			if (isSimple)
			{
				return;
			}

			if (! double.IsNaN(defaultZ))
			{
				// if it still contains NaN values set all NULL values to defaultZ
				_msg.VerboseDebug(() => $"Setting all Z values to {defaultZ}");
				ApplyConstantZ(geometry, defaultZ);
			}
		}

		/// <summary>
		/// Tries to calculate missing Z values, if there are any.
		/// </summary>
		/// <param name="geometry">A polycurve geometry. Other geometries can be tested but not simplified.</param>
		/// <returns>Whether the geometry was made Z-simple or already was Z-simple to begin with.</returns>
		public static bool TrySimplifyZ([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry);

			if (! HasUndefinedZValues(geometry))
			{
				_msg.VerboseDebug(() => "Geometry is considered z simple");
				return true;
			}

			var geometryZ = geometry as IZ;

			if (geometryZ == null)
			{
				// non-polycurves do not implement IZ
				return false;
			}

			// NOTE: CalculateNonSimpleZs on a geometry (part) with no Zs 
			//       - does nothing for polylines 
			//       - fails for polygons with COMException (0x8004025E): A polygon part has no defined zs
			var geometryCollection = (IGeometryCollection) geometry;

			bool canSimplify = GetParts(geometryCollection).All(HasAnyZValues);

			if (! canSimplify)
			{
				_msg.VerboseDebug(() => "Non-simple Zs cannot be calculated (too few Z values)");
				return false;
			}

			_msg.VerboseDebug(() => "Calculating non-simple Zs");

			// try interpolation if it's a line or poly and already has some Zs
			geometryZ.CalculateNonSimpleZs();

			// NOTE: Polylines with a single Z value are not processed by CalculateNonSimpleZs. Consider using
			//       the existing Z value to extrapolate

			return ! HasUndefinedZValues(geometry);
		}

		/// <summary>
		/// Determines whether a geometry intersects with another geometry. Allows to control the
		/// use of spatial indexes on the geometries.
		/// </summary>
		/// <param name="geom1">The first geometry (containing).</param>
		/// <param name="geom2">The second geometry (contained).</param>
		/// <param name="suppressIndexing">If <c>true</c>, no spatial index is created on any of 
		/// the geometries. Otherwise, spatial indexes are build for both geometries if not already
		/// existing.</param>
		/// <returns>
		/// 	<c>true</c> if the first geometry intersects with the second geometry; otherwise, <c>false</c>.
		/// </returns>
		public static bool Intersects([NotNull] IGeometry geom1,
		                              [NotNull] IGeometry geom2,
		                              bool suppressIndexing = false)
		{
			return ! Disjoint(geom1, geom2, suppressIndexing);
		}

		/// <summary>
		/// Determines whether two geometries are disjoint. Allows to control the
		/// use of spatial indexes on the geometries.
		/// </summary>
		/// <param name="geom1">The first geometry.</param>
		/// <param name="geom2">The second geometry.</param>
		/// <param name="suppressIndexing">If <c>true</c>, no spatial index is created on any of 
		/// the geometries. Otherwise, spatial indexes are build for both geometries if not already
		/// existing.</param>
		/// <returns>
		/// 	<c>true</c> if the geometries are disjoint; otherwise, <c>false</c>.
		/// </returns>
		public static bool Disjoint([NotNull] IGeometry geom1,
		                            [NotNull] IGeometry geom2,
		                            bool suppressIndexing = false)
		{
			if (! suppressIndexing)
			{
				AllowIndexing(geom1);
				AllowIndexing(geom2);
			}

			bool result;

			try
			{
				result = ((IRelationalOperator) geom1).Disjoint(geom2);

				if (! result)
				{
					// WORK-AROUND:
					// Bug in 10.0 (NIM074889): if point is in envelope of multipoint -> false even if disjoint
					if (geom1.GeometryType == esriGeometryType.esriGeometryMultipoint &&
					    geom2.GeometryType == esriGeometryType.esriGeometryPoint)
					{
						result = ! Contains(geom1, geom2);
					}
					else if (geom2.GeometryType == esriGeometryType.esriGeometryMultipoint &&
					         geom1.GeometryType == esriGeometryType.esriGeometryPoint)
					{
						result = ! Contains(geom2, geom1);
					}

					// END WORK-AROUND
				}
			}
			catch (COMException e)
			{
				_msg.Debug("COM Exception in Disjoint.", e);

				_msg.DebugFormat("Geometry1: {0}{1}Geometry2: {2}",
				                 ToString(geom1), Environment.NewLine, ToString(geom2));

				// The most frequent error - enum code is 533
				if (e.ErrorCode == _esriGeometryErrorInconsistentSpatialRel)
				{
					throw new InvalidOperationException(
						"Disjoint: Spatial References of provided geometries are not consistent.",
						e);
				}

				throw;
			}

			return result;
		}

		/// <summary>
		/// Determines whether a high-level geometry and an envelope are disjoint.
		/// This is a cheap (especially for low-level geometries) but dangerous optimization.
		/// Dangerous because an envelope that touches a polyline is considered disjoint by
		/// ArcObjects. This method additionally tests whether the envelope touches the geometry.
		/// </summary>
		/// <param name="highLevelGeometry"></param>
		/// <param name="envelope"></param>
		/// <returns></returns>
		public static bool Disjoint([NotNull] IGeometry highLevelGeometry,
		                            [NotNull] IEnvelope envelope)
		{
			// NOTE: Disjoint on envelopes returns true even though it touches a line (regardles of AllowIndexing)
			// Scenario: highLevelGeometry is a polyline that touches corners of the envelope.
			// TODO: Test in ArcGIS 10, send bug report,
			// TODO: Test if this also happens with other geometry types than lines and adapt accordingly

			// alternative: clone envelope, expand by tolerance

			bool disjoint;

			if (Disjoint(highLevelGeometry, (IGeometry) envelope))
			{
				if (((IRelationalOperator) envelope).Touches(highLevelGeometry))
				{
					disjoint = false;
				}
				else
				{
					disjoint = true;
				}
			}
			else
			{
				disjoint = false;
			}

			return disjoint;
		}

		public static bool Disjoint([NotNull] IEnvelope envelope1,
		                            [NotNull] IEnvelope envelope2,
		                            double tolerance)
		{
			// This is a significantly faster than IRelationalOperator.Disjoint
			if (envelope1.XMax + tolerance < envelope2.XMin)
			{
				return true;
			}

			if (envelope1.XMin - tolerance > envelope2.XMax)
			{
				return true;
			}

			if (envelope1.YMax + tolerance < envelope2.YMin)
			{
				return true;
			}

			if (envelope1.YMin - tolerance > envelope2.YMax)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether a geometry contains another geometry. Allows to control the
		/// use of spatial indexes on the geometries.
		/// </summary>
		/// <param name="geom1">The first geometry (containing).</param>
		/// <param name="geom2">The second geometry (contained).</param>
		/// <param name="suppressIndexing">If <c>true</c>, no spatial index is created on any of 
		/// the geometries. Otherwise, spatial indexes are build for both geometries if not already
		/// existing.</param>
		/// <returns>
		/// 	<c>true</c> if the first geometry contains the second geometry; otherwise, <c>false</c>.
		/// </returns>
		public static bool Contains([NotNull] IGeometry geom1,
		                            [NotNull] IGeometry geom2,
		                            bool suppressIndexing = false)
		{
			if (! suppressIndexing)
			{
				AllowIndexing(geom1);
				AllowIndexing(geom2);
			}

			bool result;

			try
			{
				result = ((IRelationalOperator) geom1).Contains(geom2);
			}
			catch (COMException e)
			{
				_msg.Debug("COM Exception in Contains.", e);

				_msg.DebugFormat("Geometry1: {0}{1}Geometry2: {2}",
				                 ToString(geom1), Environment.NewLine, ToString(geom2));

				// The most frequent error - enum code is 533
				if (e.ErrorCode == _esriGeometryErrorInconsistentSpatialRel)
				{
					throw new InvalidOperationException(
						"Contains: Spatial References of provided geometries are not consistent.",
						e);
				}

				throw;
			}

			return result;
		}

		/// <summary>
		/// Determines whether two geometries touch each other. Allows control over the
		/// use of spatial indexes on the geometries.
		/// </summary>
		/// <param name="geom1">The first geometry</param>
		/// <param name="geom2">The second geometry</param>
		/// <param name="suppressIndexing">If <c>true</c>, no spatial index is created on any of 
		/// the geometries. Otherwise, spatial indexes are build for both geometries if not already
		/// existing.</param>
		/// <c>true</c> if the geometries touch; otherwise, <c>false</c>.
		public static bool Touches([NotNull] IGeometry geom1,
		                           [NotNull] IGeometry geom2,
		                           bool suppressIndexing = false)
		{
			if (! suppressIndexing)
			{
				AllowIndexing(geom1);
				AllowIndexing(geom2);
			}

			bool result;

			try
			{
				result = ((IRelationalOperator) geom1).Touches(geom2);

				// BUG: if the first part of a multipart polyline touches a polygon, but the second part intersects: touches returns true which is wrong
				if (result &&
				    geom1 is IPolycurve && geom2 is IPolycurve &&
				    (geom1.Dimension == esriGeometryDimension.esriGeometry1Dimension ||
				     geom2.Dimension == esriGeometryDimension.esriGeometry1Dimension))
				{
					// by definition the interior intersection must be empty:
					// NOTE: this fails in the unit test
					//var matrix = new IntersectionMatrix("T********");
					//IList<IGeometry> interiorIntersections = matrix.GetIntersections(geom1, geom2);

					var geom1Collection = (IGeometryCollection) geom1;
					var geom2Collection = (IGeometryCollection) geom2;

					bool wrongResult =
						(geom1Collection.GeometryCount > 1 &&
						 geom1.Dimension == esriGeometryDimension.esriGeometry1Dimension &&
						 AnyPartInteriorIntersects(geom1Collection, geom2)) ||
						(geom2Collection.GeometryCount > 1 &&
						 geom2.Dimension == esriGeometryDimension.esriGeometry1Dimension &&
						 AnyPartInteriorIntersects(geom2Collection, geom1));

					if (wrongResult)
					{
						_msg.DebugFormat(
							"IRelationalOperator.Touches returned true which was corrected to false. Geometry1: {0}{1}Geometry2:{2}",
							ToString(geom1), Environment.NewLine, ToString(geom2));

						result = false;
					}
				}
			}
			catch (COMException e)
			{
				_msg.Debug("COM Exception in Touches.", e);

				_msg.DebugFormat("Geometry1: {0}{1}Geometry2: {2}",
				                 ToString(geom1), Environment.NewLine, ToString(geom2));

				// The enum code is 533, see http://resources.esri.com/help/9.3/arcgisengine/dotnet/a3bd05c8-64a6-4dd4-acb3-0d10b021f2f8.htm#ConvertTen
				if (e.ErrorCode == _esriGeometryErrorInconsistentSpatialRel)
				{
					throw new InvalidOperationException(
						"Touches: Spatial References of provided geometries are not consistent.",
						e);
				}

				throw;
			}

			return result;
		}

		/// <summary>
		/// Determines whether two geometries overlap. Allows to control the
		/// use of spatial indexes on the geometries.
		/// </summary>
		/// <param name="geometry1">The first geometry.</param>
		/// <param name="geometry2">The second geometry.</param>
		/// <param name="suppressIndexing">If <c>true</c>, no spatial index is created on any of 
		/// the geometries. Otherwise, spatial indexes are build for both geometries if not already
		/// existing.</param>
		/// <returns>
		/// 	<c>true</c> if the first geometry contains the second geometry; otherwise, <c>false</c>.
		/// </returns>
		public static bool Overlaps([NotNull] IGeometry geometry1,
		                            [NotNull] IGeometry geometry2,
		                            bool suppressIndexing = false)
		{
			if (! suppressIndexing)
			{
				AllowIndexing(geometry1);
				AllowIndexing(geometry2);
			}

			bool result;

			try
			{
				result = ((IRelationalOperator) geometry1).Overlaps(geometry2);
			}
			catch (COMException e)
			{
				_msg.Debug("COM Exception in Contains.", e);

				_msg.DebugFormat("Geometry1: {0}{1}Geometry2: {2}",
				                 ToString(geometry1), Environment.NewLine, ToString(geometry2));

				// The most frequent error - enum code is 533
				if (e.ErrorCode == _esriGeometryErrorInconsistentSpatialRel)
				{
					throw new InvalidOperationException(
						"Contains: Spatial References of provided geometries are not consistent.",
						e);
				}

				throw;
			}

			return result;
		}

		/// <summary>
		/// Determines whether two geometries cross. Allows to control the
		/// use of spatial indexes on the geometries.
		/// </summary>
		/// <param name="geometry1">The first geometry.</param>
		/// <param name="geometry2">The second geometry.</param>
		/// <param name="suppressIndexing">If <c>true</c>, no spatial index is created on any of 
		/// the geometries. Otherwise, spatial indexes are build for both geometries if not already
		/// existing.</param>
		/// <returns>
		/// 	<c>true</c> if the first geometry crosses the second geometry; otherwise, <c>false</c>.
		/// </returns>
		public static bool Crosses([NotNull] IGeometry geometry1,
		                           [NotNull] IGeometry geometry2,
		                           bool suppressIndexing = false)
		{
			if (! suppressIndexing)
			{
				AllowIndexing(geometry1);
				AllowIndexing(geometry2);
			}

			bool result;

			try
			{
				result = ((IRelationalOperator) geometry1).Crosses(geometry2);
			}
			catch (COMException e)
			{
				_msg.Debug("COM Exception in Contains.", e);

				_msg.DebugFormat("Geometry1: {0}{1}Geometry2: {2}",
				                 ToString(geometry1), Environment.NewLine, ToString(geometry2));

				// The most frequent error - enum code is 533
				if (e.ErrorCode == _esriGeometryErrorInconsistentSpatialRel)
				{
					throw new InvalidOperationException(
						"Contains: Spatial References of provided geometries are not consistent.",
						e);
				}

				throw;
			}

			return result;
		}

		private static bool AnyPartInteriorIntersects(
			IGeometryCollection geometryCollection, IGeometry geometry)
		{
			var result = false;

			foreach (IGeometry part in GetParts(geometryCollection))
			{
				IGeometry highLevelPart = GetHighLevelGeometry(part, true);

				if (InteriorIntersects(highLevelPart, geometry))
				{
					result = true;
				}
			}

			return result;
		}

		/// <summary>
		/// Determines whether the interior of two geometries intersect. Allows control over the
		/// use of spatial indexes on the geometries.
		/// </summary>
		/// <param name="geom1">The first geometry</param>
		/// <param name="geom2">The second geometry</param>
		/// <param name="suppressIndexing">If <c>true</c>, no spatial index is created on any of 
		/// the geometries. Otherwise, spatial indexes are build for both geometries if not already
		/// existing.</param>
		/// <c>true</c> if the interiors intersect; otherwise, <c>false</c>.
		public static bool InteriorIntersects([NotNull] IGeometry geom1,
		                                      [NotNull] IGeometry geom2,
		                                      bool suppressIndexing = false)
		{
			// TODO: "G1.INTERIOR INTERSECTS G2.INTERIOR" is faster but does it actually work correctly?
			//		 NOTE: "T*********" fails in some (touch) situations, especially if geometries are not simple
			// TODO: consider catching exception and falling back to Touches
			return ! Disjoint(geom1, geom2, suppressIndexing) &&
			       ! Touches(geom1, geom2, suppressIndexing);
		}

		public static IGeometry Buffer([NotNull] IGeometry geometry, double bufferRadius,
		                               bool suppressIndexing = false)
		{
			Assert.ArgumentNotNaN(bufferRadius, nameof(bufferRadius));

			ITopologicalOperator geoTopoOp = GetTopoOperator(geometry);

			IGeometry buffer = geoTopoOp.Buffer(bufferRadius);

			if (! suppressIndexing)
			{
				AllowIndexing(buffer);
			}

			return buffer;
		}

		public static IGeometry GeneralizedBuffer([NotNull] IGeometry geo,
		                                          double bufferRadius,
		                                          bool suppressIndexing = false,
		                                          bool simplify = false)
		{
			Assert.ArgumentNotNaN(bufferRadius, nameof(bufferRadius));

			// TODO REPLACE 
			IGeometry source;

			if (geo is IPolycurve)
			{
				// patch for buffer error 
				source = GeometryFactory.Clone(geo);
				const double maxAllowableOffset = 0.1;
				((IPolycurve) source).Generalize(maxAllowableOffset);
			}
			else
			{
				source = geo;
			}

			ITopologicalOperator geoTopoOp = GetTopoOperator(source);

			if (simplify && geo is ITopologicalOperator2)
			{
				var topoOp2 = (ITopologicalOperator2) source;
				topoOp2.IsKnownSimple_2 = false;
				topoOp2.Simplify();
				geoTopoOp = topoOp2;
			}

			IGeometry buffer = geoTopoOp.Buffer(bufferRadius);

			if (! suppressIndexing)
			{
				AllowIndexing(buffer);
			}

			return buffer;
		}

		[CanBeNull]
		public static IPolycurve ConstructOffset([NotNull] IPolycurve polycurve,
		                                         double distance, out string message)
		{
			// NOTE: if adding esriConstructOffsetSimple, specific distance values fail and 
			//		 larger distances seem to create very random output
			const esriConstructOffsetEnum constructionMethod =
				esriConstructOffsetEnum.esriConstructOffsetMitered;

			return ConstructOffset(polycurve, distance, constructionMethod, false, out message);
		}

		/// <summary>
		/// Calculates (potentially non-simple) offset geometry. As construct offset is not very
		/// reliable, especially with non-small distances the return value could be null.
		/// </summary>
		/// <param name="polycurve">The input polycurve</param>
		/// <param name="distance">The offset distance</param>
		/// <param name="constructionMethod"></param>
		/// <param name="avoidSelfIntersectingResult">Whether a self-intersecting result should be avoided and null returned instead.</param>
		/// <param name="message">The message why no offset could be calculated. Typically because the distance is too large or, when
		/// including the simple esriConstructOffsetEnum, the specific tolerance results in errors.</param>
		/// <returns></returns>
		[CanBeNull]
		public static IPolycurve ConstructOffset([NotNull] IPolycurve polycurve,
		                                         double distance,
		                                         esriConstructOffsetEnum constructionMethod,
		                                         bool avoidSelfIntersectingResult,
		                                         out string message)
		{
			Assert.ArgumentNotNull(polycurve, nameof(polycurve));

			message = null;
			object missing = Type.Missing;
			object constructionMethodObj = constructionMethod;

			IConstructCurve constructCurve = null;

			if (polycurve is IPolyline)
			{
				constructCurve = new PolylineClass();
			}
			else if (polycurve is IPolygon)
			{
				constructCurve = new PolygonClass();
			}

			Assert.NotNull(constructCurve, "Unsupported polycurve");

			((IGeometry) constructCurve).SpatialReference = polycurve.SpatialReference;

			try
			{
				constructCurve.ConstructOffset(polycurve, distance, ref constructionMethodObj,
				                               ref missing);
			}
			catch (COMException e)
			{
				_msg.Debug("Error constructing offset", e);

				if (e.ErrorCode == _esriGeometryErrorUnknownError)
				{
					message =
						string.Format(
							"Unable to construct offset with the given tolerance ({0}). A different tolerance might work.",
							distance);
					return null;
				}
			}

			bool selfIntersecting = IsSelfIntersecting((IGeometry) constructCurve);

			if (selfIntersecting)
			{
				message =
					"The offset-geometry is self-intersecting. The distance is probably too large.";

				if (avoidSelfIntersectingResult)
				{
					return null;
				}
			}

			return constructCurve as IPolycurve;
		}

		/// <summary>
		/// Creates a polygon consisting of offset segments of the input that fulfills the buffer-like
		/// criterion, i.e. whose outline is at least at the specified distance or more from the input.
		/// The difference to the standard buffer is that it has flat ('pointy') corners.
		/// </summary>
		/// <param name="polygon">The polygon to buffer.</param>
		/// <param name="bufferDistance">The buffer distance, as opposed to offset, is using the same 
		/// behaviour as buffer, i.e. positive is to the outside, negative to the inside.</param>
		/// <returns>The 'buffer-style' offset polygon (simple) or null if the result is empty or no offset could be calculated.</returns>
		[CanBeNull]
		public static IPolygon ConstructOffsetBuffer([NotNull] IPolygon polygon,
		                                             double bufferDistance)
		{
			double offsetDistance = bufferDistance * -1;

			var offset = (IPolygon) ConstructOffset(polygon, offsetDistance, out string _);

			if (offset == null)
			{
				return null;
			}

			// so far there are still self-intersections...
			Simplify(offset);

			if (offset.IsEmpty)
			{
				_msg.DebugFormat("Unable to construct offset buffer: generalized result is empty.");
				return null;
			}

			// Alternative: Buffer with curves and replace the curves after the intersection / union
			// to avoid all non-offset type segments (like bevelled-off ends) using 
			// SegmentReplacementUtils.ReplaceSegmentsWithProlongedAdjacentSegments.
			// The problem could be that there seems no guarantee that curves are actually created.

			var bufferFactory = new BufferFactory(false, false);

			IList<IPolygon> buffers = bufferFactory.Buffer(polygon, bufferDistance);

			if (buffers.Count == 0)
			{
				_msg.DebugFormat("Unable to construct offset buffer: buffer polygon count is 0.");

				return null;
			}

			Assert.AreEqual(1, buffers.Count, "Unexpected buffer geometry count.");
			IPolygon buffer = buffers[0];

			IPolygon result;
			if (bufferDistance < 0)
			{
				result = (IPolygon) IntersectionUtils.GetIntersection(buffer, offset);
			}
			else
			{
				result = (IPolygon) Union(buffer, offset);
			}

			// TODO: marshal release buffer and offset

			Generalize(result, GetXyTolerance(polygon));

			return result;
		}

		/// <summary>
		/// Removes the specified segments from the segment collection. The ArcObjects functionality
		/// is used to close the gaps if required by the closeGap parameter.
		/// NOTE: If closeGap is false, make sure to call Simplify() after removing the segments, 
		///       otherwise the adjacent segments are still connected.
		/// </summary>
		/// <param name="segmentCollection">The segment collection</param>
		/// <param name="segmentsToRemove">The segments to remove</param>
		/// <param name="closeGap">Whether the created gaps should be closed or not</param>
		public static void RemoveSegments([NotNull] ISegmentCollection segmentCollection,
		                                  [NotNull] IList<int> segmentsToRemove,
		                                  bool closeGap)
		{
			Assert.ArgumentNotNull(segmentCollection, nameof(segmentCollection));
			Assert.ArgumentNotNull(segmentsToRemove, nameof(segmentsToRemove));

			var sortedSegmentsToRemove = new List<int>(segmentsToRemove);
			sortedSegmentsToRemove.Sort();

			var removedSegmentCount = 0;

			foreach (int indexToRemove in sortedSegmentsToRemove)
			{
				_msg.DebugFormat("Removing segment <index> {0} from segment collection.",
				                 indexToRemove);

				segmentCollection.RemoveSegments(
					indexToRemove - removedSegmentCount, 1, closeGap);

				removedSegmentCount++;
			}
		}

		/// <summary>
		/// Removes any vertical segments that may be present in a polyline or polygon.
		/// </summary>
		/// <param name="polycurve">The polycurve.</param>
		/// <returns>The number of removed vertical segments</returns>
		public static int RemoveVerticalSegments([NotNull] IPolycurve polycurve)
		{
			Assert.ArgumentNotNull(polycurve, nameof(polycurve));

			if (! ((IZAware) polycurve).ZAware)
			{
				return 0;
			}

			if (! ((IZ) polycurve).ZVertical)
			{
				return 0;
			}

			// there is at least one vertical segment
			var parts = (IGeometryCollection) polycurve;

			var removedCount = 0;

			foreach (IGeometry part in GetParts(parts))
			{
				var verticalSegmentIndexes = new List<int>();
				var segments = (ISegmentCollection) part;

				var index = 0;
				const bool allowRecycling = true;

				foreach (ISegment segment in GetSegments(segments.EnumSegments, allowRecycling))
				{
					if (Math.Abs(segment.Length) < double.Epsilon)
					{
						var segmentZ = (ISegmentZ) segment;

						double fromZ;
						double toZ;
						segmentZ.GetZs(out fromZ, out toZ);

						if (Math.Abs(fromZ - toZ) > double.Epsilon)
						{
							verticalSegmentIndexes.Add(index);
						}
					}

					index++;
				}

				if (verticalSegmentIndexes.Count <= 0)
				{
					continue;
				}

				verticalSegmentIndexes.Sort();
				verticalSegmentIndexes.Reverse(); // --> sorted descending

				foreach (int indexToRemove in verticalSegmentIndexes)
				{
					const int count = 1;
					const bool closeGap = true;
					segments.RemoveSegments(indexToRemove, count, closeGap);

					removedCount++;
				}

				segments.SegmentsChanged();
			}

			if (removedCount > 0)
			{
				// important: this must be called on the segment collection of 
				// the polycurve (not only the ring/path), otherwise IZ.ZVertical remains true

				((ISegmentCollection) polycurve).SegmentsChanged();
			}

			return removedCount;
		}

		/// <summary>
		/// Removes the parts from the provided geometry according to the provided index-list.
		/// </summary>
		/// <param name="geometryCollection"></param>
		/// <param name="partsToRemove"></param>
		public static void RemoveParts([NotNull] IGeometryCollection geometryCollection,
		                               [NotNull] List<int> partsToRemove)
		{
			Assert.ArgumentNotNull(geometryCollection, nameof(geometryCollection));
			Assert.ArgumentNotNull(partsToRemove, nameof(partsToRemove));

			partsToRemove.Sort();

			var removedPartCount = 0;

			foreach (int indexToRemove in partsToRemove)
			{
				_msg.DebugFormat("Removing ring <index> {0} from geometry.",
				                 indexToRemove);

				geometryCollection.RemoveGeometries(
					indexToRemove - removedPartCount, 1);

				removedPartCount++;
			}
		}

		public static void RemovePoints([NotNull] IPointCollection fromPointCollection,
		                                [NotNull] Predicate<IPoint> predicate)
		{
			for (var i = 0; i < fromPointCollection.PointCount; i++)
			{
				bool remove = predicate(fromPointCollection.Point[i]);

				if (remove)
				{
					fromPointCollection.RemovePoints(i, 1);
					i--;
				}
			}
		}

		/// <summary>
		/// Extends the curve tangentially by the specified distance.
		/// </summary>
		/// <param name="curve"></param>
		/// <param name="extendAt">Must be esriExtendTangentAtFrom, esriExtendTangentAtTo or esriExtendTangents.</param>
		/// <param name="distance">The distance by which the line should be extended. In case of 
		/// esriExtendTangents both ends are extended by the specified distance.</param>
		public static void ExtendCurve([NotNull] ICurve curve,
		                               esriSegmentExtension extendAt,
		                               double distance)
		{
			Assert.ArgumentNotNull(curve);

			double extensionDistance;
			bool extendAtFrom;

			if (extendAt == esriSegmentExtension.esriExtendTangents)
			{
				ExtendCurve(curve, esriSegmentExtension.esriExtendTangentAtFrom,
				            distance);
				ExtendCurve(curve, esriSegmentExtension.esriExtendTangentAtTo, distance);
				return;
			}

			if (extendAt == esriSegmentExtension.esriExtendTangentAtFrom ||
			    extendAt == esriSegmentExtension.esriExtendAtFrom)
			{
				extensionDistance = 0 - distance;
				extendAtFrom = true;
			}
			else if (extendAt == esriSegmentExtension.esriExtendTangentAtTo ||
			         extendAt == esriSegmentExtension.esriExtendAtTo)
			{
				extensionDistance = curve.Length + distance;
				extendAtFrom = false;
			}
			else
			{
				throw new ArgumentOutOfRangeException(
					nameof(extendAt),
					$"Currently unsupported segmentExtension method: {extendAt}. Must be esriExtendTangentAtFrom, esriExtendTangentAtTo or esriExtendTangents.");
			}

			// TODO: Test for non-linear curves - QueryTangent might be better and add the line 
			IConstructPoint newPoint = new PointClass();
			newPoint.ConstructAlong(curve, extendAt, extensionDistance, false);

			if (extendAtFrom)
			{
				curve.FromPoint = (IPoint) newPoint;
			}
			else
			{
				curve.ToPoint = (IPoint) newPoint;
			}
		}

		/// <summary>
		/// Extends the original polyline at the specified end to the intersection point
		/// with the specified target.
		/// </summary>
		/// <param name="original">The original polyline</param>
		/// <param name="target">The target curve</param>
		/// <param name="atEnd">The end point to extend</param>
		/// <param name="result">A (empty) polyline to be populated with the result.</param>
		/// <returns></returns>
		public static bool TryGetExtendedPolyline(
			[NotNull] IPolyline original,
			[NotNull] ICurve target,
			LineEnd atEnd,
			[NotNull] IPolyline result)
		{
			esriCurveExtension extension = GetCurveExtension(atEnd);

			return TryGetExtendedPolyline(original, target, extension, result);
		}

		public static bool TryGetExtendedPolyline(
			[NotNull] IPolyline original,
			[NotNull] ICurve target,
			esriCurveExtension extensionType,
			[NotNull] IPolyline result)
		{
			var isExtensionPerfomed = false;

			// If the original curve is a path, no extension is performed, even if there is an intersection.
			// The target can be a path (probably even a segment).
			((IConstructCurve) result).ConstructExtended(
				original, target, (int) extensionType, ref isExtensionPerfomed);

			return isExtensionPerfomed;
		}

		public static esriCurveExtension GetCurveExtension(LineEnd forProlongationAt)
		{
			switch (forProlongationAt)
			{
				case LineEnd.From:
					return esriCurveExtension.esriRelocateEnds |
					       esriCurveExtension.esriNoExtendAtTo;

				case LineEnd.To:
					return esriCurveExtension.esriRelocateEnds |
					       esriCurveExtension.esriNoExtendAtFrom;

				case LineEnd.Both:
					return esriCurveExtension.esriRelocateEnds;
				default:
					throw new ArgumentOutOfRangeException(
						$"{forProlongationAt} is not supported. Only <LineEnd.From> and <LineEnd.To> is supported");
			}
		}

		/// <summary>
		/// Enforces minimum width and height of the specified envelope.
		/// </summary>
		/// <param name="envelope"></param>
		/// <param name="minimumSize"></param>
		/// <returns>
		/// <c>true</c> if the envelope was adapted, <c>false</c> if the specified
		/// envelope was already in the expected form and was left unchanged.
		/// </returns>
		public static bool EnsureMinimumSize([NotNull] IEnvelope envelope,
		                                     double minimumSize)
		{
			Assert.ArgumentNotNull(envelope);

			double dx = 0;
			double dy = 0;

			if (envelope.Width < minimumSize)
			{
				dx = (minimumSize - envelope.Width) / 2;
			}

			if (envelope.Height < minimumSize)
			{
				dy = (minimumSize - envelope.Height) / 2;
			}

			if (dx > 0 || dy > 0)
			{
				envelope.Expand(dx, dy, false);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Allows the use of a spatial index on a geometry (if supported by the geometry type).
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		public static void AllowIndexing([NotNull] IGeometry geometry)
		{
			var spatialIndex = geometry as ISpatialIndex;

			if (spatialIndex != null)
			{
				spatialIndex.AllowIndexing = true;
			}
		}

		/// <summary>
		/// Disallows the use of a spatial index on a geometry (if supported by the geometry type).
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		public static void DisallowIndexing([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var spatialIndex = geometry as ISpatialIndex;
			if (spatialIndex != null)
			{
				spatialIndex.AllowIndexing = false;
			}
		}

		public static bool PointTouchesCurve([NotNull] IPoint point,
		                                     [NotNull] ICurve targetCurve,
		                                     out double distanceOnTarget,
		                                     bool asRatio)
		{
			Assert.ArgumentNotNull(point, nameof(point));
			Assert.ArgumentNotNull(targetCurve, nameof(targetCurve));

			distanceOnTarget = -1;

			IPoint outPoint = new PointClass();
			double fromDistance = -1;
			var rightSide = false;

			targetCurve.QueryPointAndDistance(esriSegmentExtension.esriNoExtension, point,
			                                  asRatio, outPoint, ref distanceOnTarget,
			                                  ref fromDistance, ref rightSide);

			return ((IRelationalOperator) point).Equals(outPoint);
		}

		public static void InsertPoints([NotNull] IPointCollection4 pointColl, int index,
		                                [NotNull] IGeometry insertGeometry)
		{
			if (insertGeometry is IPoint)
			{
				var points = new IPoint[1];
				points[0] = (IPoint) insertGeometry;

				GeometryBridge.InsertPoints(pointColl, index, ref points);
			}
			else
			{
				pointColl.InsertPointCollection(index,
				                                (IPointCollection) insertGeometry);
			}
		}

		public static void ReplacePoints([NotNull] IPointCollection4 pointColl,
		                                 int index,
		                                 int count,
		                                 [NotNull] IGeometry replaceGeometry)
		{
			if (replaceGeometry is IPoint)
			{
				var points = new IPoint[1];
				points[0] = (IPoint) replaceGeometry;
				GeometryBridge.ReplacePoints(pointColl, index, count, ref points);
			}
			else
			{
				pointColl.ReplacePointCollection(index, count,
				                                 (IPointCollection) replaceGeometry);
			}
		}

		[NotNull]
		public static IList<IGeometry> GetGeometryList([NotNull] IGeometry geometry)
		{
			IList<IGeometry> geometries = new List<IGeometry>();

			if (geometry is IGeometryCollection)
			{
				var collection = (IGeometryCollection) geometry;

				int geometryCount = collection.GeometryCount;
				for (var index = 0; index < geometryCount; index++)
				{
					geometries.Add(collection.Geometry[index]);
				}
			}
			else
			{
				geometries.Add(geometry);
			}

			return geometries;
		}

		[CanBeNull]
		public static IGeometry GetHitGeometryPart([NotNull] IPoint point,
		                                           [NotNull] IGeometry geometry,
		                                           double searchRadius)
		{
			int? partIndex = FindHitPartIndex(geometry, point, searchRadius);

			return partIndex != null
				       ? ((IGeometryCollection) geometry).Geometry[(int) partIndex]
				       : null;
		}

		/// <summary>
		/// Searches a vertex at a given point within the specified search tolerance.
		/// </summary>
		/// <param name="geometry">The geometry to search</param>
		/// <param name="point">The search point</param>
		/// <param name="searchTolerance">The search tolerance.</param>
		/// <param name="partIndex">Index of the part of the hit vertex.</param>
		/// <returns>
		/// The (local) vertex index of the hit part according to IHitTest or null if 
		/// the vertex was not found.
		/// </returns>
		public static int? FindHitVertexIndex([NotNull] IGeometry geometry,
		                                      [NotNull] IPoint point,
		                                      double searchTolerance,
		                                      out int partIndex)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(point, nameof(point));

			// do not waste memory - large geometries use several MB which stick in VB even when released
			const bool dontClone = true;
			IHitTest hitTest = GetHitTest(geometry, dontClone);

			IPoint hitPoint = GeometryFactory.Clone(PointTemplate);
			double hitDistance = 0;
			int segmentIndex = -1;
			partIndex = -1;
			var rightSide = false;

			bool found = hitTest.HitTest(
				point, searchTolerance,
				esriGeometryHitPartType.esriGeometryPartVertex, hitPoint,
				ref hitDistance, ref partIndex, ref segmentIndex, ref rightSide);

			return found
				       ? (int?) segmentIndex
				       : null;
		}

		/// <summary>
		/// Searches a vertex at a given point within the specified search tolerance.
		/// </summary>
		/// <param name="vertices">The vertices to search</param>
		/// <param name="point">The search point</param>
		/// <param name="searchTolerance2D">The search tolerance.</param>
		/// <returns>
		/// The indices of all vertices that were found within the search tolerance.
		/// </returns>
		public static IList<int> FindVertexIndices([NotNull] IPointCollection vertices,
		                                           [NotNull] IPoint point,
		                                           double searchTolerance2D)
		{
			Assert.ArgumentNotNull(vertices, nameof(vertices));
			Assert.ArgumentNotNull(point, nameof(point));

			var result = new List<int>();

			// NOTE: Even for multipoints, do not use IPointCollection3.IndexedEnumVertices to 
			// get vertex indices because they are wrong.

			for (var i = 0; i < vertices.PointCount; i++)
			{
				vertices.QueryPoint(i, PointTemplate);

				if (GetPointDistance(point, PointTemplate) <= searchTolerance2D)
				{
					result.Add(i);
				}
			}

			return result;
		}

		/// <summary>
		/// Searches a geometry part at a given point within the specified search tolerance.
		/// </summary>
		/// <param name="parts">The parts to search</param>
		/// <param name="point">The search point</param>
		/// <param name="searchTolerance2D">The search tolerance.</param>
		/// <returns>
		/// The indices of all parts that were found within the search tolerance.
		/// </returns>
		public static IEnumerable<int> FindPartIndices([NotNull] IGeometryCollection parts,
		                                               [NotNull] IPoint point,
		                                               double searchTolerance2D)
		{
			Assert.ArgumentNotNull(parts, nameof(parts));
			Assert.ArgumentNotNull(point, nameof(point));

			var result = new List<int>();

			if (((IGeometry) parts).GeometryType == esriGeometryType.esriGeometryMultipoint)
			{
				return FindVertexIndices((IPointCollection) parts, point, searchTolerance2D);
			}

			IPoint nearestPoint = GeometryFactory.Clone(PointTemplate);

			for (var i = 0; i < parts.GeometryCount; i++)
			{
				IGeometry part = parts.Geometry[i];

				var curve = part as ICurve;

				Assert.NotNull(curve,
				               "FindPartIndices is not implemented for the provided geometry type.");

				double distance = GetDistanceFromCurve(point, curve, nearestPoint);

				if (distance <= searchTolerance2D)
				{
					result.Add(i);
				}
			}

			return result;
		}

		/// <summary>
		/// Searches a vertex at a given point within the specified search tolerance.
		/// </summary>
		/// <param name="segments">The vertices to search</param>
		/// <param name="searchPoint">The search point</param>
		/// <param name="searchTolerance2D">The search tolerance.</param>
		/// <returns>
		/// The indices of all vertices that were found within the search tolerance.
		/// </returns>
		public static IList<int> FindSegmentIndices([NotNull] ISegmentCollection segments,
		                                            [NotNull] IPoint searchPoint,
		                                            double searchTolerance2D)
		{
			const bool excludeBoundaryMatches = false;
			return FindSegmentIndices(segments, searchPoint, searchTolerance2D,
			                          excludeBoundaryMatches);
		}

		/// <summary>
		/// Searches a vertex at a given point within the specified search tolerance.
		/// </summary>
		/// <param name="segments">The vertices to search</param>
		/// <param name="searchPoint">The search point</param>
		/// <param name="searchTolerance2D">The search tolerance.</param>
		/// <param name="excludeBoundaryMatches">Exclude segments that intersect the search point in the from- or to-point</param>
		/// <returns>
		/// The indices of all vertices that were found within the search tolerance.
		/// </returns>
		public static IList<int> FindSegmentIndices([NotNull] ISegmentCollection segments,
		                                            [NotNull] IPoint searchPoint,
		                                            double searchTolerance2D,
		                                            bool excludeBoundaryMatches)
		{
			Assert.ArgumentNotNull(segments, nameof(segments));
			Assert.ArgumentNotNull(searchPoint, nameof(searchPoint));

			var result = new List<int>();

			IEnvelope searchEnvelope = GetExpandedEnvelope(searchPoint, searchTolerance2D);

			// Only polycurves implement get_IndexedEnumSegments(searchEnvelope)
			IPolycurve polycurve = segments as IPolycurve ??
			                       (IPolycurve)
			                       GetHighLevelGeometry((IGeometry) segments, true);

			AllowIndexing(polycurve);

			IEnumSegment enumSegments =
				((ISegmentCollection) polycurve).IndexedEnumSegments[searchEnvelope];

			bool recycling = enumSegments.IsRecycling;

			ISegment segment;

			int partIndex = -1;
			int segmentIndex = -1;
			enumSegments.Next(out segment, ref partIndex, ref segmentIndex);

			while (segment != null)
			{
				// NOTE: ((IProximityOperator) point).ReturnDistance(segment) often returns the distance 
				//		 to the from/to point rather than to the closest point on the segment
				//		 -> Test QueryPointAndDistance, consider HNF?
				double distance = ((IProximityOperator) segment).ReturnDistance(searchPoint);

				if (distance <= searchTolerance2D &&
				    (! excludeBoundaryMatches ||
				     (! AreEqualInXY(searchPoint, segment.FromPoint) &&
				      ! AreEqualInXY(searchPoint, segment.ToPoint))))
				{
					result.Add(
						GetGlobalSegmentIndex((IGeometry) segments, partIndex, segmentIndex));
				}

				if (recycling)
				{
					// release the segment, otherwise "pure virtual function call" occurs 
					// when there are certain circular arcs (IsLine == true ?)
					Marshal.ReleaseComObject(segment);
				}

				enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
			}

			return result;
		}

		/// <summary>
		/// Searches a vertex at a given point within the specified search tolerance.
		/// </summary>
		/// <param name="multipoint">The multipoint to search</param>
		/// <param name="searchPoint">The search point</param>
		/// <param name="searchTolerance2D">The search tolerance.</param>
		/// <returns>
		/// The vertices that were found within the search tolerance.
		/// </returns>
		public static IEnumerable<IPoint> FindPoints([NotNull] IMultipoint multipoint,
		                                             [NotNull] IPoint searchPoint,
		                                             double searchTolerance2D)
		{
			Assert.ArgumentNotNull(multipoint, nameof(multipoint));
			Assert.ArgumentNotNull(searchPoint, nameof(searchPoint));

			AllowIndexing(multipoint);

			double size = 2 * searchTolerance2D;
			IEnvelope extent = GeometryFactory.CreateEnvelope(searchPoint, size, size);

			IEnumVertex enumVertex =
				((IPointCollection3) multipoint).IndexedEnumVertices[extent];

			Assert.NotNull(enumVertex, "pointCollection.IndexedEnumVertices is null");

			try
			{
				int partIndex;
				int vertexIndex;

				enumVertex.Reset();

				// NOTE: out partIndex, out vertexIndex are not the actual part/vertex indices!
				//       They seem to be the index of the found point (starting at 0)
				enumVertex.QueryNext(PointTemplate, out partIndex, out vertexIndex);

				// It seems that IndexedEnumVertices on Multipoint geometries
				// gives an empty point to signal end of enumeration
				while (! PointTemplate.IsEmpty && partIndex >= 0 && vertexIndex >= 0)
				{
					if (GetPointDistance(searchPoint, PointTemplate) <= searchTolerance2D)
					{
						IPoint resultPoint = GeometryFactory.Clone(PointTemplate);

						yield return resultPoint;
					}

					enumVertex.QueryNext(PointTemplate, out partIndex,
					                     out vertexIndex);
				}
			}
			finally
			{
				enumVertex.Reset();
				Marshal.ReleaseComObject(enumVertex);
			}
		}

		/// <summary>
		/// Searches a segment at a given point within the specified search tolerance.
		/// </summary>
		/// <param name="geometry">The geometry to search</param>
		/// <param name="point">The search point</param>
		/// <param name="searchTolerance">The search tolerance.</param>
		/// <param name="partIndex">Index of the part.</param>
		/// <returns>
		/// The (local) segment index of the hit part according to IHitTest or null if 
		/// the segment was not found.
		/// </returns>
		public static int? FindHitSegmentIndex([NotNull] IGeometry geometry,
		                                       [NotNull] IPoint point,
		                                       double searchTolerance,
		                                       out int partIndex)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(point, nameof(point));

			var multipatch = geometry as IMultiPatch;

			if (multipatch != null)
			{
				// Using HitTest on multipatches yields wrong results -> special implementation
				return FindHitSegmentIndex(multipatch, point, searchTolerance, out partIndex);
			}

			const bool dontClone = true;
			IHitTest hitTest = GetHitTest(geometry, dontClone);

			IPoint hitPoint = GeometryFactory.Clone(PointTemplate);
			double hitDistance = 0;
			int segmentIndex = -1;
			partIndex = -1;
			var rightSide = false;

			bool found = hitTest.HitTest(
				point, searchTolerance,
				esriGeometryHitPartType.esriGeometryPartBoundary, hitPoint,
				ref hitDistance, ref partIndex, ref segmentIndex, ref rightSide);

			if (hitTest != geometry)
			{
				Marshal.ReleaseComObject(hitTest);
			}

			return found
				       ? (int?) segmentIndex
				       : null;
		}

		/// <summary>
		/// Searches a segment at a given point within the specified search tolerance.
		/// </summary>
		/// <param name="multipatch">The geometry to search. Only rings consisting of segments are evaluated.</param>
		/// <param name="point">The search point</param>
		/// <param name="searchTolerance">The search tolerance.</param>
		/// <param name="partIndex">Index of the part.</param>
		/// <returns>
		/// The (local) segment index of the hit part or null if the segment was not found.
		/// </returns>
		public static int? FindHitSegmentIndex([NotNull] IMultiPatch multipatch,
		                                       [NotNull] IPoint point,
		                                       double searchTolerance,
		                                       out int partIndex)
		{
			Assert.ArgumentNotNull(multipatch, nameof(multipatch));
			Assert.ArgumentNotNull(point, nameof(point));

			var geometryCollection = (IGeometryCollection) multipatch;

			// Using HitTest directly on multipatches yields wrong results!
			for (var i = 0; i < geometryCollection.GeometryCount; i++)
			{
				var segments = geometryCollection.get_Geometry(i) as ISegmentCollection;

				if (segments == null)
				{
					_msg.VerboseDebug(() =>
						                  "FindHitSegmentIndex: Non-segments multipatch parts are not supported.");
					continue;
				}

				int? foundIndex = FindHitSegmentIndex((IGeometry) segments, point, searchTolerance,
				                                      out partIndex);

				if (foundIndex == null)
				{
					continue;
				}

				partIndex = i;
				return foundIndex;
			}

			partIndex = -1;
			return null;
		}

		/// <summary>
		/// Returns the part index whose boundary is within the search tolerance of the searchPoint or null.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="searchPoint"></param>
		/// <param name="searchTolerance"></param>
		/// <returns></returns>
		public static int? FindHitPartIndex([NotNull] IGeometry geometry,
		                                    [NotNull] IPoint searchPoint,
		                                    double searchTolerance)
		{
			IHitTest hitTest = GetHitTest(geometry, true);

			IPoint hitPoint = GeometryFactory.Clone(PointTemplate);

			double hitDistance = -1;
			int hitPart = -1, hitSegment = -1;
			var rightSide = false;

			bool found = hitTest.HitTest(
				searchPoint, searchTolerance,
				esriGeometryHitPartType.esriGeometryPartBoundary,
				hitPoint, ref hitDistance, ref hitPart,
				ref hitSegment, ref rightSide);

			return found
				       ? (int?) hitPart
				       : null;
		}

		/// <summary>
		/// Searches the vertex at a given search location within a search tolerance, and return the global
		/// point index within the geometry. If a vertex is found, the vertex parameter is updated. 
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="searchPoint">The search point.</param>
		/// <param name="searchTolerance">The search tolerance.</param>
		/// <param name="vertex">The vertex.</param>
		/// <returns></returns>
		public static int QueryVertex([NotNull] IPointCollection geometry,
		                              [NotNull] IPoint searchPoint,
		                              double searchTolerance,
		                              [NotNull] IPoint vertex)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(searchPoint, nameof(searchPoint));
			Assert.ArgumentNotNull(vertex, nameof(vertex));

			double hitDist = -1;
			int geoPartIndex = -1;
			int segmentIndex = -1;
			var rightSide = false;

			IHitTest hitTest = GetHitTest((IGeometry) geometry);

			if (hitTest.HitTest(searchPoint, searchTolerance,
			                    esriGeometryHitPartType.esriGeometryPartVertex, vertex,
			                    ref hitDist, ref geoPartIndex, ref segmentIndex,
			                    ref rightSide))
			{
				return GetGlobalIndex((IGeometry) geometry, geoPartIndex, segmentIndex);
			}

			return -1;
		}

		public static bool UpdateVertexZ([NotNull] IPointCollection geometry,
		                                 [NotNull] IPoint searchPoint,
		                                 double searchTolerance, double z)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(searchPoint, nameof(searchPoint));

			IPoint point = new PointClass();
			int index = QueryVertex(geometry, searchPoint, searchTolerance, point);

			if (index < 0)
			{
				return false;
			}

			point.Z = z;
			geometry.UpdatePoint(index, point);

			return true;
		}

		/// <summary>
		/// Returns the segment index as a global index in the geometry's segment collection.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="partIndex">index of the geometry part in the geometry collection</param>
		/// <param name="segmentIndex">index in the segment within the geometry part</param>
		/// <returns></returns>
		public static int GetGlobalSegmentIndex([NotNull] IGeometry geometry,
		                                        int partIndex,
		                                        int segmentIndex)
		{
			int result = segmentIndex;

			var collection = geometry as IGeometryCollection;

			if (collection != null && partIndex > 0)
			{
				for (var i = 0; i < partIndex; i++)
				{
					IGeometry partGeometry = collection.Geometry[i];
					var segments = (ISegmentCollection) partGeometry;

					result += segments.SegmentCount;
				}
			}

			return result;
		}

		/// <summary>
		/// Returns the point index as a global index in the geometry's pointcollection
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="partIndex">index of the geometry part in the geometry collection</param>
		/// <param name="pointIndex">index in the part of the geometry</param>
		/// <returns></returns>
		public static int GetGlobalIndex([NotNull] IGeometry geometry,
		                                 int partIndex,
		                                 int pointIndex)
		{
			int result = pointIndex;

			// in case of multipoint both partIndex and pointIndex are the same (also by hit test's opinion)
			if (geometry.GeometryType == esriGeometryType.esriGeometryMultipoint)
			{
				return pointIndex;
			}

			var collection = geometry as IGeometryCollection;

			if (collection != null && partIndex > 0)
			{
				for (var i = 0; i < partIndex; i++)
				{
					IGeometry partGeometry = collection.Geometry[i];

					var points = partGeometry as IPointCollection;
					if (points != null)
					{
						result += points.PointCount;
					}
					else if (partGeometry is IPoint)
					{
						result++;
					}
				}
			}

			return result;
		}

		public static bool PathIntersects([NotNull] IPath path, [NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(path, nameof(path));
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var relationalGeometry = geometry as IRelationalOperator;

			if (relationalGeometry == null)
			{
				relationalGeometry =
					(IRelationalOperator) GetHighLevelGeometry(geometry);
			}

			bool intersects;

			// Disjoint check on envelope is critical for performance
			if (Disjoint((IGeometry) relationalGeometry, path.Envelope))
			{
				intersects = false;
			}
			else
			{
				IGeometry highLevelPath = GetHighLevelGeometry(path);

				intersects = ! Disjoint((IGeometry) relationalGeometry, highLevelPath);

				Marshal.ReleaseComObject(highLevelPath);
			}

			if (relationalGeometry != geometry)
			{
				Marshal.ReleaseComObject(relationalGeometry);
			}

			return intersects;
		}

		public static bool IsPathWithin([NotNull] IPath path, [NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(path, nameof(path));
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var relationalGeometry = geometry as IRelationalOperator;

			if (relationalGeometry == null)
			{
				relationalGeometry =
					(IRelationalOperator) GetHighLevelGeometry(geometry);
			}

			bool contains;

			// Disjoint check is critical for performance
			if (Disjoint((IGeometry) relationalGeometry, path.Envelope))
			{
				contains = false;
			}
			else
			{
				IGeometry highLevelPath = GetHighLevelGeometry(path);

				contains = Contains((IGeometry) relationalGeometry, highLevelPath);

				Marshal.ReleaseComObject(highLevelPath);
			}

			if (relationalGeometry != geometry)
			{
				Marshal.ReleaseComObject(relationalGeometry);
			}

			return contains;
		}

		[CanBeNull]
		public static IGeometry FindNearestPart([NotNull] IGeometry fromGeo,
		                                        [NotNull] IGeometry searchGeo,
		                                        out int nearestPartIndex)
		{
			nearestPartIndex = 0;

			if (searchGeo is IPoint)
			{
				return searchGeo;
			}

			var geoColl = (IGeometryCollection) searchGeo;
			if (geoColl.GeometryCount == 1)
			{
				return searchGeo;
			}

			IGeometry nearestGeometry = null;
			double shortestDistance = double.NaN;

			var adjustProximit = (IProximityOperator) fromGeo;

			int geometryCount = geoColl.GeometryCount;
			for (var geoIndex = 0; geoIndex < geometryCount; geoIndex++)
			{
				IGeometry geometry = geoColl.Geometry[geoIndex];
				IGeometry highLevelGeo = geometry;
				if (geometry is ICurve)
				{
					highLevelGeo = GeometryFactory.CreatePolyline(geometry);
				}

				double minimalDistance = adjustProximit.ReturnDistance(highLevelGeo);

				if (double.IsNaN(shortestDistance) || shortestDistance > minimalDistance)
				{
					nearestGeometry = geometry;
					shortestDistance = minimalDistance;
					nearestPartIndex = geoIndex;
				}
			}

			return nearestGeometry;
		}

		/// <summary>
		/// Get the shortest connection between the specified from-point and the 
		/// specified curve to connect to. Additionally returns the connect point
		/// on the curve.
		/// </summary>
		/// <param name="connectFromPoint"></param>
		/// <param name="curveToConnectTo"></param>
		/// <param name="pointToConnectTo"></param>
		/// <returns></returns>
		[NotNull]
		public static IPath GetShortestConnection([NotNull] IPoint connectFromPoint,
		                                          [NotNull] ICurve curveToConnectTo,
		                                          [NotNull] out IPoint pointToConnectTo)
		{
			Assert.ArgumentNotNull(connectFromPoint, nameof(connectFromPoint));
			Assert.ArgumentNotNull(curveToConnectTo, nameof(curveToConnectTo));

			pointToConnectTo = GeometryFactory.Clone(PointTemplate);

			double distanceAlongSource = -1;
			double distanceToSource = -1;

			var rightSide = false;

			const bool asRatio = false;
			curveToConnectTo.QueryPointAndDistance(esriSegmentExtension.esriNoExtension,
			                                       connectFromPoint,
			                                       asRatio, pointToConnectTo,
			                                       ref distanceAlongSource,
			                                       ref distanceToSource, ref rightSide);

			return GeometryFactory.CreatePath(connectFromPoint, pointToConnectTo);
		}

		/// <summary>
		/// Returns the distance of the specified point along the specified curve.
		/// </summary>
		public static double GetDistanceAlongCurve([NotNull] ICurve targetCurve,
		                                           [NotNull] IPoint point, bool asRatio = false)
		{
			return GetDistanceAlongCurve(targetCurve, point, asRatio, out IPoint _);
		}

		/// <summary>
		/// Returns the distance of the specified point along the specified curve.
		/// </summary>
		/// <param name="targetCurve"></param>
		/// <param name="point"></param>
		/// <param name="asRatio"></param>
		/// <param name="pointOnCurve"></param>
		/// <returns></returns>
		public static double GetDistanceAlongCurve([NotNull] ICurve targetCurve,
		                                           [NotNull] IPoint point,
		                                           bool asRatio,
		                                           out IPoint pointOnCurve)
		{
			Assert.ArgumentNotNull(targetCurve);
			Assert.ArgumentNotNull(point);

			pointOnCurve = GeometryFactory.Clone(PointTemplate);

			double distanceFrom = -1;
			var rightSide = false;
			double distanceAlong = -1;

			targetCurve.QueryPointAndDistance(
				esriSegmentExtension.esriNoExtension, point, asRatio,
				pointOnCurve, ref distanceAlong, ref distanceFrom, ref rightSide);

			return distanceAlong;
		}

		/// <summary>
		/// Returns the list of points with their respective distance along the provided polycurve.
		/// </summary>
		/// <param name="points"></param>
		/// <param name="polycurve"></param>
		/// <param name="maxDistanceFromCurve">If larger 0 and not NaN, split points further away than maxDistanceFromCurve
		/// are ignored</param>
		/// <param name="projectedOnto">Wether the output points should be projected onto the polycurve or not</param>
		/// <returns></returns>
		public static List<KeyValuePair<IPoint, double>> GetDistancesAlongPolycurve(
			[NotNull] IPointCollection points,
			[NotNull] IPolycurve polycurve,
			double maxDistanceFromCurve,
			bool projectedOnto)
		{
			var pointsToUse = new List<KeyValuePair<IPoint, double>>(points.PointCount);

			for (var i = 0; i < points.PointCount; i++)
			{
				IPoint point = points.Point[i];

				IPoint outPoint = GeometryFactory.Clone(PointTemplate);

				double distanceFrom = -1;
				var rightSide = false;
				double distanceAlong = -1;

				const bool asRatio = false;
				polycurve.QueryPointAndDistance(
					esriSegmentExtension.esriNoExtension, point, asRatio,
					outPoint, ref distanceAlong, ref distanceFrom, ref rightSide);

				// filter 
				if (double.IsNaN(maxDistanceFromCurve) || maxDistanceFromCurve < 0 ||
				    distanceFrom <= maxDistanceFromCurve)
				{
					IPoint pointToAdd = projectedOnto
						                    ? outPoint
						                    : GeometryFactory.Clone(point);

					pointsToUse.Add(new KeyValuePair<IPoint, double>(pointToAdd, distanceAlong));
				}
			}

			return pointsToUse;
		}

		public static bool HaveUniqueGeometryType<T>(
			[NotNull] ICollection<T> geometries) where T : IGeometry
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));

			if (geometries.Count == 1)
			{
				return true;
			}

			var geometryType = esriGeometryType.esriGeometryNull;

			foreach (T geometry in geometries)
			{
				if (geometryType == esriGeometryType.esriGeometryNull)
				{
					geometryType = geometry.GeometryType;
				}
				else if (geometryType != geometry.GeometryType)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the union of the specified feature's geometries. The union
		/// is always a copy of the feature's shape even if only one feature
		/// is in the list.
		/// </summary>
		/// <param name="features">The features</param>
		/// <param name="projectToFeatureClassSpatialReference">Whether the output should have
		/// the projection of the feature's feature class or the original spatial
		/// reference of the first feature.</param>
		/// <returns></returns>
		[NotNull]
		public static IGeometry UnionFeatures(
			[NotNull] IList<IFeature> features,
			bool projectToFeatureClassSpatialReference = false)
		{
			Assert.ArgumentNotNull(features, nameof(features));
			Assert.True(features.Count > 0, "Feature list is empty.");

			var geometries = new List<IGeometry>(features.Count);

			foreach (IFeature feature in features)
			{
				IGeometry shape = feature.ShapeCopy;

				if (projectToFeatureClassSpatialReference)
				{
					// NOTE: when reading the shape, it is projected to the spatial reference of
					// the map. The xy tolerance of this spatial reference may be very small.
					// --> project back to the feature class spatial reference
					// NOTE: but this might not be desired in every case as a later simplify 
					// can change the geometry significantly if the geometry is not simple in
					// in the first place.
					EnsureSpatialReference(shape, feature);
				}

				geometries.Add(shape);
			}

			return UnionGeometries(geometries);
		}

		[NotNull]
		public static IEnvelope UnionGeometryEnvelopes(
			[NotNull] IEnumerable<IGeometry> geometries)
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));

			var result = new EnvelopeClass();
			var template = new EnvelopeClass();

			var first = true;
			foreach (IGeometry geometry in geometries)
			{
				if (first)
				{
					EnsureSpatialReference(result, geometry.SpatialReference);

					first = false;
				}

				geometry.QueryEnvelope(template);
				result.Union(template);
			}

			return result;
		}

		[NotNull]
		public static IGeometry Union(params IGeometry[] geometries)
		{
			return UnionGeometries(new List<IGeometry>(geometries));
		}

		[NotNull]
		public static IGeometry Union<T>([NotNull] IList<T> geometries) where T : IGeometry
		{
			return UnionGeometries(geometries);
		}

		[NotNull]
		public static IGeometry UnionGeometries<T>([NotNull] IList<T> geometries)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));
			Assert.ArgumentCondition(geometries.Count > 0, "geometries is empty");

			if (geometries.Count == 1)
			{
				return geometries[0];
			}

			Assert.True(HaveUniqueGeometryType(geometries),
			            "All geometries must have the same geometry type");

			var allZAware = true;
			var allMAware = true;
			foreach (T geometry in geometries)
			{
				if (! ((IMAware) geometry).MAware)
				{
					allMAware = false;
				}

				if (! ((IZAware) geometry).ZAware)
				{
					allZAware = false;
				}
			}

			IGeometry result;
			switch (geometries[0].GeometryType)
			{
				case esriGeometryType.esriGeometryPolyline:
				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryMultiPatch:
					result = UnionPolylinePolygonMultipatchGeometries(geometries);
					break;

				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryMultipoint:
					result = UnionPointGeometries(geometries);
					break;

				default:
					throw new ArgumentException(
						"Only point, multipoint, polyline, multipatch and " +
						"polygon geometries can be used for union.");
			}

			((IZAware) result).ZAware = allZAware;
			((IMAware) result).MAware = allMAware;

			return result;
		}

		[NotNull]
		private static IGeometry UnionPolylinePolygonMultipatchGeometries<T>(
			[NotNull] IList<T> geometries) where T : IGeometry
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));
			Assert.ArgumentCondition(geometries.Count > 0, "geometries is empty");

			IGeometry firstGeometry = geometries[0];

			ISpatialReference spatialReference = firstGeometry.SpatialReference;

			IGeometry leftHandGeometry;
			switch (firstGeometry.GeometryType)
			{
				case esriGeometryType.esriGeometryPolygon:
					leftHandGeometry = new PolygonClass();
					break;

				case esriGeometryType.esriGeometryPolyline:
					leftHandGeometry = new PolylineClass();
					break;

				case esriGeometryType.esriGeometryMultiPatch:
					leftHandGeometry = new MultiPatchClass();
					break;

				default:
					throw new ArgumentException("Unsupported geometry type");
			}

			leftHandGeometry.SpatialReference = spatialReference;

			IGeometryBag bag = new GeometryBagClass
			                   {
				                   SpatialReference = spatialReference
			                   };

			foreach (T geometry in geometries)
			{
				object missing = Type.Missing;
				((IGeometryCollection) bag).AddGeometry(geometry, ref missing, ref missing);
			}

			try
			{
				var topoOp = (ITopologicalOperator) leftHandGeometry;
				topoOp.ConstructUnion((IEnumGeometry) bag);
			}
			catch (Exception ex)
			{
				_msg.Debug(string.Format("Exception in ConstructUnion. {0}", ToString(bag)), ex);
				throw;
			}

			return leftHandGeometry;
		}

		[NotNull]
		private static IGeometry UnionPointGeometries<T>(
			[NotNull] IEnumerable<T> geometries) where T : IGeometry
		{
			IMultipoint multipoint = new MultipointClass();

			var pointColl = (IPointCollection) multipoint;

			foreach (T geometry in geometries)
			{
				var point = geometry as IPoint;
				if (point != null)
				{
					object missing = Type.Missing;
					pointColl.AddPoint(point, ref missing, ref missing);
				}
				else
				{
					pointColl.AddPointCollection((IPointCollection) geometry);
				}
			}

			return (IGeometry) pointColl;
		}

		[CanBeNull]
		public static IGeometry GetUnionExterior([NotNull] IPolygon firstPolygon,
		                                         [NotNull] IPolygon secondPolygon)
		{
			if (((IRelationalOperator) firstPolygon).Disjoint(secondPolygon))
			{
				return null;
			}

			var topoOp = (ITopologicalOperator) firstPolygon;

			IGeometry unionGeo;
			try
			{
				unionGeo = topoOp.Union(secondPolygon);
			}
			catch (Exception)
			{
				_msg.DebugFormat("firstPolygon: {0}", ToString(firstPolygon));
				_msg.DebugFormat("secondPolygon: {0}", ToString(secondPolygon));
				throw;
			}

			if (unionGeo == null || unionGeo.IsEmpty || ! (unionGeo is IPolygon))
			{
				return null;
			}

			return GeometryFactory.CreatePolygon(
				((IGeometryCollection) unionGeo).Geometry[0],
				unionGeo.SpatialReference);
		}

		[NotNull]
		public static IHitTest GetHitTest([NotNull] IGeometry geometry)
		{
			const bool dontClone = false;
			return GetHitTest(geometry, dontClone);
		}

		/// <summary>
		/// Returns IHitTest on a new geometry that references the input geometry
		/// rather than a copy of it. 
		/// This allows for using the hit-test even after changing the geometry and
		/// is more memory-efficient.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="dontClone"></param>
		/// <returns></returns>
		public static IHitTest GetHitTest(IGeometry geometry, bool dontClone)
		{
			return (IHitTest) GetHighLevelGeometry(geometry, dontClone);
		}

		[NotNull]
		public static ITopologicalOperator GetTopoOperator([NotNull] IGeometry geometry)
		{
			return (ITopologicalOperator) GetHighLevelGeometry(geometry);
		}

		/// <summary>
		/// Returns a hgih-level geometry which either references the low-level-geometry directly
		/// or a copy of it.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="dontClonePath">Whether the low-level geometry is added directly as a reference
		/// or as a reference to a clone. So far this applies only in case the low-level geometry is a path or ring.
		/// points, polylines, polygons, segments, envelopes are always cloned.
		/// Note: if the input path is not cloned do *not* com-release the returned high-level geometry if
		/// the input path is still used.</param>
		/// <returns></returns>
		[NotNull]
		public static IGeometry GetHighLevelGeometry([NotNull] IGeometry geometry,
		                                             bool dontClonePath = false)
		{
			// TODO: consider general option dontClone -> also for high-level or single segment input

			Assert.ArgumentNotNull(geometry, nameof(geometry));

			switch (geometry.GeometryType)
			{
				case esriGeometryType.esriGeometryPoint:
				case esriGeometryType.esriGeometryMultipoint:
				case esriGeometryType.esriGeometryPolyline:
				case esriGeometryType.esriGeometryPolygon:
				case esriGeometryType.esriGeometryMultiPatch:
					return geometry;

				case esriGeometryType.esriGeometryLine:
				case esriGeometryType.esriGeometryEllipticArc:
				case esriGeometryType.esriGeometryBezier3Curve:
				case esriGeometryType.esriGeometryCircularArc:
					return GeometryFactory.CreatePolyline(geometry);

				case esriGeometryType.esriGeometryEnvelope:
					return GeometryFactory.CreatePolygon(geometry);

				case esriGeometryType.esriGeometryPath:
				case esriGeometryType.esriGeometryRing:
					return GetHighLevelGeometry((IPath) geometry, dontClonePath);

				default:
					throw new ArgumentOutOfRangeException(nameof(geometry),
					                                      geometry.GeometryType,
					                                      @"Geometry type not supported");
			}
		}

		[NotNull]
		private static IGeometry GetHighLevelGeometry([NotNull] IPath path, bool dontClone)
		{
			IGeometry result = null;

			if (dontClone)
			{
				if (path.GeometryType == esriGeometryType.esriGeometryRing)
				{
					result = GeometryFactory.CreatePolygon(
						path.SpatialReference, IsZAware(path), IsMAware(path));
				}
				else if (path.GeometryType == esriGeometryType.esriGeometryPath)
				{
					result = GeometryFactory.CreatePolyline(
						path.SpatialReference, IsZAware(path), IsMAware(path));
				}

				Assert.NotNull(result, "Unsupported geometry type: {0}", path.GeometryType);

				object emptyRef = Type.Missing;

				((IGeometryCollection) result).AddGeometry(path, ref emptyRef, ref emptyRef);
			}
			else
			{
				if (path.GeometryType == esriGeometryType.esriGeometryRing)
				{
					result = GeometryFactory.CreatePolygon(path);
				}
				else if (path.GeometryType == esriGeometryType.esriGeometryPath)
				{
					result = GeometryFactory.CreatePolyline(path);
				}
				else
				{
					Assert.CantReach("Unsupported geometry type: {0}", path.GeometryType);
				}
			}

			return result;
		}

		/// <summary>
		/// Ensures that a given geometry is simple. Do not call this method in performance-
		/// critical situations - better use Simplify() directly.
		/// NOTE: Self-intersections are allowed for Polyline features.
		/// </summary>
		/// <param name="geometry">The geometry. 
		/// NOTE: This method assumes that the geometry already has the target spatial reference 
		/// (same spatial refernce as the feature class in which it will be stored.</param>
		/// <returns><c>true</c> if the geometry was simplified, <c>false</c> if 
		/// it was already simple.</returns>
		/// <remarks>If the geometry is not simple, the input geometry is 
		/// modified by being simplified.</remarks>
		public static bool EnsureSimple([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			string nonSimpleReason;

			if (IsGeometrySimple(geometry, geometry.SpatialReference, true,
			                     out nonSimpleReason))
			{
				return false;
			}

			_msg.DebugFormat("Geometry needs to be simplified because {0}",
			                 nonSimpleReason);

			Simplify(geometry);

			return true;
		}

		/// <summary>
		/// Ensures that a given geometry is simple. Do not call this method in performance-
		/// critical situations - better use Simplify() directly.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="targetSpatialReference">The spatial reference in which the geometry should be 
		/// storable. Its tolerance is used during the simple-check.</param>
		/// <param name="allowNonPlanarLines">Whether non-planar polyline geometries (i.e. those with self-
		/// intersections should be considered simple or not. NOTE: In geometric networks self-intersections
		/// are allowed (except loops) and multipart geometries are not allowed (which would be created by 
		/// the standard simplify operation).</param>
		/// <returns><c>true</c> if the geometry was simplified, <c>false</c> if 
		/// it was already simple.</returns>
		/// <remarks>If the geometry is not simple, the input geometry is 
		/// modified by being simplified.</remarks>
		public static bool EnsureSimple([NotNull] IGeometry geometry,
		                                [NotNull] ISpatialReference targetSpatialReference,
		                                bool allowNonPlanarLines)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(targetSpatialReference, nameof(targetSpatialReference));

			string nonSimpleReason;

			if (IsGeometrySimple(geometry, geometry.SpatialReference, true,
			                     out nonSimpleReason))
			{
				return false;
			}

			_msg.DebugFormat("Geometry needs to be simplified because {0}",
			                 nonSimpleReason);

			Simplify(geometry);

			return true;
		}

		/// <summary>
		/// Ensures that a given geometry is simple. The result
		/// is returned as an out parameter, which is either the original geometry if it 
		/// already is simple, or a simplified <b>copy</b> if the input was not simple.
		/// </summary>
		/// <param name="immutableGeometry">The geometry.</param>
		/// <returns><c>true</c> if the output is a simplified copy, <c>false</c> if 
		/// the output is the same instance as the input, which was already simple.</returns>
		/// <param name="simpleGeometry">simple geometry.</param>
		public static bool EnsureSimple<T>([NotNull] T immutableGeometry,
		                                   [NotNull] out T simpleGeometry)
			where T : IGeometry
		{
			if (IsGeometrySimple(immutableGeometry, immutableGeometry.SpatialReference,
			                     true, out string _))
			{
				simpleGeometry = immutableGeometry;
				return false;
			}

			T clone = GeometryFactory.Clone(immutableGeometry);

			Simplify(clone);

			simpleGeometry = clone;
			return true;
		}

		/// <summary>
		/// Verifies if a geometry is simple or not. NOTE: Do not use in performance-critical situations!
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="targetSpatialReference">The spatial reference in which the geometry should be
		/// storable. Its tolerance is used during the simple-check.</param>
		/// <param name="allowNonPlanarLines">Whether non-planar polyline geometries (i.e. those with self-
		/// intersections should be considered simple or not. NOTE: In geometric networks self-intersections
		/// are allowed (except loops) and multipart geometries are not allowed (which would be created by
		/// the standard simplify operation).</param>
		/// <param name="nonSimpleReasonDescription">The reason why the geometry is not simple.</param>
		/// <returns>
		///   <c>true</c> if the geometry is simple, <c>false</c> if it is not simple.
		/// </returns>
		public static bool IsGeometrySimple(
			[NotNull] IGeometry geometry,
			[NotNull] ISpatialReference targetSpatialReference,
			bool allowNonPlanarLines,
			[NotNull] out string nonSimpleReasonDescription)
		{
			return IsGeometrySimple(geometry, targetSpatialReference, allowNonPlanarLines,
			                        out nonSimpleReasonDescription, out _);
		}

		/// <summary>
		/// Verifies if a geometry is simple or not. NOTE: Do not use in performance-critical situations!
		/// ITopologicalOperator3.get_IsSimpleEx is generally slower than Simplify()
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="targetSpatialReference">The spatial reference in which the geometry should be
		/// storable. Its tolerance is used during the simple-check.</param>
		/// <param name="allowNonPlanarLines">Whether non-planar polyline geometries (i.e. those with self-
		/// intersections should be considered simple or not. NOTE: In geometric networks self-intersections
		/// are allowed (except loops) and multipart geometries are not allowed (which would be created by
		/// the standard simplify operation).</param>
		/// <param name="nonSimpleReasonDescription">The reason why the geometry is not simple.</param>
		/// <param name="nonSimpleReason">The reason why the geometry is non-simple.</param>
		/// <returns>
		///   <c>true</c> if the geometry is simple, <c>false</c> if it is not simple.
		/// </returns>
		public static bool IsGeometrySimple(
			[NotNull] IGeometry geometry,
			[NotNull] ISpatialReference targetSpatialReference,
			bool allowNonPlanarLines,
			[NotNull] out string nonSimpleReasonDescription,
			out GeometryNonSimpleReason? nonSimpleReason)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(targetSpatialReference, nameof(targetSpatialReference));

			nonSimpleReasonDescription = string.Empty;

			// work on a copy and make sure the tolerance is the one of the feature's spatial reference
			// NOTE: ITopologicalOperator3.get_IsSimpleEx changes the geometry in a non-obvious way 
			//       (e.g. ISegmentCollection.IndexedEnumSegments does not fail any more on polylines after IsSimpleEx
			IGeometry geometryCopy = GeometryFactory.Clone(geometry);

			try
			{
				EnsureSpatialReference(geometryCopy, targetSpatialReference);

				double xyTolerance = GetXyTolerance(geometryCopy);

				var topoOp3 = geometryCopy as ITopologicalOperator3;

				if (topoOp3 != null)
				{
					topoOp3.IsKnownSimple_2 = false;

					esriNonSimpleReasonEnum reason;
					if (! topoOp3.get_IsSimpleEx(out reason))
					{
						return TranslateNonSimpleReason(reason,
						                                geometryCopy,
						                                xyTolerance,
						                                allowNonPlanarLines,
						                                out nonSimpleReasonDescription,
						                                out nonSimpleReason);
					}

					// for multi-part polygons: exactly equal rings are not detected as non-simple
					var polygon = geometryCopy as IPolygon;
					if (polygon != null)
					{
						if (HasClosedRingsEqualInXY(polygon))
						{
							nonSimpleReasonDescription =
								LocalizableStrings.GeometryUtils_IsGeometrySimple_IdenticalRings;
							nonSimpleReason = GeometryNonSimpleReason.IdenticalRings;
							return false;
						}
					}
				}
				else
				{
					var topoOp2 = geometryCopy as ITopologicalOperator2;
					if (topoOp2 != null)
					{
						// Multipoints (probably)
						topoOp2.IsKnownSimple_2 = false;
						if (! topoOp2.IsSimple)
						{
							if (geometryCopy.GeometryType ==
							    esriGeometryType.esriGeometryMultipoint)
							{
								nonSimpleReason = GeometryNonSimpleReason.DuplicatePoints;
								nonSimpleReasonDescription =
									LocalizableStrings
										.GeometryUtils_NonSimpleReason_DuplicatePoints;
							}
							else
							{
								nonSimpleReason = GeometryNonSimpleReason.Unknown;
								nonSimpleReasonDescription =
									LocalizableStrings.GeometryUtils_NonSimpleReason_Unknown;
							}

							return false;
						}
					}
				}
			}
			finally
			{
				Marshal.ReleaseComObject(geometryCopy);
			}

			// Points, always simple
			nonSimpleReason = null;
			return true;
		}

		private static bool HasClosedRingsEqualInXY([NotNull] IPolygon polygon)
		{
			var rings = (IGeometryCollection) polygon;

			if (rings.GeometryCount < 2)
			{
				return false;
			}

			var ringsByKey = new Dictionary<string, List<IRing>>(StringComparer.Ordinal);

			foreach (IRing ring in GetRings(polygon))
			{
				if (ring.IsEmpty || ! ring.IsClosed)
				{
					continue;
				}

				string key = string.Format("{0}:{1}",
				                           ((IPointCollection) ring).PointCount,
				                           ring.IsExterior
					                           ? "x"
					                           : "i");

				List<IRing> similarRings;
				if (! ringsByKey.TryGetValue(key, out similarRings))
				{
					similarRings = new List<IRing>();
					ringsByKey.Add(key, similarRings);
				}

				similarRings.Add(ring);
			}

			IEnvelope envelope1 = null;
			IEnvelope envelope2 = null;
			foreach (List<IRing> similarRings in ringsByKey.Values)
			{
				int count = similarRings.Count;

				if (count < 2)
				{
					continue;
				}

				var ringPolygonsByIndex = new Dictionary<int, IPolygon>();

				try
				{
					for (var i = 0; i < count - 1; i++)
					{
						IRing ring1 = similarRings[i];
						if (envelope1 == null)
						{
							envelope1 = new EnvelopeClass();
						}

						ring1.QueryEnvelope(envelope1);
						((IZAware) envelope1).ZAware = false;

						var envelope1RelOp = (IRelationalOperator) envelope1;

						for (int j = i + 1; j < count; j++)
						{
							IRing ring2 = similarRings[j];
							if (envelope2 == null)
							{
								envelope2 = new EnvelopeClass();
							}

							ring2.QueryEnvelope(envelope2);
							((IZAware) envelope2).ZAware = false;

							// NOTE: IRelationalOperator.Equals() for envelopes DOES compare Z values also,
							// contrary to the documentation. 2D comparison only if envelopes are Z-unaware.
							if (! envelope1RelOp.Equals(envelope2))
							{
								continue;
							}

							// point count, orientation and envelopes are equal
							// -> compare ring polygons for 2D equality

							IPolygon ring1Polygon;
							if (! ringPolygonsByIndex.TryGetValue(i, out ring1Polygon))
							{
								ring1Polygon = GeometryFactory.CreatePolygon(ring1);
								ringPolygonsByIndex.Add(i, ring1Polygon);
							}

							IPolygon ring2Polygon;
							if (! ringPolygonsByIndex.TryGetValue(j, out ring2Polygon))
							{
								ring2Polygon = GeometryFactory.CreatePolygon(ring2);
								ringPolygonsByIndex.Add(j, ring2Polygon);
							}

							if (AreEqualInXY(ring1Polygon, ring2Polygon))
							{
								// two equal rings found
								return true;
							}
						}
					}
				}
				finally
				{
					foreach (IPolygon ringPolygon in ringPolygonsByIndex.Values)
					{
						Marshal.ReleaseComObject(ringPolygon);
					}
				}
			}

			if (envelope1 != null)
			{
				Marshal.ReleaseComObject(envelope1);
			}

			if (envelope2 != null)
			{
				Marshal.ReleaseComObject(envelope2);
			}

			return false;
		}

		/// <summary>
		/// Translates the non simple reason into an explanation, and returns a value
		/// indicating if the geometry is *really* not simple
		/// </summary>
		/// <param name="nonSimpleReason">The non simple reason.</param>
		/// <param name="geometry">The geometry.</param>
		/// <param name="xyTolerance">The xy tolerance.</param>
		/// <param name="allowNonPlanarLines">if set to <c>true</c> non-planar (self-intersecting)
		/// lines are allowed.</param>
		/// <param name="nonSimpleReasonDescription">The non simple explanation.</param>
		/// <param name="reason">The reason.</param>
		/// <returns>
		///   <c>false</c> if the value is not simple, <c>true</c> if it can be
		/// considered simple even if the reason passed in indicates that it isn't
		/// </returns>
		private static bool TranslateNonSimpleReason(
			esriNonSimpleReasonEnum nonSimpleReason,
			[NotNull] IGeometry geometry,
			double xyTolerance,
			bool allowNonPlanarLines,
			[NotNull] out string nonSimpleReasonDescription,
			out GeometryNonSimpleReason? reason)
		{
			switch (nonSimpleReason)
			{
				case esriNonSimpleReasonEnum.esriNonSimpleShortSegments:

					nonSimpleReasonDescription =
						LocalizableStrings.GeometryUtils_NonSimpleReason_ShortSegments;
					reason = GeometryNonSimpleReason.ShortSegments;
					return false;

				case esriNonSimpleReasonEnum.esriNonSimpleSelfIntersections:
					// NOTE: The typical short segments case is also reported as esriNonSimpleSelfIntersections
					//		 both above and below the xy tolerance
					const bool use3DLength = false;
					const IPolygon perimeter = null;
					bool hasShortSegments = HasShortSegments(
						(ISegmentCollection) geometry, xyTolerance,
						use3DLength, perimeter);

					if (hasShortSegments)
					{
						nonSimpleReasonDescription =
							LocalizableStrings.GeometryUtils_NonSimpleReason_ShortSegments;
						reason = GeometryNonSimpleReason.ShortSegments;
						return false;
					}

					// TODO: check what IsSimple returns for lines with M values.
					if (allowNonPlanarLines && geometry is IPolyline)
					{
						nonSimpleReasonDescription =
							LocalizableStrings
								.GeometryUtils_NonSimpleReason_SelfIntersections_Allowed;
						reason = null;
						return true;
					}

					nonSimpleReasonDescription =
						LocalizableStrings.GeometryUtils_NonSimpleReason_SelfIntersections;
					reason = GeometryNonSimpleReason.SelfIntersections;
					return false;

				case esriNonSimpleReasonEnum.esriNonSimpleUnclosedRing:
					nonSimpleReasonDescription =
						LocalizableStrings.GeometryUtils_NonSimpleReason_UnclosedRing;
					reason = GeometryNonSimpleReason.UnclosedRing;
					return false;

				case esriNonSimpleReasonEnum.esriNonSimpleEmptyPart:
					nonSimpleReasonDescription =
						LocalizableStrings.GeometryUtils_NonSimpleReason_EmptyPart;
					reason = GeometryNonSimpleReason.EmptyPart;
					return false;

				case esriNonSimpleReasonEnum.esriNonSimpleRingOrientation:
					nonSimpleReasonDescription =
						LocalizableStrings.GeometryUtils_NonSimpleReason_RingOrientation;
					reason = GeometryNonSimpleReason.IncorrectRingOrientation;
					return false;

				case esriNonSimpleReasonEnum.esriNonSimpleSegmentOrientation:
					nonSimpleReasonDescription =
						LocalizableStrings.GeometryUtils_NonSimpleReason_SegmentOrientation;
					reason = GeometryNonSimpleReason.IncorrectSegmentOrientation;
					return false;

				case esriNonSimpleReasonEnum.esriNonSimpleOK:
					nonSimpleReasonDescription = string.Empty;
					reason = null;
					return true;

				default:
					throw new ArgumentOutOfRangeException(nameof(nonSimpleReason),
					                                      nonSimpleReason,
					                                      @"Unknown nonSimpleReason");
			}
		}

		/// <summary>
		/// Verifies if a geometry is simple or not. NOTE: Do not use in performance-critical situations!
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="targetFeatureClass">The feature class in which the geometry should be storable. It is 
		/// used to determine if it is a geometric network edge geometry and thus self-intersections are 
		/// allowed. Additionally the tolerance of the feature class' spatial reference is used during the
		/// simple-check.</param>
		/// <param name="allowNonPlanarLines"></param>
		/// <param name="nonSimpleReason">The reason why the geometry is not simple.</param>
		/// <returns><c>true</c> if the geometry is simple, <c>false</c> if it is not simple.</returns>
		public static bool IsGeometrySimple([NotNull] IGeometry geometry,
		                                    [NotNull] IFeatureClass targetFeatureClass,
		                                    bool allowNonPlanarLines,
		                                    out string nonSimpleReason)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(targetFeatureClass, nameof(targetFeatureClass));

			ISpatialReference sref = ((IGeoDataset) targetFeatureClass).SpatialReference;

			allowNonPlanarLines =
				allowNonPlanarLines ||
				targetFeatureClass.FeatureType == esriFeatureType.esriFTSimpleEdge ||
				targetFeatureClass.FeatureType == esriFeatureType.esriFTComplexEdge;

			bool simple = IsGeometrySimple(geometry, sref, allowNonPlanarLines,
			                               out nonSimpleReason);

			return simple;
		}

		/// <summary>
		/// Ensures that all segments will habe a lengt >= minSegmentLength.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="minSegmentLength">Minimal length of a segment.</param>
		/// <returns>New geometry with all segment-lengths >= minSegmentLength</returns>
		public static T EnsureMinimalSegmentLength<T>([NotNull] T geometry,
		                                              double minSegmentLength)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.True(geometry is ISegmentCollection,
			            "Only geometries supporting ISegmentCollection " +
			            "can be ensured for minimal segment lengths.");

			var removePointIndexes = new Dictionary<int, IList<int>>();

			var geoColl = (IGeometryCollection) GeometryFactory.Clone(geometry);

			int geometryCount = geoColl.GeometryCount;

			for (var geoIndex = 0; geoIndex < geometryCount; geoIndex++)
			{
				var segments = (ISegmentCollection) geoColl.Geometry[geoIndex];
				double partLength = 0;

				IList<int> removeIndexes = new List<int>();
				int nrSegments = segments.SegmentCount;

				if (nrSegments > 2)
				{
					for (var segIndex = 0; segIndex < nrSegments - 1; segIndex++)
					{
						ISegment segment = segments.Segment[segIndex];

						if (partLength + segment.Length < minSegmentLength)
						{
							int removePointIndex = segIndex + 1;

							// If only one single segment is to short (both
							// neighbours are long enough) then remove the point
							// on the side, where the angle between with the neighbour
							// is smaller
							if (Math.Abs(partLength) < double.Epsilon &&
							    removePointIndex < nrSegments &&
							    segIndex > 0)
							{
								ISegment previousSegment =
									segments.Segment[segIndex - 1];

								ISegment nextSegment =
									segments.Segment[removePointIndex];

								if (previousSegment.Length > minSegmentLength &&
								    nextSegment.Length > minSegmentLength)
								{
									if (Math.Abs(GetAngle(previousSegment, segment)) >
									    Math.Abs(GetAngle(segment, nextSegment)))
									{
										removePointIndex = segIndex;
									}
								}
							}

							// Add remove point if it is not already in remove list
							if (! removeIndexes.Contains(removePointIndex))
							{
								removeIndexes.Add(removePointIndex);
								partLength += segment.Length;
							}
						}
						else
						{
							partLength = 0;
						}
					}

					if (removeIndexes.Count > 0)
					{
						removePointIndexes.Add(geoIndex, removeIndexes);
					}
				}
			}

			foreach (int geoIndex in removePointIndexes.Keys)
			{
				IList<int> remIndexes = removePointIndexes[geoIndex];

				var points = (IPointCollection) geoColl.Geometry[geoIndex];

				//                for (int i=0; i<remIndexes.Count; i++)
				//                {
				//                    points.RemovePoints(remIndexes[i]-i, 1);
				//                }

				for (int i = remIndexes.Count - 1; i > -1; i--)
				{
					points.RemovePoints(remIndexes[i], 1);
				}
			}

			return (T) geoColl;
		}

		/// <summary>
		/// Calculates the 2D angle (in degrees) between two segments. If the segments are
		/// non-linear, then the angle is calculated between the trend lines 
		/// (straight lines between start/endpoint)
		/// </summary>
		/// <param name="firstSegment">The first segment.</param>
		/// <param name="secondSegment">The second segment.</param>
		/// <returns>Angle between trend lines of segments (degrees)</returns>
		public static double GetAngle([NotNull] ISegment firstSegment,
		                              [NotNull] ISegment secondSegment)
		{
			Assert.ArgumentNotNull(firstSegment, nameof(firstSegment));
			Assert.ArgumentNotNull(secondSegment, nameof(secondSegment));

			var firstLine = firstSegment as ILine;
			if (firstLine == null)
			{
				// TODO use tangent (where?)
				// not a straight line - not really supported. Approximate by end/end line
				firstLine = new LineClass();
				firstLine.PutCoords(firstSegment.FromPoint, firstSegment.ToPoint);
			}

			var secondLine = secondSegment as ILine;
			if (secondLine == null)
			{
				// TODO use tangent (where?)
				// not a straight line - not really supported. Approximate by end/end line
				secondLine = new LineClass();
				secondLine.PutCoords(secondSegment.FromPoint, secondSegment.ToPoint);
			}

			double angle = 180 * (secondLine.Angle - firstLine.Angle) / Math.PI;
			angle = angle % 360;

			if (angle < 0)
			{
				angle += 360;
			}

			return angle;
		}

		/// <summary>
		/// Returns the angle in radians at point b between the vectors ba and bc.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public static double GetAngle3DInRad([NotNull] IPoint a,
		                                     [NotNull] IPoint b,
		                                     [NotNull] IPoint c)
		{
			// Get the 2 vectors
			double v1X = a.X - b.X;
			double v1Y = a.Y - b.Y;
			double v1Z = a.Z - b.Z;

			double v2X = c.X - b.X;
			double v2Y = c.Y - b.Y;
			double v2Z = c.Z - b.Z;

			return GeomUtils.GetAngle3DInRad(v1X, v1Y, v1Z, v2X, v2Y, v2Z);
		}

		/// <summary>
		/// Calculates the vertices of a polygon with a provided centre point, radius and number of vertices.
		/// The first vertex can be defined by vertexOnStartRadians, i.e. it will be on the line between the
		/// centre and the provided vertexOnStartRadians.
		/// </summary>
		/// <param name="centre"></param>
		/// <param name="radius"></param>
		/// <param name="vertexCount"></param>
		/// <param name="vertexOnStartRadians"></param>
		/// <returns></returns>
		public static WKSPointZ[] CalculateRegularPolyVertices(
			[NotNull] IPoint centre,
			double radius,
			int vertexCount,
			[CanBeNull] IPoint vertexOnStartRadians)
		{
			double radiansOfIntersectingVertex = vertexOnStartRadians == null
				                                     ? 0
				                                     : Math.Atan2(
					                                     vertexOnStartRadians.Y - centre.Y,
					                                     vertexOnStartRadians.X - centre.X);

			WKSPointZ[] result = CalculateRegularPolyVertices(centre, radius, vertexCount,
			                                                  radiansOfIntersectingVertex);

			return result;
		}

		/// <summary>
		/// Calculates the vertices of a polygon with a provided centre point, radius and number of vertices.
		/// The first vertex can be defined by startRadians.
		/// </summary>
		/// <param name="centre"></param>
		/// <param name="radius"></param>
		/// <param name="vertexCount"></param>
		/// <param name="startRadians"></param>
		/// <returns></returns>
		public static WKSPointZ[] CalculateRegularPolyVertices([NotNull] IPoint centre,
		                                                       double radius,
		                                                       int vertexCount,
		                                                       double startRadians)
		{
			Assert.ArgumentNotNull(centre, nameof(centre));
			Assert.ArgumentCondition(radius > 0, "Radius must be greater than 0.");
			Assert.ArgumentCondition(vertexCount > 2,
			                         "The number of vertices must be higher than 2");

			var result = new WKSPointZ[vertexCount];

			for (var i = 0; i < vertexCount; i++)
			{
				var point = new WKSPointZ();

				double radians = 2 * Math.PI * i / vertexCount + startRadians;

				point.X = centre.X + radius * Math.Cos(radians);
				point.Y = centre.Y + radius * Math.Sin(radians);
				point.Z = centre.Z;

				result[i] = point;
			}

			return result;
		}

		/// <summary>
		/// Ensures that a given geometry conforms to a given spatial reference.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <param name="dontComparePrecision">if set to <c>true</c> sref precision is not compared.</param>
		/// <param name="useProjectEx">
		///   if set to <c>true</c> project with transformation if needed (IGeometry2.ProjectEx()).
		///   if set to <c>false</c> project without transformation if needed (IGeometry.Project()).</param>
		/// <returns></returns>
		/// <remarks>If the spatial reference is not as expected, the input geometry is
		/// modified by reprojection to the expected spatial reference.</remarks>
		public static bool EnsureSpatialReference(
			[NotNull] IGeometry geometry,
			[CanBeNull] ISpatialReference spatialReference,
			bool dontComparePrecision = false,
			bool useProjectEx = false)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			// spatialReference may be null (no projection will occur)

			if (spatialReference == null)
			{
				return false;
			}

			bool comparePrecisionAndTolerance = ! dontComparePrecision;
			if (SpatialReferenceUtils.AreEqual(geometry.SpatialReference,
			                                   spatialReference,
			                                   comparePrecisionAndTolerance,
			                                   false))
			{
				return false;
			}

			if (useProjectEx)
			{
				SpatialReferenceUtils.ProjectEx(geometry, spatialReference, noNewInstance: true);
			}
			else
			{
				geometry.Project(spatialReference);
			}

			return true;
		}

		public static bool EnsureSpatialReference([NotNull] IGeometry geometry,
		                                          [NotNull] IFeatureClass
			                                          targetFeatureClass)
		{
			ISpatialReference sref = ((IGeoDataset) targetFeatureClass).SpatialReference;
			return EnsureSpatialReference(geometry, sref);
		}

		public static bool EnsureSpatialReference([NotNull] IGeometry geometry,
		                                          [NotNull] IFeature targetFeature)
		{
			return EnsureSpatialReference(geometry, (IFeatureClass) targetFeature.Class);
		}

		/// <summary>
		/// Ensures that a given geometry conforms to a given spatial reference. The result
		/// is returned as an out parameter, which is either the original geometry if it 
		/// already is in the correct spatial reference, or a projected <b>copy</b> if 
		/// reprojection was applied.
		/// </summary>
		/// <param name="immutableGeometry">The geometry.</param>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <returns><c>true</c> if the output is a projected copy, <c>false</c> if 
		/// the output is the same instance as the input, which was already in the expected 
		/// spatial reference.</returns>
		/// <param name="projectedGeometry">geometry in correct spatial reference.</param>
		public static bool EnsureSpatialReference<T>(
			[NotNull] T immutableGeometry,
			[CanBeNull] ISpatialReference spatialReference,
			out T projectedGeometry)
			where T : IGeometry
		{
			const bool dontComparePrecision = false;
			return EnsureSpatialReference(immutableGeometry, spatialReference,
			                              dontComparePrecision, out projectedGeometry);
		}

		/// <summary>
		/// Ensures that a given geometry conforms to a given spatial reference. The result
		/// is returned as an out parameter, which is either the original geometry if it
		/// already is in the correct spatial reference, or a projected <b>copy</b> if
		/// reprojection was applied.
		/// </summary>
		/// <param name="immutableGeometry">The geometry.</param>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <param name="dontComparePrecision">if set to <c>true</c> sref precision is not compared.</param>
		/// <param name="projectedGeometry">geometry in correct spatial reference.</param>
		/// <returns>
		/// 	<c>true</c> if the output is a projected copy, <c>false</c> if
		/// the output is the same instance as the input, which was already in the expected
		/// spatial reference or had no assigned spatial reference.
		/// </returns>
		/// <remarks>If the input geometry has no spatial reference assigned, it is 
		/// assumed that no reprojection is needed, and <c>false</c> is returned.</remarks>
		public static bool EnsureSpatialReference<T>(
			[NotNull] T immutableGeometry,
			[CanBeNull] ISpatialReference spatialReference,
			bool dontComparePrecision,
			[NotNull] out T projectedGeometry)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(immutableGeometry, nameof(immutableGeometry));

			if (spatialReference == null)
			{
				projectedGeometry = immutableGeometry;
				return false;
			}

			if (SpatialReferenceUtils.AreEqual(immutableGeometry.SpatialReference,
			                                   spatialReference,
			                                   ! dontComparePrecision,
			                                   false))
			{
				projectedGeometry = immutableGeometry;
				return false;
			}

			T clone = GeometryFactory.Clone(immutableGeometry);
			clone.Project(spatialReference);

			projectedGeometry = clone;
			return true;

			// TODO consider (from ContainerTest)
			//if (spatialReference == null)
			//{
			//    projectedGeometry = immutableGeometry;
			//    return false;
			//}
			//if (immutableGeometry.SpatialReference == null)
			//{
			//    projectedGeometry = (T)((IClone)immutableGeometry).Clone();
			//    projectedGeometry.SpatialReference = spatialReference;
			//    return true;
			//}
			//else if (((IClone)immutableGeometry.SpatialReference).IsEqual(
			//    (IClone)spatialReference))
			//{
			//    projectedGeometry = immutableGeometry;
			//    return false;
			//}

			//projectedGeometry = (T)((IClone)immutableGeometry).Clone();
			//projectedGeometry.Project(spatialReference);

			//return true;
		}

		public static bool EnsureSpatialReference<T>(
			[NotNull] T immutableGeometry,
			[NotNull] IFeatureClass targetFeatureClass,
			out T projectedGeometry)
			where T : IGeometry
		{
			const bool dontComparePrecision = false;
			return EnsureSpatialReference(immutableGeometry,
			                              targetFeatureClass, dontComparePrecision,
			                              out projectedGeometry);
		}

		public static bool EnsureSpatialReference<T>(
			[NotNull] T immutableGeometry,
			[NotNull] IFeatureClass targetFeatureClass,
			bool dontComparePrecision,
			out T projectedGeometry)
			where T : IGeometry
		{
			ISpatialReference sref = ((IGeoDataset) targetFeatureClass).SpatialReference;

			return EnsureSpatialReference(immutableGeometry, sref, dontComparePrecision,
			                              out projectedGeometry);
		}

		public static bool EnsureSpatialReference<T>(T immutableGeometry,
		                                             IFeature targetFeature,
		                                             out T projectedGeometry)
			where T : IGeometry
		{
			const bool dontComparePrecision = false;
			return EnsureSpatialReference(immutableGeometry, targetFeature,
			                              dontComparePrecision, out projectedGeometry);
		}

		public static bool EnsureSpatialReference<T>(T immutableGeometry,
		                                             IFeature targetFeature,
		                                             bool dontComparePrecision,
		                                             out T projectedGeometry)
			where T : IGeometry
		{
			return EnsureSpatialReference(immutableGeometry,
			                              (IFeatureClass) targetFeature.Class,
			                              dontComparePrecision,
			                              out projectedGeometry);
		}

		/// <summary>
		/// Ensures that the geometry is has the same Z-aware and M-aware properties as the
		/// <paramref name="targetGeometryDef"/>. The result is returned as an out parameter
		/// which is either a copy if the input geometry did not conform with the Z- or 
		/// M-awareness of the <paramref name="targetGeometryDef"/> or the input geometry
		/// if they are already the same. If the geometry has been made Z/M-aware and there 
		/// were null (non-simple) Z values they are interpolated from other values if possible,
		/// otherwise they are set to 0. If there are null M-values they are replaced with 0.
		/// </summary>
		/// <param name="immutableGeometry">The geometry that should conform to the target GeometryDef.</param>
		/// <param name="targetGeometryDef">The GeometryDef that defines if the geometry should 
		/// have Z and / or M.</param>
		/// <param name="awareGeometry">The output geometry which conforms to the GeometryDef.</param>
		/// <returns>
		/// <c>true</c> if the output is an adapted copy, <c>false</c> if the output is the 
		/// same instance as the input, which was already in the expected form.
		/// </returns>
		public static bool EnsureSchemaZM<T>([NotNull] T immutableGeometry,
		                                     [NotNull] IGeometryDef targetGeometryDef,
		                                     out T awareGeometry)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(immutableGeometry, nameof(immutableGeometry));

			return EnsureSchemaZM(immutableGeometry,
			                      targetGeometryDef.HasZ,
			                      targetGeometryDef.HasM,
			                      out awareGeometry);
		}

		public static void EnsureSchemaZM<T>([NotNull] T geometry,
		                                     bool schemaHasZ,
		                                     bool schemaHasM)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			EnsureSchemaZ(geometry, schemaHasZ);

			EnsureSchemaM(geometry, schemaHasM);
		}

		public static bool EnsureSchemaZM<T>([NotNull] T immutableGeometry,
		                                     bool schemaHasZ,
		                                     bool schemaHasM,
		                                     [NotNull] out T awareGeometry)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(immutableGeometry, nameof(immutableGeometry));

			IGeometry zAwareGeometry;
			bool geometryChanged = EnsureSchemaZ(immutableGeometry, schemaHasZ,
			                                     out zAwareGeometry);

			IGeometry zmAwareGeometry;
			if (geometryChanged)
			{
				// no need for further copy
				zmAwareGeometry = zAwareGeometry;
				EnsureSchemaM(zAwareGeometry, schemaHasM);
			}
			else
			{
				// not copied yet, copy on m assignment if required
				geometryChanged = EnsureSchemaM(zAwareGeometry, schemaHasM,
				                                out zmAwareGeometry);
			}

			awareGeometry = (T) zmAwareGeometry;
			return geometryChanged;
		}

		/// <summary>
		/// Ensures that the Z-awareness of the provided geometry is as specified.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="immutableGeometry">The immutable geometry.</param>
		/// <param name="schemaHasZ">if set to <c>true</c> the target schema is z aware.</param>
		/// <param name="awareGeometry">geometry in correct z-awareness</param>
		/// <returns>
		/// Whether the geometry had to be adapted. If
		/// <c>true</c> if the output is a copy, <c>false</c> if the output is
		/// the same instance as the input which already had the correct z-awareness.
		/// </returns>
		public static bool EnsureSchemaZ<T>([NotNull] T immutableGeometry,
		                                    bool schemaHasZ,
		                                    [NotNull] out T awareGeometry)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(immutableGeometry, nameof(immutableGeometry));

			// TODO: consider double.NaN as default for defaultZ
			return EnsureSchemaZ(immutableGeometry, schemaHasZ, 0d, out awareGeometry);
		}

		/// <summary>
		/// Ensures that the Z-awareness of the provided geometry is as specified.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="schemaHasZ"></param>
		/// <param name="defaultZ"></param>
		public static void EnsureSchemaZ([NotNull] IGeometry geometry,
		                                 bool schemaHasZ,
		                                 double defaultZ = 0)
		{
			var zAware = (IZAware) geometry;

			if (zAware.ZAware && ! schemaHasZ)
			{
				// Important to make sure simplify cleans up duplicate segments
				// must be done when still Z-aware otherwise DropZs() has no effect
				zAware.DropZs();
			}

			zAware.ZAware = schemaHasZ;

			if (! zAware.ZAware || zAware.ZSimple)
			{
				return;
			}

			SimplifyZ(geometry, defaultZ);
		}

		public static void EnsureSchemaM<T>([NotNull] T geometry, bool schemaHasM)
			where T : IGeometry
		{
			var mAware = (IMAware) geometry;

			mAware.MAware = schemaHasM;

			if (! mAware.MAware || mAware.MSimple)
			{
				return;
			}

			// TODO revise - why not keep as NaN (which is valid for M values?)
			_msg.VerboseDebug(() => "Setting missing M values to 0");
			ReplaceUndefinedMValuesNoCopy(geometry, 0);
		}

		/// <summary>
		/// Ensures that the geometry conforms to the schema of the feature with regard to 
		/// Z- and M-awareness.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="feature"></param>
		/// <param name="awareGeometry"></param>
		/// <returns></returns>
		public static bool EnsureSchemaZM<T>([NotNull] T geometry,
		                                     [NotNull] IFeature feature,
		                                     out T awareGeometry)
			where T : IGeometry
		{
			IGeometryDef geometryDef = DatasetUtils.GetGeometryDef(feature);

			return EnsureSchemaZM(geometry, geometryDef, out awareGeometry);
		}

		/// <summary>
		/// Tries to convert the specified geometry to a geometry that can be used by the specified target feature.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="targetFeature"></param>
		/// <param name="convertedGeometry">The converted geometry or, if no conversion was needed, a copy of the input geometry.</param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		public static bool TryConvertGeometry(
			[NotNull] IGeometry geometry,
			[NotNull] IFeature targetFeature,
			out IGeometry convertedGeometry,
			[CanBeNull] NotificationCollection notifications)
		{
			return TryConvertGeometry(geometry, (IFeatureClass) targetFeature.Class,
			                          out convertedGeometry, null);
		}

		/// <summary>
		/// Tries to convert the specified geometry to a geometry that can be used by the specified target feature. The conversion
		/// includes a simplify operation on the converted geometry.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="targetFeatureClass"></param>
		/// <param name="convertedGeometry">The converted geometry or, if no conversion was needed, a copy of the input geometry.</param>
		/// <param name="notifications"></param>
		/// <returns></returns>
		public static bool TryConvertGeometry(
			[NotNull] IGeometry geometry,
			[NotNull] IFeatureClass targetFeatureClass,
			out IGeometry convertedGeometry,
			[CanBeNull] NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(targetFeatureClass, nameof(targetFeatureClass));

			esriGeometryType targetGeometryType = targetFeatureClass.ShapeType;

			bool canConvert = TryConvertGeometry(
				geometry, targetGeometryType, notifications, out convertedGeometry);

			if (canConvert)
			{
				canConvert = TryPrepareForStoring(ref convertedGeometry, targetFeatureClass,
				                                  notifications);
			}

			return canConvert;
		}

		/// <summary>
		/// Tries to convert the specified geometry to the target geometry type.
		/// Currently supported conversions:
		/// - Polygon -> Polyline
		/// - Polyline, Multipatch -> Polygon
		/// - Polygon, Polyline -> Multipatch
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="targetGeometryType"></param>
		/// <param name="convertedGeometry">The converted geometry or, if no conversion was needed, a copy of the input geometry.</param>
		/// <returns></returns>
		public static bool TryConvertGeometry([NotNull] IGeometry geometry,
		                                      esriGeometryType targetGeometryType,
		                                      out IGeometry convertedGeometry)
		{
			return TryConvertGeometry(geometry, targetGeometryType, null, out convertedGeometry);
		}

		/// <summary>
		/// Tries to convert the specified geometry to the target geometry type.
		/// Currently supported conversions:
		/// - Polygon -> Polyline
		/// - Polyline, Multipatch -> Polygon
		/// - Polygon, Polyline -> Multipatch
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="targetGeometryType"></param>
		/// <param name="notifications"></param>
		/// <param name="convertedGeometry">The converted geometry or, if no conversion was needed, a copy of the input geometry.</param>
		/// <returns></returns>
		public static bool TryConvertGeometry(
			[NotNull] IGeometry geometry,
			esriGeometryType targetGeometryType,
			[CanBeNull] NotificationCollection notifications,
			out IGeometry convertedGeometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			// TODO: more conversions as needed (e.g. to multipoint)

			convertedGeometry = null;

			if (geometry.IsEmpty)
			{
				NotificationUtils.Add(notifications, "Source geometry is empty");
				return false;
			}

			if (targetGeometryType == geometry.GeometryType)
			{
				convertedGeometry = GeometryFactory.Clone(geometry);
			}
			else
			{
				switch (targetGeometryType)
				{
					case esriGeometryType.esriGeometryPolyline:
						var sourcePoly = geometry as IPolygon;

						if (sourcePoly != null)
						{
							// careful if the target is an edge feature -> no cirles allowed, no multiparts allowed
							convertedGeometry = GeometryFactory.CreatePolyline(sourcePoly);
						}

						var sourceMultipatch = geometry as IMultiPatch;

						if (sourceMultipatch != null)
						{
							// Line salad, must be converted to proper line by subsequnt simplify.
							// Use case: Vertical facades modelled as multipatch to 2D lines.
							convertedGeometry = GeometryFactory.CreatePolyline(sourceMultipatch);
						}

						if (geometry is IMultipoint)
						{
							convertedGeometry = GeometryFactory.CreateEmptyPolyline(geometry);

							((IPointCollection) convertedGeometry).AddPointCollection(
								(IPointCollection) geometry);
						}

						break;
					case esriGeometryType.esriGeometryPolygon:
						var multipatch = geometry as IMultiPatch;

						if (multipatch != null)
						{
							convertedGeometry = GeometryFactory.CreatePolygon(multipatch);
						}

						var polyline = geometry as IPolyline;

						if (polyline != null)
						{
							convertedGeometry = GeometryFactory.CreatePolygon(polyline);
						}

						break;
					case esriGeometryType.esriGeometryMultiPatch:
						var sourcePolygon = geometry as IPolygon;

						if (sourcePolygon == null && geometry is IPolyline)
						{
							sourcePolygon = GeometryFactory.CreatePolygon(geometry);

							// ensure ring orientation:
							Simplify(sourcePolygon);
						}

						if (sourcePolygon != null)
						{
							// Non-linear segments are lost when added to a multipatch. Only the points are added
							// -> They should be linearized before the conversion
							if (HasNonLinearSegments(sourcePolygon))
							{
								NotificationUtils.Add(notifications,
								                      "Source geometry has non-linear segments, which is not allowed for multipatch geometries");
							}
							else if (! IsZAware(sourcePolygon))
							{
								// multipatches must have z
								NotificationUtils.Add(notifications,
								                      "Source geometry is not z-aware which is required for multipatch geometries");
							}
							else
							{
								convertedGeometry = GeometryFactory.CreateMultiPatch(sourcePolygon);
							}
						}

						break;
					case esriGeometryType.esriGeometryMultipoint:
						var sourcePoint = geometry as IPoint;

						if (sourcePoint != null)
						{
							convertedGeometry = GeometryFactory.CreateMultipoint(sourcePoint);
						}

						var sourcePointCollection = geometry as IPointCollection;

						if (sourcePointCollection != null)
						{
							convertedGeometry =
								GeometryFactory.CreateMultipoint(sourcePointCollection);
						}

						break;
				}
			}

			if (convertedGeometry == null)
			{
				NotificationUtils.Add(notifications,
				                      "Cannot convert from {0} to {1}",
				                      Format(geometry.GeometryType),
				                      Format(targetGeometryType));
				return false;
			}

			return true;
		}

		private static bool TryPrepareForStoring(
			[NotNull] ref IGeometry geometry,
			[NotNull] IFeatureClass targetFeatureClass,
			[CanBeNull] NotificationCollection notifications)
		{
			var canStore = true;

			if (EnsureSchemaZM(geometry, DatasetUtils.GetGeometryDef(targetFeatureClass),
			                   out geometry))
			{
				_msg.DebugFormat("TryPrepareForStoring: Z/M awareness was adapted to suit {0}.",
				                 ((IDataset) targetFeatureClass).Name);
			}

			if (EnsureSpatialReference(geometry, targetFeatureClass))
			{
				_msg.DebugFormat("TryPrepareForStoring: Geometry was projected to store in {0}.",
				                 ((IDataset) targetFeatureClass).Name);
			}

			// TODO: consider alternative that stores non-simple geometries but exactly as the input
			var points = geometry as IPointCollection;
			int? originalPointCount = points?.PointCount;

			const bool allowReorder = true;
			bool allowPathSplit =
				targetFeatureClass.FeatureType != esriFeatureType.esriFTSimpleEdge &&
				targetFeatureClass.FeatureType != esriFeatureType.esriFTComplexEdge;

			Simplify(geometry, allowReorder, allowPathSplit);

			if (geometry.IsEmpty)
			{
				NotificationUtils.Add(notifications, "Simplified geometry is empty");
				return false;
			}

			if (originalPointCount != null && originalPointCount > points.PointCount)
			{
				NotificationUtils.Add(notifications,
				                      "The simple form of the geometry is different from the input ({0} points removed). The input might have been non-simple",
				                      originalPointCount - points.PointCount);
			}

			if (targetFeatureClass.FeatureType == esriFeatureType.esriFTSimpleEdge ||
			    targetFeatureClass.FeatureType == esriFeatureType.esriFTComplexEdge)
			{
				// -> no multiparts, no loops can be stored
				// TODO: make sure same rules account for complex edges as well!

				if (((IGeometryCollection) geometry).GeometryCount > 1)
				{
					NotificationUtils.Add(notifications,
					                      "Result geometry is multipart which is not supported by geometric network edges");

					canStore = false;
				}

				if (((IPolyline) geometry).IsClosed)
				{
					NotificationUtils.Add(notifications,
					                      "Result geometry is a closed polyline which is not supported by geometric network edges");

					canStore = false;
				}
			}

			return canStore;
		}

		[NotNull]
		public static T AssignZ<T>([NotNull] T geometry, [NotNull] ISurface surface,
		                           bool useDefaultZ = false,
		                           double defaultZ = double.NaN,
		                           bool drape = false,
		                           double stepSize = -1,
		                           [CanBeNull] IDrapeZ drapeService = null,
		                           double drapeTolerance = -1)
			where T : IGeometry
		{
			const double domainEpsilon = 0.11111;
			return AssignZ(geometry, surface,
			               useDefaultZ, defaultZ, drape, stepSize,
			               drapeService, drapeTolerance, domainEpsilon);
		}

		[NotNull]
		public static T AssignZ<T>([NotNull] T geometry,
		                           [NotNull] ISurface surface,
		                           bool useDefaultZ,
		                           double defaultZ,
		                           double domainEpsilon)
			where T : IGeometry
		{
			return AssignZ(geometry, surface, useDefaultZ, defaultZ, false,
			               double.NaN, null, double.NaN, domainEpsilon);
		}

		[NotNull]
		public static T AssignZ<T>([NotNull] T geometry,
		                           [NotNull] ISurface surface,
		                           bool useDefaultZ,
		                           double defaultZ,
		                           bool drape,
		                           double stepSize,
		                           [CanBeNull] IDrapeZ drapeService,
		                           double drapeTolerance,
		                           double domainEpsilon)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(surface, nameof(surface));
			// drapeService may be null -> no draping

			// TODO: indicate if default value was applied
			// TODO: when unioning parts, give z value from surface precedence

			IGeometry result;

			IPolygon surfaceDomain = surface.Domain;

			if (surfaceDomain == null)
			{
				// no surface
				IGeometry clone = GeometryFactory.Clone(geometry);

				if (useDefaultZ)
				{
					// TODO: simplify not needed in here
					ReplaceUndefinedZValues(clone, defaultZ);
				}

				result = clone;
			}
			else if (Contains(surfaceDomain, geometry))
			{
				// the geometry is fully within the domain of the surface

				try
				{
					result = Interpolate(geometry, surface, drape, stepSize,
					                     drapeService, drapeTolerance);
				}
				catch (Exception e)
				{
					_msg.Debug(
						"Error interpolating geometry, trying again as PartlyOutside", e);

					// this is done to avoid the more expensive ContainsWithoutTouchingBoundary test and applying 
					// the epsilon buffering always.
					// The situation where this exception may happen is very rare, is not worth
					// the expensive test being done every time.

					IGeometry projectedGeometry;
					EnsureSpatialReference(geometry,
					                       surfaceDomain.SpatialReference,
					                       out projectedGeometry);

					result = AssignZPartlyOutsideDomain(geometry, surface, useDefaultZ,
					                                    defaultZ, drape, stepSize,
					                                    drapeService,
					                                    drapeTolerance, projectedGeometry,
					                                    domainEpsilon);
				}
			}
			else
			{
				// the geometry is partially or fully outside

				ISpatialReference inputSpatialReference = geometry.SpatialReference;

				IGeometry projectedGeometry;
				bool projected = EnsureSpatialReference(geometry,
				                                        surfaceDomain.SpatialReference,
				                                        out projectedGeometry);

				bool cloned;

				if (projected)
				{
					cloned = true;
					EnsureSimple(projectedGeometry);
				}
				else
				{
					IGeometry simpleGeometry;
					cloned = EnsureSimple(projectedGeometry, out simpleGeometry);

					projectedGeometry = simpleGeometry;
				}

				if (Disjoint(surfaceDomain, projectedGeometry))
				{
					// the geometry is fully outside the domain
					_msg.Debug(
						"Geometry to assign z to is fully outside the surface domain");

					// proceed with clone
					IGeometry clone;
					if (cloned)
					{
						clone = projectedGeometry;
					}
					else
					{
						clone = GeometryFactory.Clone(projectedGeometry);
					}

					if (useDefaultZ)
					{
						ReplaceUndefinedZValues(clone, defaultZ);
					}

					result = clone;
				}
				else
				{
					// the geometry is partially outside the domain
					_msg.Debug(
						"Geometry to assign z to is partly outside the surface domain");

					result = AssignZPartlyOutsideDomain(geometry, surface, useDefaultZ,
					                                    defaultZ, drape, stepSize,
					                                    drapeService,
					                                    drapeTolerance, projectedGeometry,
					                                    domainEpsilon);
				}

				if (projected)
				{
					EnsureSpatialReference(result, inputSpatialReference);
				}
			}

			Simplify(result);

			Assert.True(IsZAware(result), "Error: result is not z aware");
			Assert.NotNull(result);

			return (T) result;
		}

		[NotNull]
		private static IGeometry AssignZPartlyOutsideDomain<T>(
			[NotNull] T geometry,
			[NotNull] ISurface surface,
			bool useDefaultZ,
			double defaultZ,
			bool drape,
			double stepSize,
			[CanBeNull] IDrapeZ drapeService,
			double drapeTolerance,
			[NotNull] IGeometry projectedGeometry,
			double domainEpsilon)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(surface, nameof(surface));
			Assert.ArgumentNotNull(projectedGeometry, nameof(projectedGeometry));
			Assert.True(domainEpsilon > 0, "domainEpsilon must be a positive number");

			const double expansionDistance = 1;
			IPolygon relevantDomain = GetRelevantSurfaceDomain(
				surface, projectedGeometry, expansionDistance);

			IPolygon bufferedDomain = GeometryFactory.CreateBuffer(relevantDomain,
				-domainEpsilon);

			Simplify(bufferedDomain);
			AllowIndexing(bufferedDomain);

			// geometry partially intersects domain
			IGeometry inside =
				((ITopologicalOperator) projectedGeometry).Intersect(
					bufferedDomain, projectedGeometry.Dimension);

			// Null spatial reference would cause problems when draping
			EnsureSpatialReference(inside, geometry.SpatialReference);

			// normally interpolate the inside part
			IGeometry insideWithZ = Interpolate(inside, surface,
			                                    drape, stepSize, drapeService,
			                                    drapeTolerance);

			// create outside part, by subtracting the inside part from the whole
			IGeometry outside =
				((ITopologicalOperator) projectedGeometry).Difference(inside);

			// Null spatial reference might cause problems when unioning
			EnsureSpatialReference(outside, geometry.SpatialReference);

			// IGeometry mergedResult = ((ITopologicalOperator) outside).Union(insideWithZ);
			IGeometry mergedResult = ((ITopologicalOperator) insideWithZ).Union(outside);
			Simplify(mergedResult);

			// always extrapolate if possible - only if NO z values exist at all the default may be applied
			if (HasAnyZValues(mergedResult))
			{
				((IZ) mergedResult).CalculateNonSimpleZs();
			}

			// remove introduced points on the cut line
			RemoveCutPointsService.RemoveBorderPoints(
				projectedGeometry, mergedResult, bufferedDomain);

			if (useDefaultZ)
			{
				ReplaceUndefinedZValues(mergedResult, defaultZ);
			}

			return mergedResult;
		}

		[NotNull]
		private static IPolygon GetRelevantSurfaceDomain(
			[NotNull] ISurface surface,
			[NotNull] IGeometry projectedGeometry,
			double expansionDistance)
		{
			Assert.ArgumentNotNull(surface, nameof(surface));
			Assert.ArgumentNotNull(projectedGeometry, nameof(projectedGeometry));

			IEnvelope geometryExtent = projectedGeometry.Envelope;

			geometryExtent.Expand(expansionDistance, expansionDistance, false);
			IPolygon extentPoly = GeometryFactory.CreatePolygon(geometryExtent);

			return (IPolygon) ((ITopologicalOperator) extentPoly).Intersect(
				surface.Domain,
				esriGeometryDimension.esriGeometry2Dimension);
		}

		public static bool IsMAware([NotNull] IGeometry geometry)
		{
			var mAware = geometry as IMAware;

			return mAware != null && mAware.MAware;
		}

		public static bool IsPointIDAware([NotNull] IGeometry geometry)
		{
			var pointIDAware = geometry as IPointIDAware;

			return pointIDAware != null && pointIDAware.PointIDAware;
		}

		public static bool IsZAware([NotNull] IGeometry geometry)
		{
			var zAware = geometry as IZAware;

			return zAware != null && zAware.ZAware;
		}

		public static void MakeZAware([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var zAware = (IZAware) geometry;
			if (! zAware.ZAware)
			{
				zAware.ZAware = true;
			}
		}

		public static void MakeNonZAware([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			if (HasAnyZValues(geometry))
			{
				ApplyConstantZ(geometry, double.NaN);
			}

			var zAware = (IZAware) geometry;

			if (zAware.ZAware)
			{
				zAware.ZAware = false;
			}
		}

		public static void MakeMAware([NotNull] IGeometry geometry, bool aware = true)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var mAware = (IMAware) geometry;
			if (! (mAware.MAware == aware))
			{
				mAware.MAware = aware;
			}
		}

		public static void MakePointIDAware([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var pointIDAware = (IPointIDAware) geometry;
			if (! pointIDAware.PointIDAware)
			{
				pointIDAware.PointIDAware = true;
			}
		}

		public static void AssignConstantPointID([NotNull] IPointCollection points,
		                                         int newID)
		{
			Assert.ArgumentNotNull(points, nameof(points));

			// NOTE: IEnumVertex.put_ID does not work for multipatches

			int pointCount = points.PointCount;

			for (var i = 0; i < pointCount; i++)
			{
				IPoint changePoint = points.Point[i];
				changePoint.ID = newID;
				points.UpdatePoint(i, changePoint);
			}
		}

		public static void AssignConstantPointID(
			[NotNull] IGeometryCollection highLevelGeometry,
			int geometryPartIndex,
			int newID)
		{
			var points = (IPointCollection) highLevelGeometry;

			int globalStartIndex = GetGlobalIndex((IGeometry) highLevelGeometry,
			                                      geometryPartIndex, 0);

			IGeometry part = highLevelGeometry.Geometry[geometryPartIndex];

			var partPoints = part as IPointCollection;

			if (partPoints == null)
			{
				IPoint changePoint = points.Point[geometryPartIndex];
				changePoint.ID = newID;
				points.UpdatePoint(geometryPartIndex, changePoint);
			}
			else
			{
				int vertexCount = ((IPointCollection) part).PointCount;

				for (int i = globalStartIndex; i < globalStartIndex + vertexCount; i++)
				{
					IPoint changePoint = points.Point[i];
					changePoint.ID = newID;
					points.UpdatePoint(i, changePoint);
				}
			}
		}

		public static bool HasUniqueVertexId([NotNull] IGeometry geometry,
		                                     out int vertexId)
		{
			var singlePoint = geometry as IPoint;

			if (singlePoint != null)
			{
				vertexId = singlePoint.ID;

				return true;
			}

			vertexId = 0;
			var pointCollection = (IPointCollection) geometry;

			int? idResult = null;

			IPoint testPoint = new PointClass();
			for (var i = 0; i < pointCollection.PointCount; i++)
			{
				pointCollection.QueryPoint(i, testPoint);

				int currentId = testPoint.ID;

				if (idResult == null)
				{
					idResult = currentId;
				}
				else if (idResult != currentId)
				{
					return false;
				}
			}

			if (idResult.HasValue)
			{
				vertexId = idResult.Value;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether a the Z-aware geometry has any NaN z values.
		/// NOTE: If the specified geometry has non-linear segments and was changed without calling SegmentsChanged()
		/// this method might return false despite it has NaN z values!
		/// NOTE: This method can also return false if there are no NaN z values but two identical vertices (touching
		///       rings)
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <returns>
		/// 	<c>true</c> if the specified geometry is Z aware and has at least one NaN z value; otherwise, <c>false</c>.
		/// </returns>
		public static bool HasUndefinedZValues([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var zAware = (IZAware) geometry;

			if (! zAware.ZAware)
			{
				return false;
			}

			return ! zAware.ZSimple;

			// TODO: consider this as safety net against changed non-linear Segments
			//var polycurve = geometry as IPolycurve;
			//return polycurve != null && HasNonLinearSegments(polycurve)
			//        ? HasAnyZValues(geometry, z => double.IsNaN(z))
			//        : ! zAware.ZSimple;
		}

		public static bool HasUndefinedMValues([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var mAware = (IMAware) geometry;

			if (! mAware.MAware)
			{
				return false;
			}

			return ! mAware.MSimple;
		}

		/// <summary>
		/// Determines whether the specified geometry has any non-NaN z values.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <returns>
		/// 	<c>true</c> if the specified geometry has at least one non-NaN z value; otherwise, <c>false</c>.
		/// </returns>
		public static bool HasAnyZValues([NotNull] IGeometry geometry)
		{
			return HasAnyZValues(geometry, z => ! double.IsNaN(z));
		}

		/// <summary>
		/// Determines whether the specified geometry has any z value for which the specified predicate returns true
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="predicate">The predicate for the tested Z values</param>
		/// <returns>
		/// 	<c>true</c> if the specified geometry has at least one z value for which the predicate returns true; otherwise, <c>false</c>.
		/// </returns>
		public static bool HasAnyZValues([NotNull] IGeometry geometry,
		                                 Func<double, bool> predicate)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var points = geometry as IPointCollection;

			if (points == null)
			{
				return ((IZAware) geometry).ZAware;
			}

			IEnumVertex vertices = points.EnumVertices;
			vertices.Reset();

			try
			{
				IPoint point = new PointClass();

				int partIndex;
				int vertexIndex;

				vertices.QueryNext(point, out partIndex, out vertexIndex);
				while (vertexIndex >= 0)
				{
					if (predicate(point.Z))
					{
						return true;
					}

					vertices.QueryNext(point, out partIndex, out vertexIndex);
				}

				return false;
			}
			finally
			{
				vertices.Reset();
			}
		}

		[NotNull]
		public static T ConstantZ<T>([NotNull] T geometry, double constantZ)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			IGeometry newGeometry = GeometryFactory.Clone(geometry);

			ApplyConstantZ(newGeometry, constantZ);

			return (T) newGeometry;
		}

		public static void ApplyConstantZ([NotNull] IGeometry geometry, double constantZ)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			MakeZAware(geometry);

			if (geometry is IZ)
			{
				((IZ) geometry).SetConstantZ(constantZ);
			}
			else if (geometry is IPoint)
			{
				((IPoint) geometry).Z = constantZ;
			}
			else if (geometry is IPointCollection)
			{
				IEnumVertex eVertex =
					((IPointCollection) geometry).EnumVertices;
				IPoint point = new PointClass();
				int part;
				int vertex;

				eVertex.QueryNext(point, out part, out vertex);
				while (part >= 0 && vertex >= 0)
				{
					eVertex.put_Z(constantZ);
					eVertex.QueryNext(point, out part, out vertex);
				}
			}
			else if (geometry is IEnvelope)
			{
				((IEnvelope) geometry).ZMin = constantZ;
				((IEnvelope) geometry).ZMax = constantZ;
			}
			else
			{
				throw new ArgumentException(@"The given geometry does not support the " +
				                            @"needed interfaces to apply constant z values.",
				                            nameof(geometry));
			}
		}

		[NotNull]
		public static T OffsetZ<T>([NotNull] T geometry, double offset)
			where T : IGeometry
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			IGeometry newGeometry = GeometryFactory.Clone(geometry);
			MakeZAware(newGeometry);

			if (newGeometry is IZCollection)
			{
				((IZCollection) newGeometry).OffsetZs(offset);
			}
			else if (newGeometry is IPoint)
			{
				var vertex = (IPoint) newGeometry;
				vertex.Z += offset;
			}
			else if (newGeometry is ITransform3D)
			{
				((ITransform3D) newGeometry).Move3D(0, 0, offset);
			}
			else
			{
				throw new ArgumentException(@"The given geometry does not support the " +
				                            @"needed interfaces to apply constant z values.",
				                            nameof(geometry));
			}

			return (T) newGeometry;
		}

		public static T MultipleZ<T>(T geometry, double factor) where T : IGeometry
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			IGeometry newGeometry = GeometryFactory.Clone(geometry);
			MakeZAware(newGeometry);

			if (newGeometry is IZCollection)
			{
				((IZCollection) newGeometry).MultiplyZs(factor);
			}
			else if (newGeometry is IPoint)
			{
				var vertex = (IPoint) newGeometry;
				vertex.Z *= factor;
			}
			else
			{
				throw new ArgumentException(@"The given geometry does not support the " +
				                            @"needed interfaces to apply constant z values.",
				                            nameof(geometry));
			}

			return (T) newGeometry;
		}

		[NotNull]
		public static IPolyline InterpolateZ([NotNull] IPolyline polyline)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));
			Assert.ArgumentCondition(IsZAware(polyline), "polyline is not Z aware");

			IPolyline result = GeometryFactory.Clone(polyline);

			var polyLineZ = (IZ) result;

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Interpolating Z values for polyline with envelope {0}",
				                 Format(result.Envelope));
			}

			var partIndex = 0;
			foreach (IPath path in GetPaths(result))
			{
				if (! path.IsEmpty)
				{
					int pointCount = ((IPointCollection) path).PointCount;

					if (pointCount > 2)
					{
						_msg.VerboseDebug(
							() => $"Interpolating path {partIndex} with {pointCount} points");

						polyLineZ.InterpolateZsBetween(partIndex, 0, partIndex,
						                               pointCount - 1);
					}
					else
					{
						_msg.VerboseDebug(
							() =>
								$"Path {partIndex} has insufficient point count for interpolation: {pointCount}");
					}
				}
				else
				{
					_msg.WarnFormat("Empty path found on polyline with envelope {0}",
					                Format(result.Envelope));
				}

				partIndex++;
			}

			return result;
		}

		[NotNull]
		public static T ExtrapolateZ<T>([NotNull] T geometry,
		                                [NotNull] IPolycurve source) where T : IGeometry
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(source, nameof(source));

			T result = GeometryFactory.Clone(geometry);

			UpdateZFromSource(result, source);

			return result;
		}

		public static void UpdateZFromSource([NotNull] IGeometry geometry,
		                                     [NotNull] IPolycurve source)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(source, nameof(source));

			Assert.True(((IZAware) source).ZAware,
			            "Source geometry for extrapolating is not ZAware.");

			MakeZAware(geometry);

			if (geometry.IsEmpty)
			{
				return;
			}

			var vertices = geometry as IPointCollection;
			if (vertices != null)
			{
				IPoint vertex = new PointClass();

				double searchDistance = GetSearchRadius(geometry);

				int pointCount = vertices.PointCount;
				for (var i = 0; i < pointCount; i++)
				{
					vertices.QueryPoint(i, vertex);

					double zValue = GetExtrapolatedZValue(
						vertex, source, searchDistance, out double _);

					if (! double.IsNaN(zValue))
					{
						vertex.Z = zValue;
						vertices.UpdatePoint(i, vertex);
					}
					else
					{
						_msg.VerboseDebug(() => "Z value could not be found. Keeping old value");
					}
				}
			}
			else if (geometry is IPoint point)
			{
				double zValue = GetExtrapolatedZValue(point, source, out double _);

				if (! double.IsNaN(zValue))
				{
					point.Z = zValue;
				}
				else
				{
					_msg.VerboseDebug(() => "Z value could not be found. Keeping old value");
				}
			}
			else
			{
				throw new ArgumentException("Given geometry type can not be extrapolated.");
			}
		}

		public static double GetExtrapolatedZValue([NotNull] IPoint point,
		                                           [NotNull] IGeometry source)
		{
			return GetExtrapolatedZValue(point, source, out double _);
		}

		public static double GetExtrapolatedZValue([NotNull] IPoint point,
		                                           [NotNull] IGeometry source,
		                                           out double distanceToZSource)
		{
			Assert.ArgumentNotNull(point, nameof(point));
			Assert.ArgumentNotNull(source, nameof(source));

			double searchTolerance = GetSearchRadius(source);

			return GetExtrapolatedZValue(point, source, searchTolerance,
			                             out distanceToZSource);
		}

		/// <summary>
		/// Ensures that the specified geometry's vertexes between the current position of the provided
		/// IEnumVertex2 and the provided stopVertexIndex have the same Z values as the target geometry. 
		/// The target geometry must be coincident in XY with the source geometry between the start- and 
		/// the end-vertex.
		/// </summary>
		/// <param name="mutableGeometry">The geometry to compare to the target and which shall be updated
		/// if they differ.</param>
		/// <param name="target">The target geometry for Z values.</param>
		/// <param name="enumVertex">The enum vertex of the mutable geometry. The process starts at its 
		/// current position.</param>
		/// <param name="stopAtVertexIndex">The vertex at which the process ends (this vertex is not processed
		/// any more. The vertex needs to be on the same part as the start position.</param>
		/// <returns>Whether the mutable geometry had to be updated or not to be equal to the target geometry</returns>
		/// <remarks>The indices are part-specific, don't use the global index of the geometry.</remarks>
		public static bool EnsureTargetZsInPart([NotNull] IGeometry mutableGeometry,
		                                        [NotNull] IGeometry target,
		                                        [NotNull] IEnumVertex2 enumVertex,
		                                        int stopAtVertexIndex)
		{
			Assert.ArgumentNotNull(mutableGeometry, nameof(mutableGeometry));
			Assert.ArgumentNotNull(target, nameof(target));

			var geometryChanged = false;

			int newVertexIndex;
			IPoint point = new PointClass();

			double zTolerance = GetZTolerance(mutableGeometry);

			do
			{
				enumVertex.QueryNextInPart(point, out newVertexIndex);

				double zValue = GetZValueFromGeometry(target, point);

				if (double.IsNaN(zValue))
				{
					throw new InvalidOperationException(
						string.Format(
							"The target geometry has NaN Z values or is not congurent with the source geometry at {0}|{1}.",
							point.X, point.Y));
				}

				if (zValue > point.Z + zTolerance ||
				    zValue < point.Z - zTolerance)
				{
					_msg.DebugFormat("Updating Z for vertex {0} from {1} to {2}.",
					                 newVertexIndex, point.Z, zValue);

					enumVertex.put_Z(zValue);
					geometryChanged = true;
				}
			} while (newVertexIndex != stopAtVertexIndex);

			return geometryChanged;
		}

		[NotNull]
		public static T ApplyTargetZs<T>([NotNull] T geometry,
		                                 [NotNull] IEnumerable<IGeometry> targets,
		                                 MultiTargetSubMode mode) where T : IGeometry
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(targets, nameof(targets));

			IGeometry newGeometry = GeometryFactory.Clone(geometry);
			MakeZAware(newGeometry);

			var vertices = newGeometry as IPointCollection;

			ICollection<IGeometry> targetCollection = CollectionUtils.GetCollection(targets);

			if (vertices != null)
			{
				IPoint vertex = new PointClass();

				for (var i = 0; i < vertices.PointCount; i++)
				{
					vertices.QueryPoint(i, vertex);

					double zValue = GetTargetsZValue(vertex, targetCollection, mode);
					if (! double.IsNaN(zValue))
					{
						vertex.Z = zValue;
						vertices.UpdatePoint(i, vertex);
					}
					else
					{
						_msg.Debug("Z-Value could not be found. Keeping old value");
					}
				}
			}
			else
			{
				if (newGeometry is IPoint)
				{
					var point = (IPoint) newGeometry;
					double zValue = GetTargetsZValue(point, targetCollection, mode);
					if (! double.IsNaN(zValue))
					{
						point.Z = zValue;
					}
					else
					{
						_msg.Debug("Z-Value could not be found. Keeping old value");
					}
				}
				else
				{
					throw new ArgumentException(
						"Given geometry type can not be z-adjusted to targets.");
				}
			}

			return (T) newGeometry;
		}

		public static double GetZValueFromGeometry([NotNull] IGeometry geometry,
		                                           [NotNull] IPoint sourcePoint)
		{
			double searchRadius = GetSearchRadius(geometry);

			return GetZValueFromGeometry(geometry, sourcePoint, searchRadius);
		}

		public static double GetZValueFromGeometry([NotNull] IGeometry geometry,
		                                           [NotNull] IPoint sourcePoint,
		                                           double searchRadius)
		{
			IHitTest hitTest = GetHitTest(geometry);

			IPoint hitPoint = new PointClass();
			double hitDist = 0;
			int partIndex = -1, segmentIndex = -1;
			var rightSide = false;

			if (hitTest.HitTest(sourcePoint, searchRadius,
			                    esriGeometryHitPartType.esriGeometryPartBoundary, hitPoint,
			                    ref hitDist, ref partIndex, ref segmentIndex,
			                    ref rightSide))
			{
				if (geometry is IPoint)
				{
					return ((IPoint) geometry).Z;
				}

				if (geometry is IGeometryCollection)
				{
					IGeometry partGeometry =
						((IGeometryCollection) geometry).Geometry[partIndex];

					if (partGeometry is IPoint)
					{
						return ((IPoint) partGeometry).Z;
					}

					return GetZValueFromSegment(
						hitPoint, ((ISegmentCollection) partGeometry).Segment[segmentIndex]);
				}

				// neither IPoint nor IGeometryCollection 
				throw new ArgumentException(
					string.Format("Unsupported geometry type: {0}",
					              geometry.GeometryType));
			}

			return double.NaN;
		}

		public static void ReplaceUndefinedZValues([NotNull] IGeometry geometry, double z)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.False(double.IsNaN(z), "new z value must not be NaN");

			MakeZAware(geometry);

			if (! HasUndefinedZValues(geometry))
			{
				return;
			}

			var vertices = geometry as IPointCollection;

			if (vertices != null)
			{
				IPoint vertex = new PointClass();

				for (var i = 0; i < vertices.PointCount; i++)
				{
					vertices.QueryPoint(i, vertex);

					if (! double.IsNaN(vertex.Z))
					{
						continue;
					}

					vertex.Z = z;
					vertices.UpdatePoint(i, vertex);
				}
			}
			else
			{
				var point = geometry as IPoint;

				if (point == null)
				{
					// neither IPoint nor IPointCollection implemented
					throw new ArgumentException(
						string.Format("Unsupported geometry type: {0}",
						              geometry.GeometryType));
				}

				if (double.IsNaN(point.Z))
				{
					point.Z = z;
				}
			}

			if (geometry is IGeometryCollection)
			{
				((IGeometryCollection) geometry).GeometriesChanged();
			}

			Simplify(geometry);
		}

		/// <summary>
		/// Sets a constant M value for all vertices of a given geometry.
		/// </summary>
		/// <param name="geometry">The geometry to be modified.</param>
		/// <param name="constantM">The M value to be set.</param>
		/// <returns></returns>
		[NotNull]
		public static IGeometry ConstantM([NotNull] IGeometry geometry, double constantM)
		{
			IGeometry newGeometry = GeometryFactory.Clone(geometry);
			ApplyConstantM(newGeometry, constantM);

			return newGeometry;
		}

		public static void ApplyConstantM([NotNull] IGeometry geometry, double constantM)
		{
			if (geometry is IPoint)
			{
				((IPoint) geometry).M = constantM;
			}
			else if (geometry is IPointCollection)
			{
				IEnumVertex eVertex =
					((IPointCollection) geometry).EnumVertices;
				IPoint point = new PointClass();
				int part;
				int vertex;

				eVertex.QueryNext(point, out part, out vertex);
				while (part >= 0 && vertex >= 0)
				{
					eVertex.put_M(constantM);
					eVertex.QueryNext(point, out part, out vertex);
				}
			}
			else if (geometry is IEnvelope)
			{
				((IEnvelope) geometry).MMin = constantM;
				((IEnvelope) geometry).MMax = constantM;
			}
			else
			{
				throw new ArgumentException(@"The given geometry does not support the " +
				                            @"needed interfaces to apply constant m values.",
				                            nameof(geometry));
			}
		}

		/// <summary>
		/// Sets a constant M value for all vertices of a given geometry.
		/// </summary>
		/// <param name="geometry">The geometry to be modified.</param>
		/// <param name="newMValue">The M value to be set.</param>
		/// <returns></returns>
		[NotNull]
		public static IGeometry ReplaceUndefinedMValues([NotNull] IGeometry geometry,
		                                                double newMValue)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			// TODO clone only if needed

			IGeometry newGeometry = GeometryFactory.Clone(geometry);

			ReplaceUndefinedMValuesNoCopy(newGeometry, newMValue);

			return newGeometry;
		}

		/// <summary>
		/// Note: multipatch geometries need special handling as they fails for EnumVertices methods
		/// Therefore a separate implementation is necessary
		/// </summary>
		/// <param name="multipatch"></param>
		/// <param name="newMValue"></param>
		private static void ReplaceUndefinedMValuesNoCopy([NotNull] IMultiPatch multipatch,
		                                                  double newMValue)
		{
			var vertices = (IPointCollection) multipatch;

			IPoint vertex = new PointClass();

			for (var i = 0; i < vertices.PointCount; i++)
			{
				vertices.QueryPoint(i, vertex);

				if (! double.IsNaN(vertex.M))
				{
					continue;
				}

				vertex.M = newMValue;
				vertices.UpdatePoint(i, vertex);
			}

			if (multipatch is IGeometryCollection)
			{
				((IGeometryCollection) multipatch).GeometriesChanged();
			}
		}

		private static void ReplaceUndefinedMValuesNoCopy([NotNull] IGeometry geometry,
		                                                  double newMValue)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			// NOTE: eVertex.put_M(newMValue) fails for multipatches
			// NOTE: EnumVertices fails for multipatch geometries with more than 1 part
			var multipatchGeometry = geometry as IMultiPatch;

			if (multipatchGeometry != null)
			{
				ReplaceUndefinedMValuesNoCopy(multipatchGeometry, newMValue);

				return;
			}

			var newPoint = geometry as IPoint;
			if (newPoint != null)
			{
				if (double.IsNaN(newPoint.M))
				{
					newPoint.M = newMValue;
				}
			}
			else if (geometry is IPointCollection)
			{
				IEnumVertex eVertex = ((IPointCollection) geometry).EnumVertices;
				IPoint point = new PointClass();
				int part;
				int vertex;

				eVertex.QueryNext(point, out part, out vertex);
				while (part >= 0 && vertex >= 0)
				{
					if (double.IsNaN(point.M))
					{
						eVertex.put_M(newMValue);
					}

					_msg.VerboseDebug(
						() => $"Querying next vertex. Current: <part> {part} <vertex> {vertex}");

					eVertex.QueryNext(point, out part, out vertex);
				}
			}
			else if (geometry is IEnvelope)
			{
				var envelope = (IEnvelope) geometry;
				if (double.IsNaN(envelope.MMin))
				{
					if (double.IsNaN(envelope.MMax) || newMValue < envelope.MMax)
					{
						envelope.MMin = newMValue;
					}
					else
					{
						envelope.MMin = envelope.MMax;
					}
				}

				if (double.IsNaN(envelope.MMax))
				{
					if (double.IsNaN(envelope.MMin) || newMValue > envelope.MMax)
					{
						envelope.MMax = newMValue;
					}
					else
					{
						envelope.MMax = envelope.MMin;
					}
				}
			}
			else
			{
				throw new ArgumentException(@"The given geometry does not support the " +
				                            @"needed interfaces to apply constant m values.",
				                            nameof(geometry));
			}
		}

		/// <summary>
		/// Moves the geometry according to the specified offsets. If
		/// The geometry is not Z-aware the zOffset is disregarded.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="xOffset"></param>
		/// <param name="yOffset"></param>
		/// <param name="zOffset"></param>
		public static void MoveGeometry([NotNull] IGeometry geometry,
		                                double xOffset, double yOffset, double zOffset)
		{
			if (IsZAware(geometry))
			{
				MoveGeometry3D(geometry, xOffset, yOffset, zOffset);
			}
			else
			{
				if (_msg.IsDebugEnabled && Math.Abs(zOffset) > double.Epsilon)
				{
					_msg.Debug("MoveGeometry: zOffset disregarded for non-Z-aware geometry.");
				}

				MoveGeometry(geometry, xOffset, yOffset);
			}
		}

		/// <summary>
		/// Moves the geometry according to the specified offsets.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="xOffset"></param>
		/// <param name="yOffset"></param>
		public static void MoveGeometry([NotNull] IGeometry geometry,
		                                double xOffset, double yOffset)
		{
			var transform = (ITransform2D) geometry;

			transform.Move(xOffset, yOffset);
		}

		private static void MoveGeometry3D([NotNull] IGeometry geometry, double xOffset,
		                                   double yOffset, double zOffset)
		{
			Assert.ArgumentCondition(IsZAware(geometry),
			                         "Geometry is not Z aware. Use MoveGeometry.");

			if (geometry is IMultipoint)
			{
				// ITransform3D is not implemented on MultipointClass
				IPoint updatePoint = new PointClass();
				var points = (IPointCollection) geometry;

				for (var i = 0; i < points.PointCount; i++)
				{
					points.QueryPoint(i, updatePoint);
					((ITransform3D) updatePoint).Move3D(xOffset, yOffset, zOffset);
					points.UpdatePoint(i, updatePoint);
				}
			}
			else
			{
				var transform = geometry as ITransform3D;
				if (transform != null)
				{
					transform.Move3D(xOffset, yOffset, zOffset);
				}
				else
				{
					Assert.CantReach("Moving geometry is neither IMultipoint " +
					                 "nor does it support ITransform3D.");
				}
			}
		}

		// TODO This should probably be made obsolete, as the geometry's tolerance may not be well 
		// defined (may be the minimum default from the map spatial reference!)
		public static double GetSearchRadius([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			if (geometry.SpatialReference == null)
			{
				return GetDefaultSearchRadius(geometry);
			}

			double tolerance = GetXyTolerance(geometry);

			return double.IsNaN(tolerance)
				       ? GetDefaultSearchRadius(geometry)
				       : tolerance;
		}

		public static double GetSearchRadius([NotNull] IFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			return GetXyTolerance(feature);
		}

		/// <summary>
		/// Gets the xy resolution for a geometry object, as defined by its 
		/// spatial reference.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <returns></returns>
		/// <remarks>Should not be used as tolerance for searching. 
		/// Use <see cref="GetSearchRadius(IGeometry)"/> instead.</remarks>
		public static double GetXyResolution([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			ISpatialReference sref = geometry.SpatialReference;

			Assert.NotNull(sref,
			               "The geometry has no spatial reference, unable to determine xy resolution");

			return ((ISpatialReferenceResolution) sref).XYResolution[true];
		}

		[NotNull]
		private static IList<IGeometry> ExplodePolygon([NotNull] IPolygon polygon)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			var result = new List<IGeometry>();

			if (polygon.IsEmpty)
			{
				return result;
			}

			var polygonClone = (IPolygon4) GeometryFactory.Clone(polygon);

			if (GetExteriorRingCount(polygonClone) == 1)
			{
				result.Add(polygonClone);
			}
			else
			{
				IGeometryBag singlePartGeometries;
				try
				{
					singlePartGeometries = polygonClone.ConnectedComponentBag;
				}
				catch (Exception ex)
				{
					_msg.DebugFormat(
						"Exception in IPolygon4.ConnectedComponentBag ({0}) at {1}; trying again after Simplify",
						ex.Message,
						Format(polygonClone.Envelope));

					Simplify(polygonClone, allowReorder: true);

					if (polygonClone.IsEmpty)
					{
						// Simplify() emptied the geometry --> return empty list
						return result;
					}

					try
					{
						singlePartGeometries = polygonClone.ConnectedComponentBag;
					}
					catch
					{
						_msg.DebugFormat(ToString(polygonClone));
						throw;
					}
				}

				var enumGeometry = (IEnumGeometry) singlePartGeometries;
				enumGeometry.Reset();

				IGeometry currentGeometry = enumGeometry.Next();

				while (currentGeometry != null)
				{
					EnsureSpatialReference(currentGeometry, polygon.SpatialReference);

					Simplify(currentGeometry, allowReorder: true);

					if (! currentGeometry.IsEmpty)
					{
						result.Add(currentGeometry);
					}

					currentGeometry = enumGeometry.Next();
				}
			}

			return result;
		}

		/// <summary>
		/// Gets the xy tolerance for a geometry object, as defined by its 
		/// spatial reference. If the geometry has no spatial reference
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <returns></returns>
		public static double GetXyTolerance([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			ISpatialReference sref = geometry.SpatialReference;

			Assert.NotNull(sref,
			               "The geometry has no spatial reference, " +
			               "unable to determine xy tolerance");

			return GetXyTolerance(sref);
		}

		public static double GetXyTolerance([NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			ISpatialReference sref = spatialReference;

			return ((ISpatialReferenceTolerance) sref).XYTolerance;
		}

		/// <summary>
		/// Gets the xy tolerance for a feature, as defined by its 
		/// spatial reference.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns></returns>
		public static double GetXyTolerance([NotNull] IFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			return GetXyTolerance((IGeoDataset) feature.Class);
		}

		/// <summary>
		/// Gets the xy tolerance for a feature class.
		/// </summary>
		/// <param name="featureClass">The feature class.</param>
		/// <returns></returns>
		public static double GetXyTolerance([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return GetXyTolerance((IGeoDataset) featureClass);
		}

		/// <summary>
		/// Gets the xy tolerance for a geodataset.
		/// </summary>
		/// <param name="geoDataset">The geodataset.</param>
		/// <returns></returns>
		public static double GetXyTolerance([NotNull] IGeoDataset geoDataset)
		{
			Assert.ArgumentNotNull(geoDataset, nameof(geoDataset));

			ISpatialReference spatialReference = geoDataset.SpatialReference;

			Assert.NotNull(spatialReference,
			               "The dataset has no spatial reference, unable to determine xy tolerance");

			return ((ISpatialReferenceTolerance) spatialReference).XYTolerance;
		}

		/// <summary>
		/// Gets the xy tolerance for a geodataset.
		/// </summary>
		/// <param name="geoDataset">The geodataset.</param>
		/// <param name="defaultTolerance">The default xy tolerance if there is no spatial reference.</param>
		/// <returns></returns>
		public static double GetXyTolerance([NotNull] IGeoDataset geoDataset,
		                                    double defaultTolerance)
		{
			Assert.ArgumentNotNull(geoDataset, nameof(geoDataset));

			ISpatialReference spatialReference = geoDataset.SpatialReference;

			return GetXyTolerance(spatialReference, defaultTolerance);
		}

		public static double GetXyTolerance([CanBeNull] ISpatialReference spatialReference,
		                                    double defaultTolerance)
		{
			return ((ISpatialReferenceTolerance) spatialReference)?.XYTolerance ?? defaultTolerance;
		}

		public static void SetMinimumXyTolerance([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			ISpatialReference spatialReference = geometry.SpatialReference;

			Assert.NotNull(spatialReference,
			               "The geometry has no spatial reference, unable to set the minimum tolerance.");

			var srTolerance =
				(ISpatialReferenceTolerance) ((IClone) spatialReference).Clone();

			srTolerance.SetMinimumXYTolerance();

			EnsureSpatialReference(geometry, (ISpatialReference) srTolerance);
		}

		public static esriSRToleranceEnum SetXyTolerance([NotNull] IGeometry geometry,
		                                                 double tolerance)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			ISpatialReference spatialReference = geometry.SpatialReference;

			Assert.NotNull(spatialReference,
			               "The geometry has no spatial reference, unable to set the minimum tolerance.");

			var srTolerance =
				(ISpatialReferenceTolerance) ((IClone) spatialReference).Clone();

			srTolerance.XYTolerance = tolerance;

			esriSRToleranceEnum validity = srTolerance.XYToleranceValid;

			EnsureSpatialReference(geometry, (ISpatialReference) srTolerance);

			return validity;
		}

		/// <summary>
		/// Get the Z tolerance of the given geometry.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <returns>Z tolerance or NaN if no valid Z tolerance defined</returns>
		public static double GetZTolerance([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			return GetZTolerance(geometry.SpatialReference);
		}

		/// <summary>
		/// Get the Z tolerance of the given feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>Z tolerance or NaN if no valid Z tolerance defined</returns>
		public static double GetZTolerance([NotNull] IFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			return GetZTolerance((IGeoDataset) feature.Class);
		}

		/// <summary>
		/// Get the Z tolerance of the given geodataset.
		/// </summary>
		/// <param name="geoDataset">The geodataset.</param>
		/// <returns>Z tolerance or NaN if no valid Z tolerance defined</returns>
		public static double GetZTolerance([NotNull] IGeoDataset geoDataset)
		{
			Assert.ArgumentNotNull(geoDataset, nameof(geoDataset));

			return GetZTolerance(geoDataset.SpatialReference);
		}

		/// <summary>
		/// Get the Z tolerance of the given spatial reference.
		/// </summary>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <returns>Z tolerance or NaN if no valid Z tolerance defined</returns>
		public static double GetZTolerance([NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			var srt = spatialReference as ISpatialReferenceTolerance;

			return srt?.ZToleranceValid == esriSRToleranceEnum.esriSRToleranceOK
				       ? srt.ZTolerance
				       : double.NaN;
		}

		public static esriSRToleranceEnum SetZTolerance([NotNull] IGeometry geometry,
		                                                double tolerance)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			ISpatialReference spatialReference = geometry.SpatialReference;

			Assert.NotNull(spatialReference,
			               "The geometry has no spatial reference, unable to set the minimum tolerance.");

			var srTolerance =
				(ISpatialReferenceTolerance) ((IClone) spatialReference).Clone();

			srTolerance.ZTolerance = tolerance;

			esriSRToleranceEnum validity = srTolerance.ZToleranceValid;

			// EnsureSpatialRefeerence does not compare Z-tolerance, project directly:
			geometry.Project((ISpatialReference) srTolerance);

			return validity;
		}

		/// <summary>
		/// Gets the xy resolution for a geometry object, as defined by its
		/// spatial reference.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns></returns>
		/// <remarks>Should not be used as tolerance for searching. 
		/// Use <see cref="GetSearchRadius(IFeature)"/> instead.</remarks>
		public static double GetXyResolution([NotNull] IFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			return GetXyResolution((IGeoDataset) feature.Class);
		}

		/// <summary>
		/// Gets the xy resolution of a feature class
		/// </summary>
		/// <param name="featureClass">The feature class.</param>
		/// <returns></returns>
		public static double GetXyResolution([NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return GetXyResolution((IGeoDataset) featureClass);
		}

		/// <summary>
		/// Gets the xy resolution of a geodataset.
		/// </summary>
		/// <param name="geoDataset">The geodataset.</param>
		/// <returns></returns>
		public static double GetXyResolution([NotNull] IGeoDataset geoDataset)
		{
			Assert.ArgumentNotNull(geoDataset, nameof(geoDataset));

			ISpatialReference spatialReference = geoDataset.SpatialReference;

			Assert.NotNull(spatialReference,
			               "The geometry has no spatial reference, unable to determine XY resolution");

			return SpatialReferenceUtils.GetXyResolution(spatialReference);
		}

		public static double GetZResolution([NotNull] IFeature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			IGeoDataset geoDataset = (IGeoDataset) feature.Class;

			return SpatialReferenceUtils.GetZResolution(geoDataset.SpatialReference);
		}

		/// <summary>
		/// Gets the Z resolution of a geometry in metres.
		/// </summary>
		/// <param name="geometry">The geometry</param>
		/// <returns></returns>
		public static double GetZResolution([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			ISpatialReference spatialReference = geometry.SpatialReference;
			Assert.NotNull(spatialReference,
			               "The geometry has no spatial reference, unable to determine Z resolution");

			return SpatialReferenceUtils.GetZResolution(spatialReference);
		}

		public static double GetZResolution([NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			// TODO: Use SpatialReferenceUtils, review starndard units (Unit Test)
			return ((ISpatialReferenceResolution) spatialReference).ZResolution[true];
		}

		/// <summary>
		/// Determines whether the specified polyline as a single valid part.
		/// </summary>
		/// <param name="polyline">The polyline.</param>
		/// <returns>
		/// 	<c>true</c> if the specified polyline as one single, valid part; 
		///      otherwise, <c>false</c>.
		/// </returns>
		public static bool HasSingleValidPart([CanBeNull] IPolyline polyline)
		{
			if (polyline == null)
			{
				return false;
			}

			if (polyline.IsEmpty)
			{
				return false;
			}

			if (((IGeometryCollection) polyline).GeometryCount > 1)
			{
				return false;
			}

			if (polyline.Length <= 0)
			{
				return false;
			}

			return ((IPointCollection) polyline).PointCount > 1;
		}

		/// <summary>
		/// Returns a reference to polygon with the largest area of the specified polygons.
		/// </summary>
		/// <param name="polylines"></param>
		/// <returns></returns>
		public static IGeometry GetLargestGeometry(
			[NotNull] IEnumerable<IPolyline> polylines)
		{
			return GetLargestGeometry(polylines.Cast<IPolycurve>());
		}

		/// <summary>
		/// Returns a reference to the longest polylines of the specified polylines.
		/// </summary>
		/// <param name="polygons"></param>
		/// <returns></returns>
		public static IGeometry GetLargestGeometry(
			[NotNull] IEnumerable<IPolygon> polygons)
		{
			return GetLargestGeometry(polygons.Cast<IPolycurve>());
		}

		/// <summary>
		/// Returns a reference to the largest (area for polygons, length for polylines) geometry
		/// of the given geometries.
		/// </summary>
		/// <param name="polycurves">The polycurves which must all be of the same geometry type.</param>
		/// <returns></returns>
		[CanBeNull]
		public static IGeometry GetLargestGeometry(
			[NotNull] IEnumerable<IPolycurve> polycurves)
		{
			return GetLargestGeometry(polycurves.Cast<IGeometry>());
		}

		/// <summary>
		/// Returns a reference to the largest (area for IArea objects, length for ICurve objects, 
		/// point count for multipoint objects) geometry of the given geometries. If several 
		/// geometries have the largest size, the first in the list will be returned.
		/// </summary>
		/// <param name="geometries">The geometries which must all be of the same geometry type.</param>
		/// <returns></returns>
		[CanBeNull]
		public static IGeometry GetLargestGeometry(
			[NotNull] IEnumerable<IGeometry> geometries)
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));

			double largestSize = double.MinValue;
			IGeometry result = null;
			var geometryType = esriGeometryType.esriGeometryNull;

			foreach (IGeometry geometry in geometries)
			{
				Assert.True(
					geometry is IArea || geometry is ICurve || geometry is IPointCollection,
					"GetLargestGeometry: Unsupported geometry type: {0}",
					geometry.GeometryType);

				if (geometryType == esriGeometryType.esriGeometryNull)
				{
					geometryType = geometry.GeometryType;
				}

				Assert.AreEqual(geometryType, geometry.GeometryType,
				                "GetLargestGeometry: All geometry types must be the same.");

				var area = geometry as IArea;

				double size;
				if (area != null)
				{
					size = Math.Abs(area.Area);
				}
				else
				{
					var curve = geometry as ICurve;

					size = curve?.Length ?? ((IPointCollection) geometry).PointCount;
				}

				if (size > largestSize)
				{
					largestSize = size;
					result = geometry;
				}
			}

			return result;
		}

		/// <summary>
		/// Gets the largest geometry (using IArea) from the given geometries.
		/// </summary>
		/// <param name="polygons">The polygons.</param>
		/// <returns></returns>
		public static int GetLargestPolygonIndex([NotNull] IList<IPolygon> polygons)
		{
			Assert.ArgumentNotNull(polygons, nameof(polygons));
			Assert.ArgumentCondition(polygons.Count > 0, "No polygons provided");

			var largestPolygonIndex = 0;

			if (polygons.Count == 1)
			{
				return largestPolygonIndex;
			}

			double largestArea = -1;
			var currentPolygonIndex = 0;

			foreach (IPolygon polygon in polygons)
			{
				double area = ((IArea) polygon).Area;
				if (area > largestArea)
				{
					largestArea = area;
					largestPolygonIndex = currentPolygonIndex;
				}

				currentPolygonIndex++;
			}

			return largestPolygonIndex;
		}

		/// <summary>
		/// Returns a reference to the smallest (area for IArea objects, length for ICurve objects) 
		/// geometry of the given geometries. If several  geometries have the largest size, the first 
		/// in the list will be returned.
		/// </summary>
		/// <param name="geometries">The geometries which must all be of the same geometry type.</param>
		/// <returns></returns>
		public static T GetSmallestGeometry<T>([NotNull] IEnumerable<T> geometries)
			where T : IGeometry
		{
			IGeometry smallestPart = null;
			double smallestSize = double.PositiveInfinity;

			foreach (T candidate in geometries)
			{
				double candidateSize = GetGeometrySize(candidate);

				if (candidateSize < smallestSize)
				{
					smallestPart = candidate;
					smallestSize = candidateSize;
				}
			}

			return (T) smallestPart;
		}

		/// <summary>
		/// Returns a value that indicates the size of the specified geometry:
		/// - Multipatch, Polygon, Ring: 2D area
		/// - Polyline, Path, Segment: 2D length
		/// - Multipoint: Point count
		/// - Point: 0
		/// </summary>
		/// <param name="geometry"></param>
		/// <returns></returns>
		public static double GetGeometrySize([NotNull] IGeometry geometry)
		{
			var area = geometry as IArea;

			if (area != null)
			{
				return Math.Abs(area.Area);
			}

			var curve = geometry as ICurve;

			if (curve?.Length != null)
			{
				return curve.Length;
			}

			var pointCollection = geometry as IPointCollection;

			if (pointCollection != null)
			{
				return pointCollection.PointCount;
			}

			// Assuming it's a point
			return 0;
		}

		/// <summary>
		/// Determines whether the specified polygon as a single positive area part (any number of 
		/// "hole" parts are allowed).
		/// </summary>
		/// <param name="polygon">The polygon.</param>
		/// <returns>
		/// 	<c>true</c> if the specified polygon as one single, valid positive area part; 
		///      otherwise, <c>false</c>.
		/// </returns>
		public static bool HasSingleValidPart([CanBeNull] IPolygon polygon)
		{
			return polygon != null && ! polygon.IsEmpty &&
			       HasOnlyOnePositiveAreaPart(polygon);
		}

		/// <summary>
		/// Explodes the specified geometry.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <returns>The exploded parts, as high-level geometries.</returns>
		/// <remarks>Returns a copy of the input even if it is single part.</remarks>
		[NotNull]
		public static IList<IGeometry> Explode([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			IList<IGeometry> result = new List<IGeometry>();

			var parts = geometry as IGeometryCollection;

			if (parts == null || geometry.IsEmpty || parts.GeometryCount == 1)
			{
				result.Add(GetHighLevelGeometry(GeometryFactory.Clone(geometry)));
				return result;
			}

			var polygon = geometry as IPolygon;
			if (polygon != null)
			{
				return ExplodePolygon(polygon);
			}

			int partCount = parts.GeometryCount;

			for (var index = 0; index < partCount; index++)
			{
				result.Add(GetHighLevelGeometry(parts.Geometry[index]));
			}

			return result;
		}

		/// <summary>
		/// Gets the extent of a geometry. Special treatment of IGeometryBags, 
		/// to make sure the extent of the bag is Z-aware if the bag items are.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnvelope GetExtent([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			if (! (geometry is IGeometryBag))
			{
				return geometry.Envelope;
			}

			// special treatment of geometry bags
			var geometries = (IGeometryCollection) geometry;

			return GetExtent(GetParts(geometries));
		}

		/// <summary>
		/// Gets the unioned extent of the provided geometries. If any of the provided
		/// geometries is Z-aware, the result will be Z-aware.
		/// </summary>
		/// <param name="geometries"></param>
		/// <returns></returns>
		[NotNull]
		public static IEnvelope GetExtent([NotNull] IEnumerable<IGeometry> geometries)
		{
			Assert.ArgumentNotNull(geometries, nameof(geometries));

			// special treatment of geometry bags
			IEnvelope extent = new EnvelopeClass();

			var madeZAware = false;

			foreach (IGeometry geometry in geometries)
			{
				if (! madeZAware && IsZAware(geometry))
				{
					MakeZAware(extent);
					madeZAware = true;
				}

				extent.Union(geometry.Envelope);
			}

			return extent;
		}

		public static bool IsLargerInXorY([NotNull] IEnvelope extent,
		                                  [NotNull] IEnvelope otherExtent)
		{
			return extent.Width > otherExtent.Width ||
			       extent.Height > otherExtent.Height;
		}

		public static bool HitTestWksPointZs([NotNull] IGeometry geometry,
		                                     [NotNull] IPoint queryPoint,
		                                     double xyTolerance, double zTolerance,
		                                     [NotNull] IPoint hitPoint)
		{
			return HitTestWksPointZs(geometry, queryPoint,
			                         xyTolerance, zTolerance, false, hitPoint);
		}

		public static bool HitTestWksPointZs([NotNull] IGeometry geometry,
		                                     [NotNull] IPoint queryPoint,
		                                     double xyTolerance,
		                                     double zTolerance,
		                                     bool ignoreZ,
		                                     [NotNull] IPoint hitPoint)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			Assert.ArgumentNotNull(queryPoint, nameof(queryPoint));
			Assert.ArgumentNotNull(hitPoint, nameof(hitPoint));
			Assert.True(geometry is IPointCollection4,
			            "Only geometries supporting IPointCollection4 " +
			            "can be used with HitTestWksPointZs.");

			var points = (IPointCollection4) geometry;
			var wksPoints = new WKSPointZ[points.PointCount];

			QueryWKSPointZs(points, wksPoints);

			double queryX;
			double queryY;
			double queryZ = queryPoint.Z;
			queryPoint.QueryCoords(out queryX, out queryY);

			double minDistance = double.NaN;

			foreach (WKSPointZ point in wksPoints)
			{
				double distance;
				bool isInTolerance = IsInTolerance(point, queryX, queryY, queryZ,
				                                   xyTolerance, zTolerance,
				                                   ignoreZ, out distance);

				if (! isInTolerance)
				{
					continue;
				}

				if (! double.IsNaN(minDistance) && distance >= minDistance)
				{
					continue;
				}

				minDistance = distance;

				hitPoint.PutCoords(point.X, point.Y);
				hitPoint.Z = point.Z;
			}

			return ! double.IsNaN(minDistance);
		}

		public static bool IsInTolerance(WKSPointZ p1,
		                                 double p2X, double p2Y, double p2Z,
		                                 double xyTolerance, double zTolerance,
		                                 bool ignoreZ,
		                                 out double distance)
		{
			return IsInTolerance(p1.X, p1.Y, p1.Z,
			                     p2X, p2Y, p2Z,
			                     xyTolerance, zTolerance, ignoreZ, out distance);
		}

		public static bool IsInTolerance(double x1, double y1, double z1,
		                                 double x2, double y2, double z2,
		                                 double xyTolerance, double zTolerance,
		                                 bool ignoreZ, out double distance)
		{
			double xDistance = Math.Abs(x1 - x2);
			double yDistance = Math.Abs(y1 - y2);

			double xyDistance = Math.Sqrt(xDistance * xDistance + yDistance * yDistance);

			if (ignoreZ)
			{
				distance = xyDistance;
				return xyDistance < xyTolerance;
			}

			double zDistance = Math.Abs(z1 - z2);

			distance = Math.Sqrt(xyDistance * xyDistance + zDistance * zDistance);
			return xyDistance < xyTolerance && zDistance < zTolerance;
		}

		public static bool IsSamePoint([NotNull] IPoint p1,
		                               [NotNull] IPoint p2,
		                               double xyTolerance,
		                               double zTolerance)
		{
			double p1X;
			double p1Y;
			double p2X;
			double p2Y;
			p1.QueryCoords(out p1X, out p1Y);
			p2.QueryCoords(out p2X, out p2Y);

			return IsSamePoint(p1X, p1Y, p1.Z, p2X, p2Y, p2.Z, xyTolerance, zTolerance);
		}

		public static bool IsSamePoint(WKSPointZ a, WKSPointZ b,
		                               double xyTolerance, double zTolerance)
		{
			return IsSamePoint(a.X, a.Y, a.Z, b.X, b.Y, b.Z,
			                   xyTolerance, zTolerance);
		}

		public static bool IsSamePoint(double aX, double aY, double aZ,
		                               double bX, double bY, double bZ,
		                               double xyTolerance, double zTolerance)
		{
			double dx = aX - bX;
			double dy = aY - bY;
			double dd = dx * dx + dy * dy;
			double xyToleranceSquared = xyTolerance * xyTolerance;

			if (dd <= xyToleranceSquared)
			{
				if (double.IsNaN(zTolerance))
				{
					// No Z tolerance given: ignore Z coords
					return true;
				}

				if (double.IsNaN(aZ) && double.IsNaN(bZ))
				{
					// Both Z coords are NaN:
					return true;
				}

				if (double.IsNaN(aZ) || double.IsNaN(bZ))
				{
					// One Z coord is valid, the other is NaN:
					return false;
				}

				double dz = Math.Abs(aZ - bZ);
				return dz <= zTolerance;
			}

			return false;
		}

		/// <summary>
		/// Compute the distance between the point P and the line from A to B,
		/// all projected into the XY plane. Return the square of the distance.
		/// </summary>
		public static double DistanceSquaredXY(double xP, double yP,
		                                       double xA, double yA,
		                                       double xB, double yB)
		{
			double xAB = xB - xA;
			double yAB = yB - yA;

			double xAP = xP - xA;
			double yAP = yP - yA;

			double squaredLengthAP = xAP * xAP + yAP * yAP;
			double squaredLengthAB = xAB * xAB + yAB * yAB;

			// If A = B return the distance P to A=B;
			// (don't divide by zero...)

			if (Math.Abs(squaredLengthAB) < double.Epsilon)
			{
				return squaredLengthAP;
			}

			double scalarProduct = xAP * xAB + yAP * yAB;
			double squaredProjection = scalarProduct * scalarProduct / squaredLengthAB;

			return squaredLengthAP - squaredProjection;
		}

		/// <summary>
		/// Compute the Z difference between the point P and its XY projection
		/// onto the line from A via P to B. Return the squared distance.
		/// </summary>
		public static double DistanceSquaredZ(double xP, double yP, double zP,
		                                      double xA, double yA, double zA,
		                                      double xB, double yB, double zB)
		{
			double xAP = xP - xA;
			double yAP = yP - yA;
			double lengthAP = Math.Sqrt(xAP * xAP + yAP * yAP);

			double xPB = xB - xP;
			double yPB = yB - yP;
			double lengthPB = Math.Sqrt(xPB * xPB + yPB * yPB);

			double lengthAPB = lengthAP + lengthPB;

			// Special case A = P = B (don't div by 0):

			if (Math.Abs(lengthAPB) < double.Epsilon)
			{
				double zDiff = zP - zA;
				return zDiff * zDiff;
			}

			double zPP = (lengthPB * zA + lengthAP * zB) / lengthAPB;
			double dist = zP - zPP;

			return dist * dist;
		}

		/// <summary>
		/// Gets the 3d centroid coordinates of a given envelope. If the envelope is not zaware, the returned
		/// z value is NaN.
		/// </summary>
		/// <param name="envelope">The envelope.</param>
		/// <param name="centerX">The center X.</param>
		/// <param name="centerY">The center Y.</param>
		/// <param name="centerZ">The center Z.</param>
		public static void GetCentroid3D([NotNull] IEnvelope envelope,
		                                 out double centerX,
		                                 out double centerY,
		                                 out double centerZ)
		{
			Assert.ArgumentNotNull(envelope, nameof(envelope));

			double xmin;
			double ymin;
			double xmax;
			double ymax;
			envelope.QueryCoords(out xmin, out ymin, out xmax, out ymax);

			centerX = xmin + (xmax - xmin) / 2;
			centerY = ymin + (ymax - ymin) / 2;

			if (IsZAware(envelope))
			{
				double zmin = envelope.ZMin;
				double zmax = envelope.ZMax;

				centerZ = zmin + (zmax - zmin) / 2;
			}
			else
			{
				centerZ = double.NaN;
			}
		}

		/// <summary>
		/// Gets the rings (inner and exterior) of a polygon
		/// </summary>
		/// <param name="polygon">The polygon.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IRing> GetRings([NotNull] IPolygon polygon)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			var geometryCollection = (IGeometryCollection) polygon;

			int ringCount = geometryCollection.GeometryCount;

			for (var ringIndex = 0; ringIndex < ringCount; ringIndex++)
			{
				var ring = (IRing) geometryCollection.Geometry[ringIndex];

				yield return ring;
			}
		}

		/// <summary>
		/// Gets the rings of a multipatch geometry entirely consists of rings. Other
		/// geometry parts (triangles, triangle fans/strips) result in an exception if 
		/// ignoreNonRingParts is true.
		/// </summary>
		/// <param name="ringBasedMultipatch">The multipatch geometry.</param>
		/// <param name="ignoreNonRingParts">Whether non-ring geometry parts should result
		/// in an exception or not.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IRing> GetRings([NotNull] IMultiPatch ringBasedMultipatch,
		                                          bool ignoreNonRingParts = false)
		{
			Assert.ArgumentNotNull(ringBasedMultipatch, nameof(ringBasedMultipatch));

			var geometryCollection = (IGeometryCollection) ringBasedMultipatch;

			foreach (IGeometry part in GetParts(geometryCollection))
			{
				var ring = part as IRing;

				if (ring == null)
				{
					if (ignoreNonRingParts)
					{
						continue;
					}

					throw new ArgumentException(
						$"The multipatch geometry contains a part that has geometry type {part.GeometryType}. Only rings are supported.");
				}

				yield return (IRing) part;
			}
		}

		[NotNull]
		public static IEnumerable<IGeometry> GetParts(IGeometry geometry)
		{
			var parts = geometry as IGeometryCollection;
			if (parts != null)
			{
				int count = parts.GeometryCount;
				for (int i = 0; i < count; i++)
				{
					yield return parts.Geometry[i];
				}
			}
			else if (geometry != null)
			{
				yield return geometry;
			}
		}

		[NotNull]
		public static IEnumerable<IGeometry> GetParts(
			[NotNull] IGeometryCollection geometryCollection)
		{
			Assert.ArgumentNotNull(geometryCollection, nameof(geometryCollection));

			int count = geometryCollection.GeometryCount;

			for (var index = 0; index < count; index++)
			{
				yield return geometryCollection.Geometry[index];
			}
		}

		[NotNull]
		public static IEnumerable<IPoint> GetPoints(
			[NotNull] IGeometry geometry,
			bool recycle = false)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var points = geometry as IPointCollection;
			if (points != null)
			{
				foreach (IPoint vertex in GetPoints(points, recycle))
				{
					yield return vertex;
				}
			}

			var point = geometry as IPoint;
			if (point != null)
			{
				yield return point;
			}
		}

		[NotNull]
		public static IEnumerable<IPoint> GetPoints(
			[NotNull] IPointCollection pointCollection,
			bool recycle = false)
		{
			Assert.ArgumentNotNull(pointCollection, nameof(pointCollection));

			int pointCount = pointCollection.PointCount;

			if (recycle)
			{
				var point = new PointClass();

				for (var i = 0; i < pointCount; i++)
				{
					pointCollection.QueryPoint(i, point);

					yield return point;
				}
			}
			else
			{
				for (var i = 0; i < pointCount; i++)
				{
					yield return pointCollection.Point[i];
				}
			}
		}

		/// <summary>
		/// Collects all rings in to different lists.
		/// (The returning list holds the inner rings, the
		/// out list holds the exteriorRings)
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="exteriorRings">The exterior rings.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IRing> GetRings([NotNull] IGeometry geometry,
		                                    [NotNull] out IList<IRing> exteriorRings)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var innerRings = new List<IRing>();
			exteriorRings = new List<IRing>();

			var parts = (IGeometryCollection) geometry;

			int geometryCount = parts.GeometryCount;
			for (var index = 0; index < geometryCount; index++)
			{
				IGeometry part = parts.Geometry[index];

				var ring = part as IRing;
				if (ring == null)
				{
					continue;
				}

				if (ring.IsExterior)
				{
					exteriorRings.Add(ring);
				}
				else
				{
					innerRings.Add(ring);
				}
			}

			return innerRings;
		}

		/// <summary>
		/// Gets the connected component polygons (exterior rings with their contained interior rings) 
		/// for a polygon. Make sure the input polygon is simple, otherwise this method fails.
		/// </summary>
		/// <param name="polygon">The polygon.</param>
		/// <param name="allowSimplify"></param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IPolygon> GetConnectedComponents(
			[NotNull] IPolygon polygon,
			bool allowSimplify = true)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			var poly = (IPolygon4) polygon;

			IGeometryCollection componentBag;

			try
			{
				componentBag = (IGeometryCollection) poly.ConnectedComponentBag;
			}
			catch (COMException comException)
			{
				// WORK-AROUND for COMException: The operation cannot be performed on a non-simple
				// geometry (TOP-5320). Observed for in-memory (selected) features when using
				// ConnectedComponentBag.
				_msg.DebugFormat(
					"Caught semi-expected error when accessing ConnectedComponentBag: {0}. Trying again after simplify...",
					comException.Message);

				IPolygon4 simplified = (IPolygon4) GetRingCountablePolygon(polygon, allowSimplify);

				componentBag = (IGeometryCollection) simplified.ConnectedComponentBag;
				// END WORK-AROUND
			}

			int componentCount = componentBag.GeometryCount;

			for (var index = 0; index < componentCount; index++)
			{
				var component = (IPolygon) componentBag.Geometry[index];

				yield return component;
			}
		}

		/// <summary>
		/// Gets the paths of a polyline
		/// </summary>
		/// <param name="polyline">The polyline.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IPath> GetPaths([NotNull] IPolyline polyline)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			return GetPaths((IGeometry) polyline);
		}

		/// <summary>
		/// Gets the paths of a polyline or polygon geometry.
		/// </summary>
		/// <param name="geometry">The polyline polygon.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IPath> GetPaths([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));
			esriGeometryType geometryType = geometry.GeometryType;
			Assert.ArgumentCondition(
				geometryType == esriGeometryType.esriGeometryPolygon ||
				geometryType == esriGeometryType.esriGeometryPolyline,
				"The geometry must be a polygon or a polyline");

			var geometryCollection = (IGeometryCollection) geometry;
			int partCount = geometryCollection.GeometryCount;

			for (var index = 0; index < partCount; index++)
			{
				yield return geometryCollection.Geometry[index] as IPath;
			}
		}

		/// <summary>
		/// Replaces all vertices/points of this Path, Ring, Polyline, Polygon, 
		/// Multipoint, TriangleFan, Triangles, TriangleStrip, or MultiPatch 
		/// with new ones.
		/// </summary>
		/// <param name="pointCollection"></param>
		/// <param name="fromArray"></param>
		/// <param name="count"></param>
		public static void SetWKSPointZs([NotNull] IPointCollection4 pointCollection,
		                                 [NotNull] WKSPointZ[] fromArray,
		                                 int count = -1)
		{
			if (count < 0)
			{
				count = fromArray.Length;
			}

			pointCollection.SetWKSPointZs(count, ref fromArray[0]);
		}

		/// <summary>
		/// Replaces all vertices/points of this Path, Ring, Polyline, Polygon, 
		/// Multipoint, TriangleFan, Triangles, TriangleStrip, or MultiPatch 
		/// with new ones.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="fromArray"></param>
		/// <param name="count"></param>
		public static void SetWKSPointZs([NotNull] IGeometry geometry,
		                                 [NotNull] WKSPointZ[] fromArray,
		                                 int count = -1)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var points = geometry as IPointCollection4;
			Assert.NotNull(points, "geometry does not implement IPointCollection4");

			SetWKSPointZs(points, fromArray, count);
		}

		/// <summary>
		/// Replaces all vertices/points of this Path, Ring, Polyline, Polygon, 
		/// Multipoint, TriangleFan, Triangles, TriangleStrip, or MultiPatch 
		/// with new ones.
		/// </summary>
		/// <param name="pointCollection"></param>
		/// <param name="fromArray"></param>
		/// <param name="count"></param>
		public static void SetWKSPoints([NotNull] IPointCollection4 pointCollection,
		                                [NotNull] WKSPoint[] fromArray,
		                                int count = -1)
		{
			Assert.ArgumentNotNull(pointCollection, nameof(pointCollection));

			if (count < 0)
			{
				count = fromArray.Length;
			}

			pointCollection.SetWKSPoints(count, ref fromArray[0]);
		}

		/// <summary>
		/// Replaces all vertices/points of this Path, Ring, Polyline, Polygon, 
		/// Multipoint, TriangleFan, Triangles, TriangleStrip, or MultiPatch 
		/// with new ones.
		/// </summary>
		/// <param name="geometry"></param>
		/// <param name="fromArray"></param>
		/// <param name="count"></param>
		public static void SetWKSPoints([NotNull] IGeometry geometry,
		                                [NotNull] WKSPoint[] fromArray,
		                                int count = -1)
		{
			var points = geometry as IPointCollection4;
			Assert.NotNull(points, "geometry does not implement IPointCollection4");

			SetWKSPoints(points, fromArray, count);
		}

		public static void AddWKSPointZs([NotNull] IPointCollection4 toResult,
		                                 [NotNull] WKSPointZ[] wksPointZs,
		                                 int count = -1)
		{
			if (count < 0)
			{
				count = wksPointZs.Length;
			}

			if (count > 0)
			{
				toResult.AddWKSPointZs(count, ref wksPointZs[0]);
			}
		}

		public static void QueryWKSPoints([NotNull] IPointCollection4 pointCollection,
		                                  [NotNull] WKSPoint[] resultArray,
		                                  int index = 0,
		                                  int count = -1)
		{
			Assert.ArgumentNotNull(pointCollection, nameof(pointCollection));

			if (count < 0)
			{
				count = pointCollection.PointCount;
			}

			pointCollection.QueryWKSPoints(0, count, out resultArray[0]);

			// NOTE: GeometryBridge WKSPoint methods suffer from 
			// System.Runtime.InteropServices.COMException : Typelib export:
			// Type library is not registered. (Exception from HRESULT: 0x80131165)
			// -> BUG-000091380 which is probably not going to be fixed

			// GeometryBridge.QueryWKSPoints(points, 0, ref pointArray);
		}

		public static void QueryWKSPointZs([NotNull] IPointCollection4 pointCollection,
		                                   [NotNull] WKSPointZ[] resultArray,
		                                   int index = 0,
		                                   int count = -1,
		                                   bool ensureNanZsForNonZAwarePoints = false)
		{
			Assert.ArgumentNotNull(pointCollection, nameof(pointCollection));

			if (count < 0)
			{
				count = pointCollection.PointCount;
			}

			pointCollection.QueryWKSPointZs(index, count, out resultArray[0]);

			// NOTE: GeometryBridge WKSPoint methods suffer from 
			// System.Runtime.InteropServices.COMException : Typelib export:
			// Type library is not registered. (Exception from HRESULT: 0x80131165)
			// -> BUG-000091380 which is probably not going to be fixed

			// GeometryBridge.QueryWKSPointZs(points, 0, ref pointArray);

			// NOTE: For non-Z aware geometries with NaN Z values IGeometryBridge.QueryWKSPointZs
			//		 returns either 0.0 or NaN Z values and switches randomly between the two from
			//		 one call to the next -> Setting Z values to NaN seems to help.
			if (ensureNanZsForNonZAwarePoints && ! IsZAware((IGeometry) pointCollection))
			{
				for (var i = index; i < count; i++)
				{
					resultArray[i].Z = double.NaN;
				}
			}
		}

		[NotNull]
		public static WKSPointZ[] GetWKSPointZs([NotNull] IGeometry geometry,
		                                        bool ensureNanZsForNonZAwarePoints = false)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var points = geometry as IPointCollection4;
			Assert.NotNull(points, "geometry does not implement IPointCollection4");

			int pointCount = points.PointCount;
			var pointArray = new WKSPointZ[pointCount];

			QueryWKSPointZs(points, pointArray, 0, pointCount, ensureNanZsForNonZAwarePoints);

			return pointArray;
		}

		[NotNull]
		public static WKSPoint[] GetWKSPoints([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var points = geometry as IPointCollection4;
			Assert.NotNull(points, "geometry does not implement IPointCollection4");

			return GetWKSPoints(points);
		}

		[NotNull]
		public static WKSPoint[] GetWKSPoints([NotNull] IPointCollection4 points)
		{
			Assert.ArgumentNotNull(points, nameof(points));

			int pointCount = points.PointCount;
			var result = new WKSPoint[pointCount];

			QueryWKSPoints(points, result);

			return result;
		}

		/// <summary>
		/// Assigns from and to points to an existing line. 
		/// </summary>
		/// <param name="fromPoint">From point.</param>
		/// <param name="toPoint">To point.</param>
		/// <param name="polyline">The polyline.</param>
		/// <remarks>This does not create a new polyline instance, but configures 
		/// the existing one.</remarks>
		public static void UpdateEndPoints([NotNull] IPoint fromPoint,
		                                   [NotNull] IPoint toPoint,
		                                   [NotNull] IPolyline polyline)
		{
			Assert.ArgumentNotNull(fromPoint, nameof(fromPoint));
			Assert.ArgumentNotNull(toPoint, nameof(toPoint));
			Assert.ArgumentNotNull(polyline, nameof(polyline));

			polyline.FromPoint = fromPoint;
			polyline.ToPoint = toPoint;

			polyline.SpatialReference = fromPoint.SpatialReference;
		}

		[NotNull]
		public static IEnumerable<ISegment> GetSegments([NotNull] IEnumSegment enumSegment)
		{
			const bool allowRecycling = false;
			return GetSegments(enumSegment, allowRecycling, null);
		}

		[NotNull]
		public static IEnumerable<ISegment> GetSegments(
			[NotNull] IEnumSegment enumSegment,
			bool allowRecycling,
			[CanBeNull] Predicate<ISegment> predicate = null)
		{
			Assert.ArgumentNotNull(enumSegment, nameof(enumSegment));

			bool recycling = enumSegment.IsRecycling;

			enumSegment.Reset();

			ISegment segment;
			int partIndex = -1;
			int segmentIndex = -1;
			enumSegment.Next(out segment, ref partIndex, ref segmentIndex);

			while (segment != null)
			{
				if (predicate == null || predicate(segment))
				{
					yield return recycling && ! allowRecycling
						             ? GeometryFactory.Clone(segment)
						             : segment;
				}

				if (recycling)
				{
					// release the segment, otherwise "pure virtual function call" occurs 
					// when there are certain circular arcs (IsLine == true ?)
					Marshal.ReleaseComObject(segment);
				}

				enumSegment.Next(out segment, ref partIndex, ref segmentIndex);
			}

			enumSegment.Reset();
		}

		[NotNull]
		public static ISegment GetSegment([NotNull] ISegmentCollection segments,
		                                  int partIndex, int segmentIndex)
		{
			Assert.ArgumentNotNull(segments, nameof(segments));

			IEnumSegment enumSegments = segments.EnumSegments;

			enumSegments.SetAt(partIndex, segmentIndex);

			int outPartIndex = -1;
			int outSegmentIndex = -1;
			ISegment segment;
			enumSegments.Next(out segment, ref outPartIndex, ref outSegmentIndex);

			Assert.AreEqual(partIndex, outPartIndex, "part index mismatch");
			Assert.AreEqual(segmentIndex, outSegmentIndex, "segment index mismatch");

			if (! enumSegments.IsRecycling)
			{
				return segment;
			}

			ISegment clone = GeometryFactory.Clone(segment);

			// to avoid "pure virtual function call" when there are certain non-linear segments, on next GC
			Marshal.ReleaseComObject(segment);

			return clone;
		}

		[CanBeNull]
		public static ISegment GetLongestSegment([NotNull] ISegmentCollection segments)
		{
			Assert.ArgumentNotNull(segments, nameof(segments));

			IEnumSegment segmentEnum = segments.EnumSegments;

			segmentEnum.Reset();

			double maxLength = 0;
			int longestSegmentPartIndex = -1;
			int longestSegmentIndex = -1;

			ISegment segment;
			var partIndex = 0;
			var segmentIndex = 0;
			segmentEnum.Next(out segment, ref partIndex, ref segmentIndex);

			while (segment != null)
			{
				if (segment.Length > maxLength)
				{
					// segment enumator is recycling as of 10.1 -> remember part/segment index
					longestSegmentPartIndex = partIndex;
					longestSegmentIndex = segmentIndex;

					maxLength = segment.Length;
				}

				Marshal.ReleaseComObject(segment);
				segmentEnum.Next(out segment, ref partIndex, ref segmentIndex);
			}

			Marshal.ReleaseComObject(segmentEnum);

			return longestSegmentPartIndex >= 0 && longestSegmentIndex >= 0
				       ? GetSegment(segments,
				                    longestSegmentPartIndex, longestSegmentIndex)
				       : null;
		}

		[NotNull]
		public static IList<esriSegmentInfo> GetShortSegments(
			[NotNull] IPolycurve polycurve,
			double minimumSegmentLength)
		{
			const IPolygon perimeter = null;
			bool use3dLength = IsZAware(polycurve);
			return GetShortSegments(polycurve, minimumSegmentLength, perimeter, use3dLength);
		}

		/// <summary>
		/// Gets the segments that are shorter than the specified minimumSegmentLength.
		/// The length of the segments is measured in 3D if the polycurve is Z-aware.
		/// </summary>
		/// <param name="polyCurve">The segment collection (Polyline, Polygon, Path or Ring).</param>
		/// <param name="minimumSegmentLength">The minimum segment length that is not considered too short.</param>
		/// <param name="perimeter">The polygon or envelope withing which the segments shall be processed. The
		/// perimeter must have the same spatial reference as the segmentCollection.</param>
		/// <param name="use3DLength">Whether the 3D length should be used if the polycurve is Z-aware.</param>
		/// <returns>A list of esriSegmentInfo with all short segments.</returns>
		[NotNull]
		public static IList<esriSegmentInfo> GetShortSegments(
			[NotNull] IPolycurve polyCurve,
			double minimumSegmentLength,
			[CanBeNull] IGeometry perimeter,
			bool use3DLength)
		{
			use3DLength = use3DLength && IsZAware(polyCurve);

			return new List<esriSegmentInfo>(
				GetShortSegments((ISegmentCollection) polyCurve, minimumSegmentLength,
				                 use3DLength, perimeter));
		}

		public static bool HasShortSegments(
			[NotNull] ISegmentCollection segmentCollection,
			double minimumSegmentLength,
			bool use3DLength,
			[CanBeNull] IPolygon perimeter = null)
		{
			return GetShortSegmentsCore(segmentCollection,
			                            minimumSegmentLength,
			                            use3DLength,
			                            perimeter)
				.Any();
		}

		[NotNull]
		public static IList<esriSegmentInfo> GetShortSegments(
			[NotNull] ISegmentCollection segmentCollection,
			double minimumSegmentLength,
			bool use3DLength,
			[CanBeNull] IGeometry perimeter = null)
		{
			return GetShortSegmentsCore(segmentCollection,
			                            minimumSegmentLength,
			                            use3DLength,
			                            perimeter)
				.ToList();
		}

		[NotNull]
		private static IEnumerable<esriSegmentInfo> GetShortSegmentsCore(
			[NotNull] ISegmentCollection segmentCollection,
			double minimumSegmentLength,
			bool use3DLength,
			[CanBeNull] IGeometry perimeter)
		{
			// TODO: in case the geometry has Z values (it might still be non-Z aware)
			//       -> calculate 2D geometry

			Assert.ArgumentNotNull(segmentCollection, nameof(segmentCollection));
			Assert.ArgumentCondition(perimeter?.SpatialReference == null ||
			                         SpatialReferenceUtils.AreEqual(
				                         ((IGeometry) segmentCollection).SpatialReference,
				                         perimeter.SpatialReference),
			                         "Spatial reference of perimeter does not match spatial reference of segment collection.");

			if (use3DLength)
			{
				Assert.False(HasUndefinedZValues((IGeometry) segmentCollection),
				             "The provided geometry is not Z-simple");
			}

			IEnumSegment enumSegment;
			if (perimeter == null)
			{
				enumSegment = segmentCollection.EnumSegments;
			}
			else
			{
				if (Disjoint((IGeometry) segmentCollection, perimeter))
				{
					yield break;
				}

				enumSegment = segmentCollection.EnumSegments;

				//    //NOTE: get_IndexedEnumSegments with a polygon or envelope) does not work as expected 
				//    //      -> all segments are returned not just those in the polygon
				//    if (EnsureSimple((IGeometry)segmentCollection))
				//    {
				//        _msg.Debug("Geometry was simplified.");
				//    }

				//    enumSegment = segmentCollection.get_IndexedEnumSegments(perimeter);
				//}
			}

			enumSegment.Reset();

			esriSegmentInfo segmentInfo;
			enumSegment.NextEx(out segmentInfo);

			while (segmentInfo.pSegment != null)
			{
				ICurve segment = segmentInfo.pSegment;

				double segmentLength = GetLength(segment, use3DLength);

				if (segmentLength < minimumSegmentLength)
				{
					var disjoint = false;
					if (perimeter != null)
					{
						IGeometry highLevelSegment =
							GetHighLevelGeometry(segmentInfo.pSegment, false);
						try
						{
							disjoint = Disjoint(highLevelSegment, perimeter);
						}
						finally
						{
							Marshal.ReleaseComObject(highLevelSegment);
						}
					}

					if (! disjoint)
					{
						// clone the segment otherwise it will point to the last one in the curve
						segmentInfo.pSegment = GeometryFactory.Clone(segmentInfo.pSegment);

						yield return segmentInfo;
					}
				}

				enumSegment.NextEx(out segmentInfo);
			}

			// According to the doc EnumSegments should return a new instance. However random crashes occur if it is released:
			//Marshal.ReleaseComObject(enumSegment);
			enumSegment.Reset();
		}

		[NotNull]
		public static double[] GetLinearizedSegmentAngles(
			[NotNull] IPath path,
			[CanBeNull] ICollection<int> ignoredSegmentIndexes = null,
			double ignoredAngleValue = double.NaN)
		{
			Assert.ArgumentNotNull(path, nameof(path));

			return GetLinearizedSegmentAngles((ISegmentCollection) path,
			                                  ignoredSegmentIndexes,
			                                  ignoredAngleValue);
		}

		[NotNull]
		public static double[] GetLinearizedSegmentAngles(
			[NotNull] IRing ring,
			[CanBeNull] ICollection<int> ignoredSegmentIndexes = null,
			double ignoredAngleValue = double.NaN)
		{
			Assert.ArgumentNotNull(ring, nameof(ring));

			return GetLinearizedSegmentAngles((ISegmentCollection) ring,
			                                  ignoredSegmentIndexes,
			                                  ignoredAngleValue);
		}

		public static double GetLength([NotNull] IGeometry geometry,
		                               bool use3DLength = false)
		{
			var curve = geometry as ICurve;

			if (curve != null)
			{
				return GetLength(curve, use3DLength);
			}

			if (geometry.GeometryType == esriGeometryType.esriGeometryPoint ||
			    geometry.GeometryType == esriGeometryType.esriGeometryMultipoint)
			{
				return 0;
			}

			// Ring-based multipatches
			var geometryCollection = geometry as IGeometryCollection;

			if (geometryCollection != null)
			{
				return GetParts(geometryCollection).Sum(part => GetLength(part, use3DLength));
			}

			throw new NotImplementedException(
				$"GetLength not implemented for {geometry.GeometryType}");
		}

		public static double GetLength([NotNull] ICurve curve, bool use3DLength)
		{
			// TODO: in case of use3DLength == false and curve has Z values (regardless of IZAware)::
			//       implement own length calculation or fill into geometry with no Zs.
			return use3DLength
				       ? GetLength3D(curve)
				       : curve.Length;
		}

		/// <summary>
		/// Calculates the 3D length of a z-aware curve with non-NaN Z values. In case
		/// the curve has non-linear segments or is a non-linear segment the length is
		/// approximated by assuming constant slope for each segment.
		/// </summary>
		/// <param name="curve"></param>
		/// <returns></returns>
		/// <remarks>This method fails if the geometry is not Z-aware.</remarks>
		public static double GetLength3D([NotNull] ICurve curve)
		{
			// Non-linear segments don't implement ICurve3D until 9.3.1 SP1. In 9.3.1 SP2
			// and 10.0 they implement ICurve3D but fail to calculate Length3D
			var segmentCollection = curve as ISegmentCollection;
			var segmentZ = curve as ISegmentZ;

			if (segmentZ != null)
			{
				return GetLength3D(segmentZ);
			}

			Assert.NotNull(segmentCollection,
			               "Curve does neither implement ISegmentZ nor ISegmentCollection");

			var nonLinear = false;
			segmentCollection.HasNonLinearSegments(ref nonLinear);

			if (! nonLinear)
			{
				var curve3D = curve as ICurve3D;

				// Test if this ever happens:
				Assert.NotNull(curve3D,
				               (FormattableString)
				               $"Linear geometry does not implement ICurve3D: {ToString(curve)}");

				return curve3D.Length3D;
			}

			// Non-linear path/ring/polyline/polygon: Calculate length for each segment
			// based on z difference from start to end of segment, assuming constant slope
			double length3D = 0;

			IEnumSegment enumSegments = segmentCollection.EnumSegments;
			enumSegments.Reset();

			ISegment segment;
			int outPartIndex = -1;
			int segmentIndex = -1;
			enumSegments.Next(out segment, ref outPartIndex, ref segmentIndex);
			while (segment != null)
			{
				length3D += GetLength3D((ISegmentZ) segment);

				// release the segment, otherwise "pure virtual function call" occurs 
				// when there are certain circular arcs (IsLine == true ?)
				Marshal.ReleaseComObject(segment);

				enumSegments.Next(out segment, ref outPartIndex, ref segmentIndex);
			}

			return length3D;
		}

		/// <summary>
		/// Calculates the 3D length of a z-aware segment with non-NaN Z values. In case
		/// it is a non-linear segment the length is approximated by assuming constant slope.
		/// </summary>
		/// <param name="segment"></param>
		/// <returns></returns>
		public static double GetLength3D([NotNull] ISegmentZ segment)
		{
			// TODO: the constant-slope-assumption can result in totally wrong results
			//       for non-linear segments. Implement iterative method dividing the 
			//       curve into small linear pieces
			Assert.ArgumentNotNull(segment, nameof(segment));

			// Non-linear segments don't implement ICurve3D until 9.3.1 SP1. In 9.3.1 SP2
			// and 10.0 they implement ICurve3D but fail to calculate Length3D.
			if (segment is ILine)
			{
				return ((ICurve3D) segment).Length3D;
			}

			double fromZ;
			double toZ;

			segment.GetZs(out fromZ, out toZ);

			double dZ = toZ - fromZ;

			if (double.IsNaN(dZ))
			{
				throw new InvalidOperationException(string.Format(
					                                    "The segment has NaN Z values. Unable to calculate 3D length. From-point Z: {0}. To-point Z: {1}",
					                                    fromZ, toZ));
			}

			double length2D = ((ICurve) segment).Length;

			return Math.Sqrt(dZ * dZ + length2D * length2D);
		}

		[NotNull]
		public static IPolygon GetClippedPolygon([NotNull] IPolygon polygon,
		                                         [NotNull] IEnvelope clipEnvelope)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));
			Assert.ArgumentNotNull(clipEnvelope, nameof(clipEnvelope));

			var topoOp = (ITopologicalOperator) polygon;

			IPolygon result;
			try
			{
				result = new PolygonClass();
				// Note: QueryClipped() modifies input in rare cases. QueryClippedDense() apparently does not.
				//       This bug (BUG-000111224) is apparently fixed in 10.6.1
				topoOp.QueryClippedDense(clipEnvelope, double.MaxValue, result);
			}
			catch (Exception e)
			{
				_msg.DebugFormat(
					"Exception in QueryClipped ({0}), trying again using Intersect()",
					e.Message);

				result = (IPolygon) topoOp.Intersect(clipEnvelope, polygon.Dimension);

				Assert.NotNull(result, "intersection result is null");
			}

			var resultTopoOp = (ITopologicalOperator) result;
			if (! resultTopoOp.IsKnownSimple)
			{
				// as of 10.1, QueryClipped was observed to return IsKnownSimple==false
				// in some cases. The observed results were perfectly simple polygons,
				// but it's probably safer to do a Simplify() anyway, as there might 
				// be other cases where the result is *really* non-simple.
				resultTopoOp.Simplify();
			}

			return result;
		}

		[NotNull]
		public static IPolyline GetClippedPolyline([NotNull] IPolyline polyline,
		                                           [NotNull] IEnvelope clipEnvelope)
		{
			Assert.ArgumentNotNull(polyline, nameof(polyline));
			Assert.ArgumentNotNull(clipEnvelope, nameof(clipEnvelope));

			var topoOp = (ITopologicalOperator) polyline;

			IPolyline result;
			try
			{
				result = new PolylineClass();
				topoOp.QueryClipped(clipEnvelope, result);
			}
			catch (Exception e)
			{
				_msg.DebugFormat(
					"Exception in QueryClipped ({0}), trying again using Intersect()", e.Message);

				result = (IPolyline) topoOp.Intersect(clipEnvelope, polyline.Dimension);

				Assert.NotNull(result, "intersection result is null");
			}

			return result;
		}

		/// <summary>
		/// Generalizes the provided polycurve in 2D.
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="tolerance"></param>
		public static void Generalize([NotNull] IPolycurve polycurve, double tolerance)
		{
			Assert.ArgumentNotNull(polycurve, nameof(polycurve));

			// NOTE: Weed with 0.0 returns wrong result (see ReproTestWeedAndGeneralizeWithTolerance0)
			if (Math.Abs(tolerance) < double.Epsilon && polycurve is IPolygon)
			{
				// or throw NotImplementedProperlyException?
				tolerance = GetXyTolerance(polycurve) * GetSmallButValidToleranceFactor(polycurve);
			}

			EnsureLinearized(polycurve, tolerance);

			polycurve.Generalize(tolerance);
		}

		/// <summary>
		/// Generalizes the provided polycurve in 3D.
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="tolerance"></param>
		public static void Generalize3D([NotNull] IPolycurve polycurve, double tolerance)
		{
			Assert.ArgumentNotNull(polycurve, nameof(polycurve));

			// NOTE: Weed with 0.0 returns wrong result (see ReproTestWeedAndGeneralizeWithTolerance0)
			if (Math.Abs(tolerance) < double.Epsilon && polycurve is IPolygon)
			{
				tolerance = GetXyTolerance(polycurve) * GetSmallButValidToleranceFactor(polycurve);
			}

			EnsureLinearized(polycurve, tolerance);

			var zAware = polycurve as IZAware;

			Assert.True(zAware != null && zAware.ZAware,
			            "The provided polycurve is not Z aware.");

			((IPolycurve3D) polycurve).Generalize3D(tolerance);
		}

		/// <summary>
		/// Weeds the specified polycurve in 2D using the provided tolerance.
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="toleranceFactor">The factor (multiple) of the tolerance.</param>
		public static void Weed([NotNull] IPolycurve polycurve, double toleranceFactor)
		{
			Assert.ArgumentNotNull(polycurve, nameof(polycurve));
			Assert.ArgumentCondition(polycurve.SpatialReference != null,
			                         "polyCurve has no spatial reference");

			// NOTE: Weed with 0.0 returns wrong result (see ReproTestWeedAndGeneralizeWithTolerance0)
			if (Math.Abs(toleranceFactor) < double.Epsilon && polycurve is IPolygon)
			{
				// or throw NotImplementedProperlyException?
				toleranceFactor = GetSmallButValidToleranceFactor(polycurve);
			}

			double tolerance = GetXyTolerance(polycurve);

			EnsureLinearized(polycurve, tolerance * toleranceFactor);

			polycurve.Weed(toleranceFactor);
		}

		/// <summary>
		/// Weeds the specified polycurve in 3D using the provided tolerance.
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="toleranceFactor">The factor (multiple) of the tolerance.</param>
		public static void Weed3D([NotNull] IPolycurve polycurve, double toleranceFactor)
		{
			Assert.ArgumentNotNull(polycurve, nameof(polycurve));
			Assert.ArgumentCondition(polycurve.SpatialReference != null,
			                         "polyCurve has no spatial reference");

			// NOTE: Weed with 0.0 returns wrong result (see ReproTestWeedAndGeneralizeWithTolerance0)
			if (Math.Abs(toleranceFactor) < double.Epsilon && polycurve is IPolygon)
			{
				// or throw NotImplementedProperlyException?
				toleranceFactor = GetSmallButValidToleranceFactor(polycurve);
			}

			double tolerance = GetXyTolerance(polycurve);

			EnsureLinearized(polycurve, tolerance * toleranceFactor);

			((IPolycurve3D) polycurve).Weed3D(toleranceFactor);
		}

		public static bool CanWeed3D([NotNull] IPolycurve polycurve,
		                             out CannotWeedReason reason)
		{
			Assert.ArgumentNotNull(polycurve, nameof(polycurve));

			reason = CannotWeedReason.Undefined;

			if (! IsZAware(polycurve))
			{
				reason = CannotWeedReason.NotZaware;

				return false;
			}

			if (HasNonLinearSegments(polycurve))
			{
				reason = CannotWeedReason.NonLinearSegments;

				return false;
			}

			return true;
		}

		public static bool HasNonLinearSegments([NotNull] IGeometry geometry)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			var segmentCollection = geometry as ISegmentCollection;

			var result = false;
			if (segmentCollection != null)
			{
				segmentCollection.HasNonLinearSegments(ref result);
			}

			return result;
		}

		public static bool HasNonLinearSegments(
			[NotNull] ISegmentCollection segmentCollection)
		{
			Assert.ArgumentNotNull(segmentCollection, nameof(segmentCollection));

			var result = false;
			segmentCollection.HasNonLinearSegments(ref result);

			return result;
		}

		public static bool HasLinearCircularArcs([NotNull] ISegmentCollection segments)
		{
			if (! HasNonLinearSegments(segments))
			{
				return false;
			}

			return HasMatchingSegment(
				segments,
				segment =>
				{
					var circularArc = segment as ICircularArc;

					return circularArc != null && circularArc.IsLine;
				});
		}

		public static bool HasMatchingSegment([NotNull] ISegmentCollection segments,
		                                      [NotNull] Predicate<ISegment> match)
		{
			Assert.ArgumentNotNull(segments, nameof(segments));
			Assert.ArgumentNotNull(match, nameof(match));

			int segmentCount = segments.SegmentCount;

			// Note: can't use EnumSegments here, since that gets unstable after reading linear circular arcs
			for (var i = 0; i < segmentCount; i++)
			{
				ISegment segment = segments.Segment[i];
				if (match(segment))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Converts non-linear segments of the input polycurve into linear segments
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="maxDeviation">Maximum allowable offset.
		/// NOTE: This cannot be compared to the douglas-peuker tolerance, especially for
		/// small numbers. If in doubt use a smaller deviation or use Generalize()</param>
		public static void Linearize([NotNull] IPolycurve polycurve, double maxDeviation)
		{
			if (! HasNonLinearSegments(polycurve))
			{
				return;
			}

			// do not apply any max segment length
			const int maxSegmentLength = 0;

			// NOTE: System.Runtime.InteropServices.COMException : Cannot do 3D operations on curve segments.
			//		 But polycurveDensify interpolates the Z values as well!
			polycurve.Densify(maxSegmentLength, maxDeviation);
		}

		public static IEnumerable<ILine> GetLinearizedSegments(
			[NotNull] ISegmentCollection segments,
			double maxDeviation)
		{
			Assert.ArgumentNotNull(segments, nameof(segments));
			Assert.ArgumentCondition(maxDeviation > 0, "Invalid maxDeviation");

			foreach (ISegment segment in GetSegments(segments.EnumSegments))
			{
				if (segment.GeometryType == esriGeometryType.esriGeometryLine)
				{
					yield return (ILine) segment;
				}
				else
				{
					if (IsDegeneratedToLine(segment))
					{
						yield return ToLinearLine(segment);
						continue;
					}

					double allowedDeviation = 0;
					// NOTE: If a maxDeviation is supplied to Densify() and the internal
					// algorithm determines that less segments are required for the given
					// deviation, the segment count is decreased!
					ILine[] result;
					double densifiedLength;
					if (segment is ICircularArc circularArc)
					{
						double radius = circularArc.Radius;
						densifiedLength = GetDensificationDistance(maxDeviation, radius);
					}
					else if (segment is IEllipticArc elliptic)
					{
						// Just use the semi-minor axis
						double semiMajor = 0;
						double semiMinor = 0;
						double ratio = 0;
						elliptic.GetAxes(ref semiMajor, ref semiMinor, ref ratio);

						Assert.True(semiMinor > 0, "Invalid elliptic arc: semi-minor axes is 0");

						densifiedLength = GetDensificationDistance(maxDeviation, semiMinor);
					}
					else
					{
						// NOTE: This is heuristic/compromise to prevent excessive segment counts
						densifiedLength = maxDeviation * 10;
						allowedDeviation = maxDeviation;
					}

					result = LinearizeSegment(segment, allowedDeviation, densifiedLength);

					foreach (ILine line in result)
					{
						if (line != null)
						{
							yield return line;
						}
					}
				}
			}
		}

		private static ILine[] LinearizeSegment([NotNull] ISegment segment,
		                                        double maxDeviation,
		                                        double densifiedSegmentLength)
		{
			ILine[] result;
			int segmentCount = (int) (segment.Length / densifiedSegmentLength) + 1;

			result = new ILine[segmentCount];

			GeometryBridge.Densify(segment, maxDeviation, ref segmentCount, ref result);

			return result;
		}

		/// <summary>
		/// Gets the necessary densification distance of a cirular arc given a maximum
		/// deviation of the circle.
		/// </summary>
		/// <param name="maxDeviation"></param>
		/// <param name="radius"></param>
		/// <returns></returns>
		private static double GetDensificationDistance(double maxDeviation,
		                                               double radius)
		{
			double ratio = maxDeviation / radius;
			double angle = Math.Acos(1 - ratio);

			if (angle == 0)
			{
				angle = Math.PI / 96;
			}

			double densifiedLength = radius * angle;
			return densifiedLength;
		}

		private static bool IsDegeneratedToLine(ISegment nonLinearSegment)
		{
			if (nonLinearSegment is ICircularArc circularArc)
			{
				if (circularArc.IsLine)
				{
					// Degenerated
					return true;
				}
			}

			if (nonLinearSegment is IEllipticArc ellipticArc)
			{
				if (ellipticArc.IsLine)
				{
					// Degenerated
					return true;
				}
			}

			if (nonLinearSegment is IBezierCurve3 bezierCurve)
			{
				if (bezierCurve.IsLine)
				{
					return true;
				}
			}

			return false;
		}

		private static ILine ToLinearLine(ISegment segment)
		{
			ILine linearLine = new LineClass();

			((IGeometry) linearLine).SpatialReference = segment.SpatialReference;

			linearLine.FromPoint = segment.FromPoint;
			linearLine.ToPoint = segment.ToPoint;

			return linearLine;
		}

		/// <summary>
		/// Splits the provided path into several paths at the specified splitting points.
		/// </summary>
		/// <param name="pathToSplit"></param>
		/// <param name="splittingPoints"></param>
		/// <param name="splitHappenedAtFromPoint">Whether a split happened at the from point
		/// of the path or not.</param>
		/// <returns></returns>
		[NotNull]
		public static IGeometryCollection SplitPath(
			[NotNull] IPath pathToSplit,
			[NotNull] IPointCollection splittingPoints,
			out bool splitHappenedAtFromPoint)
		{
			const bool projectPointsOntoPathToSplit = false;
			double cutOffDistance = GetXyTolerance(pathToSplit);

			return SplitPath(pathToSplit, splittingPoints, projectPointsOntoPathToSplit,
			                 cutOffDistance,
			                 out splitHappenedAtFromPoint);
		}

		/// <summary>
		/// Splits the provided path into several paths at the specified splitting points.
		/// </summary>
		/// <param name="pathToSplit">The path to split</param>
		/// <param name="splittingPoints">The points where the path should be splitted</param>
		/// <param name="projectPointsOntoPathToSplit">Whether the splitting points should be projected
		/// onto the split path. If not, the X,Y,Z values from the splitting points will be introduced.</param>
		/// <param name="cutOffDistance">The maximum allowed distance of a splitting point. If the splitting
		/// point has a larger distance to the path to split it will be ignored.</param>
		/// <param name="splitHappenedAtFromPoint">Whether a split happened at the from point
		/// of the path or not.</param>
		/// <returns></returns>
		[NotNull]
		public static IGeometryCollection SplitPath(
			[NotNull] IPath pathToSplit,
			[NotNull] IPointCollection splittingPoints,
			bool projectPointsOntoPathToSplit,
			double cutOffDistance,
			out bool splitHappenedAtFromPoint)
		{
			Assert.ArgumentNotNull(pathToSplit, nameof(pathToSplit));
			Assert.ArgumentNotNull(splittingPoints, nameof(splittingPoints));

			var polycurveToSplit = (IPolycurve2) GetHighLevelGeometry(pathToSplit);

			const bool createParts = true;
			CrackPolycurve(polycurveToSplit, splittingPoints, projectPointsOntoPathToSplit,
			               createParts, cutOffDistance, out splitHappenedAtFromPoint);

			var subCurves = (IGeometryCollection) polycurveToSplit;

			_msg.DebugFormat("SplitPath: Number of parts created: {0}",
			                 subCurves.GeometryCount);

			return subCurves;
		}

		/// <summary>
		/// Splits the provided path into several paths at the specified splitting points.
		/// </summary>
		/// <param name="pathToSplit">The path to split</param>
		/// <param name="splittingPoints">The points where the path should be splitted</param>
		/// <param name="projectPointsOntoPathToSplit">Whether the splitting points should be projected
		/// onto the split path. If not, the X,Y,Z values from the splitting points will be introduced.</param>
		/// <param name="cutOffDistance">The maximum allowed distance of a splitting point. If the splitting
		/// point has a larger distance to the path to split it will be ignored.</param>
		/// <param name="allowMergingNotSplitCoincidentFromToPoints">Condition to determine whether the last
		/// part and the first part of the closed path to split should be merged if there was no split at 
		/// the original from/to-point. Can be used to move the from/to-point in rings.
		/// of the path or not.</param>
		/// <returns></returns>
		[NotNull]
		public static IGeometryCollection SplitPath(
			[NotNull] IPath pathToSplit,
			[NotNull] IPointCollection splittingPoints,
			bool projectPointsOntoPathToSplit,
			double cutOffDistance,
			[CanBeNull] Predicate<IGeometryCollection> allowMergingNotSplitCoincidentFromToPoints)
		{
			Assert.ArgumentNotNull(pathToSplit, nameof(pathToSplit));
			Assert.ArgumentNotNull(splittingPoints, nameof(splittingPoints));

			bool splitHappenedAtFrom;
			IGeometryCollection splittedPaths = SplitPath(
				pathToSplit, splittingPoints, projectPointsOntoPathToSplit, cutOffDistance,
				out splitHappenedAtFrom);

			{
				if (pathToSplit.IsClosed && ! splitHappenedAtFrom &&
				    splittedPaths.GeometryCount > 1 &&
				    allowMergingNotSplitCoincidentFromToPoints != null &&
				    allowMergingNotSplitCoincidentFromToPoints(splittedPaths))
				{
					int lastIndex = splittedPaths.GeometryCount - 1;

					var lastPart = (ISegmentCollection) splittedPaths.Geometry[lastIndex];
					var firstPart = (ISegmentCollection) splittedPaths.Geometry[0];

					lastPart.AddSegmentCollection(firstPart);

					splittedPaths.RemoveGeometries(0, 1);
				}
			}

			return splittedPaths;
		}

		/// <summary>
		/// Cracks the specified polycurve by adding vertices at the provided split points or alternatively 
		/// splitting it into separate geometry parts. This method encapsulates IPolycurve2.SplitAtPoints
		/// The general characteristics of the polycurve are maintained (non-linear segments).
		/// </summary>
		/// <param name="polycurveToSplit">The polycurve to crack up</param>
		/// <param name="splittingPoints">The points at which vertices should be inserted or the geometry should </param>
		/// <param name="projectPointsOntoPathToSplit">Whether the split points should be projected onto the 
		/// geometry before cracking it</param>
		/// <param name="createParts">Whether the geometry should be split into several geometry parts (NOTE: this is not tested for polygons)
		/// or, if false, only vertices should be inserted</param>
		/// <param name="cutOffDistance">The maximum distance at which split points are still considered</param>
		/// <returns></returns>
		public static IList<IPoint> CrackPolycurve(
			[NotNull] IPolycurve polycurveToSplit,
			[NotNull] IPointCollection splittingPoints,
			bool projectPointsOntoPathToSplit,
			bool createParts, double? cutOffDistance = null)
		{
			if (cutOffDistance == null)
			{
				// All points are used if -1 is specified (also in IPolycurve2.SplitAtPoints())
				cutOffDistance = -1;
			}

			IList<IPoint> splitPoints = SplitPolycurve(polycurveToSplit, splittingPoints,
			                                           projectPointsOntoPathToSplit,
			                                           createParts, (double) cutOffDistance);

			return splitPoints;
		}

		/// <summary>
		/// Cracks the specified polycurve by adding vertices at the provided split points or alternatively 
		/// splitting it into separate geometry parts. This method encapsulates IPolycurve2.SplitAtPoints
		/// The general characteristics of the polycurve are maintained (non-linear segments).
		/// </summary>
		/// <param name="polycurveToSplit">The polycurve to crack up</param>
		/// <param name="splittingPoints">The points at which vertices should be inserted or the geometry should </param>
		/// <param name="projectPointsOntoPathToSplit">Whether the split points should be projected onto the 
		/// geometry before cracking it</param>
		/// <param name="createParts">Whether the geometry should be split into several geometry parts (NOTE: this is not tested for polygons)
		/// or, if false, only vertices should be inserted</param>
		/// <param name="cutOffDistance">The maximum distance from the polycurve at which split points are still considered</param>
		/// <param name="splitHappenedAtFromPoint">Whether a split happened at the from point
		/// of the polycurve or not</param>
		/// <returns></returns>
		public static IList<IPoint> CrackPolycurve(
			[NotNull] IPolycurve polycurveToSplit,
			[NotNull] IPointCollection splittingPoints,
			bool projectPointsOntoPathToSplit,
			bool createParts, double? cutOffDistance,
			out bool splitHappenedAtFromPoint)
		{
			Assert.ArgumentNotNull(polycurveToSplit, nameof(polycurveToSplit));
			Assert.ArgumentNotNull(splittingPoints, nameof(splittingPoints));

			if (cutOffDistance == null)
			{
				// It seems that all points are used if -1 is specified
				cutOffDistance = -1;
			}

			IList<IPoint> splitPoints = SplitPolycurve(polycurveToSplit, splittingPoints,
			                                           projectPointsOntoPathToSplit,
			                                           createParts, (double) cutOffDistance);

			splitHappenedAtFromPoint = splitPoints.Count > 0 &&
			                           AreEqualInXY(splitPoints[0], polycurveToSplit.FromPoint);

			return splitPoints;
		}

		/// <summary>
		/// Ensures that vertices exist at the provided split points and optionally splits the polycurve
		/// into separate parts.
		/// </summary>
		/// <param name="polycurve">The polycurve to split</param>
		/// <param name="splitPoints">The split points. If two consecutive split points on a line are closer
		/// than the tolerance, an empty part results (FromPoint and ToPoint property will throw) and the next
		/// previous part's to point is snapped to the next part's from point.
		/// </param>
		/// <param name="projectOnto">Wether the split points are to be projected onto the polycurve or not.
		/// If true, the new Z and M values are interpolated. If false, they are taken from the provided split points.</param>
		/// <param name="createParts">Whether the geometry should be split into several geometry parts (NOTE: this is not tested for polygons)
		/// or, if false, only vertices should be inserted</param>
		/// <param name="cutOffDistance">If larger 0 and not NaN, split points further away than cutOffDistance
		/// are ignored</param>
		/// <returns>The list of points that were used to ensure a vertex. If projectOnto is true, the projected
		/// points are returned</returns>
		[NotNull]
		public static IList<IPoint> SplitPolycurve(
			[NotNull] IPolycurve polycurve,
			[NotNull] IPointCollection splitPoints,
			bool projectOnto,
			bool createParts,
			double cutOffDistance)
		{
			// use descending order to avoid incorect split locations due to slight changes in length by point insertion
			IEnumerable<KeyValuePair<IPoint, double>> usablePoints =
				GetPointsOrderedDecendingAlongPolycurve(splitPoints, polycurve, cutOffDistance,
				                                        projectOnto);

			var result = new List<IPoint>();

			var curveCollection = (IGeometryCollection) polycurve;

			foreach (KeyValuePair<IPoint, double> usablePoint in usablePoints)
			{
				bool splitHappened;
				int newPartIdx;

				if (projectOnto)
				{
					// use the presumably cheaper method
					polycurve.SplitAtDistance(usablePoint.Value, false, createParts,
					                          out splitHappened, out newPartIdx, out int _);
				}
				else
				{
					polycurve.SplitAtPoint(usablePoint.Key, false, createParts,
					                       out splitHappened, out newPartIdx, out int _);
				}

				// NOTE: if createParts == true: empty parts are generated, otherwise no empty segment is created and
				//		 splitHappens is false. But splitHappens is also false if there was a vertex already
				if (createParts && curveCollection.Geometry[newPartIdx].IsEmpty)
				{
					// the split point was too close to the previous split point
					curveCollection.RemoveGeometries(newPartIdx, 1);
					curveCollection.GeometriesChanged();

					_msg.DebugFormat(
						"Empty part resulted from split point {0}|{1} and was removed from output",
						usablePoint.Key.X, usablePoint.Key.Y);
				}

				// For consistency add the split point even if the result part was empty (and removed) or no additional 
				// segment was created due to short split point distance.
				// -> there is a vertex within the tolerance and the meaning of the result points is 'ensured a vertex 
				//    exists at the desired split point' rather than 'a split was needed at the desired split point'
				result.Add(usablePoint.Key);

				_msg.VerboseDebug(
					() =>
						$"Split happened at {usablePoint.Key.X}|{usablePoint.Key.Y}: {splitHappened}");
			}

			if (result.Count > 0)
			{
				// This is important for Length and ZSimple to be correct (esp. with non-linear segments)
				((ISegmentCollection) polycurve).SegmentsChanged();
			}

			// return used split points in ascending order along polycurve
			result.Reverse();

			return result;
		}

		/// <summary>
		/// Implementation that uses the ArcObjects method, which is affected by
		/// https://issuetracker02.eggits.net/browse/COM-268 (cutting circular arcs produces empty parts).
		/// TODO: Delete this method once SplitPolycurve is thoroughly tested 
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="splitPoints"></param>
		/// <param name="projectOnto"></param>
		/// <param name="createParts"></param>
		/// <param name="cutOffDistance"></param>
		/// <returns></returns>
		public static IList<IPoint> SplitPolycurveAo(
			IPolycurve polycurve,
			IPointCollection splitPoints,
			bool projectOnto,
			bool createParts,
			double cutOffDistance)
		{
			var result = new List<IPoint>();

			// NOTE: allow indexing does not improve performance
			IEnumSplitPoint enumSplitPoints =
				((IPolycurve2) polycurve).SplitAtPoints(
					splitPoints.EnumVertices, projectOnto, createParts, cutOffDistance);

			IPoint splitPoint;

			enumSplitPoints.Reset();

			enumSplitPoints.Next(out splitPoint, out int _, out int _);

			var splitHappened = false;

			while (splitPoint != null)
			{
				if (enumSplitPoints.SplitHappened)
				{
					splitHappened = true;
					result.Add(splitPoint);
				}

				if (! enumSplitPoints.SplitHappened && _msg.IsVerboseDebugEnabled)
				{
					_msg.VerboseDebug(
						() => $"Path not split at point {splitPoint.X}/{splitPoint.Y}");
				}

				enumSplitPoints.Next(out splitPoint, out int _, out int _);
			}

			if (splitHappened)
			{
				// This is important for Length and ZSimple to be correct (esp. with non-linear segments)
				((ISegmentCollection) polycurve).SegmentsChanged();
			}

			Marshal.ReleaseComObject(enumSplitPoints);

			return result;
		}

		/// <summary>
		/// Splits a polyline if possible.
		/// </summary>
		/// <param name="polyline">The polyline.</param>
		/// <param name="splitPoint">The split point.</param>
		/// <param name="projectSplitPointOntoLine">Whether the split point should be projected onto the polyline geometry
		/// or not. If not, the two geometries might form a kink at the split point. This could be desireable if the split
		/// point is for example an existing junction which should not be moved.</param>
		/// <param name="shorterGeometry">The resulting shorter line.</param>
		/// <param name="longerGeometry">The resulting longer line.</param>
		/// <returns>Whether the split could be done or not.</returns>
		public static bool TrySplitPolyline(IPolyline polyline,
		                                    IPoint splitPoint,
		                                    bool projectSplitPointOntoLine,
		                                    out IPolyline shorterGeometry,
		                                    out IPolyline longerGeometry)
		{
			shorterGeometry = null;
			longerGeometry = null;

			IPolyline originalPolyline = GeometryFactory.Clone(polyline);

			bool splitHappened;
			int newPartIndex;

			originalPolyline.SplitAtPoint(splitPoint, projectSplitPointOntoLine, true,
			                              out splitHappened, out newPartIndex, out int _);

			if (! splitHappened)
			{
				return false;
			}

			var newPart =
				(ICurve) ((IGeometryCollection) originalPolyline).get_Geometry(newPartIndex);

			double originalLength = originalPolyline.Length;

			((IGeometryCollection) originalPolyline).RemoveGeometries(newPartIndex, 1);

			var highLevelNewPart = (IPolyline) GetHighLevelGeometry(newPart);

			if (newPart.Length > originalLength / 2)
			{
				// new part is larger
				longerGeometry = highLevelNewPart;
				shorterGeometry = originalPolyline;
			}
			else
			{
				// old part is longer
				shorterGeometry = highLevelNewPart;
				longerGeometry = originalPolyline;
			}

			return true;
		}

		public static IGeometry GetBoundary([NotNull] IGeometry geometry)
		{
			ITopologicalOperator topoOperator = GetTopoOperator(geometry);

			IGeometry result;

			try
			{
				result = topoOperator.Boundary;

				// Work-around for wrong multipatch boundary (s. Repro_ITopologicalOperator_Boundary_IncorrectForSpecificMultipatch)
				// for ring-based multipatches. Non-ring cases need to be investigated separately if relevant.
				if (IsRingBasedMultipatch(geometry as IMultiPatch))
				{
					List<IRing> multipatchRings = GetRings((IMultiPatch) geometry).ToList();

					if (multipatchRings.Count == 1)
					{
						// Work around is limited to simple 1-ring multipatches - multi-ring work around would be more complex / risky
						double resolution = GetXyResolution(geometry);

						var officialResult = (IPolyline) result;
						IRing singleRing = multipatchRings[0];

						if (! MathUtils.AreEqual(singleRing.Length, officialResult.Length,
						                         resolution))
						{
							// The result is wrong - use ring boundary instead
							result = GeometryFactory.CreatePolyline(multipatchRings[0]);
						}
					}
				}
			}
			catch (COMException comException)
			{
				_msg.Debug("COM exception from ITopologicalOperator.Boundary", comException);

				// this does not work with geometry errors:
				if (comException.ErrorCode == (int) esriGeometryError.E_GEOMETRY_NOTSIMPLE ||
				    comException.ErrorCode == _esriGeometryErrorNotSimple)
				{
					_msg.DebugFormat("Non-simple geometry: {0}", ToString(geometry));

					// try again after simplifying a clone
					IGeometry inputClone = GeometryFactory.Clone(geometry);
					Simplify(inputClone);
					topoOperator = GetTopoOperator(inputClone);

					result = topoOperator.Boundary;
				}
				else
				{
					_msg.DebugFormat("Unknown COM-exception getting boundary from geometry: {0}",
					                 ToString(geometry));
					throw;
				}
			}

			return result;
		}

		/// <summary>
		/// Determines whether the specified multipatch consists of rings only.
		/// </summary>
		/// <param name="multipatch"></param>
		/// <returns></returns>
		public static bool IsRingBasedMultipatch([CanBeNull] IMultiPatch multipatch)
		{
			if (multipatch == null)
			{
				return false;
			}

			int ringCount = GetRings(multipatch, true).Count();

			var geometryCollection = (IGeometryCollection) multipatch;

			int partCount = geometryCollection.GeometryCount;

			return ringCount == partCount;
		}

		public static double ConvertDistance(double distance,
		                                     esriUnits fromUnit,
		                                     esriUnits toUnit)
		{
			IUnitConverter unitConverter = new UnitConverterClass();

			return unitConverter.ConvertUnits(distance, fromUnit, toUnit);
		}

		public static double ConvertArea(double area,
		                                 esriUnits fromUnitSquare,
		                                 esriUnits toUnitSquare)
		{
			double conversionFactor1D = ConvertDistance(1, fromUnitSquare, toUnitSquare);

			double conversionFactor2D = conversionFactor1D * conversionFactor1D;

			return area * conversionFactor2D;
		}

		public static bool TryGetLabelPoint([NotNull] IPolygon polygon,
		                                    [CanBeNull] out IPoint labelPoint)
		{
			return TryGetLabelPoint((IArea) polygon, out labelPoint);
		}

		public static bool TryGetLabelPoint([NotNull] IArea area,
		                                    [CanBeNull] out IPoint labelPoint)
		{
			labelPoint = null;

			if (((IGeometry) area).IsEmpty)
			{
				_msg.DebugFormat(
					"TryGetLabelPoint: Geometry is empty, cannot provide label point.");
				return false;
			}

			try
			{
				labelPoint = area.LabelPoint;
			}
			catch (Exception e)
			{
				// System.Runtime.InteropServices.COMException (0x80040239): Exception from HRESULT: 0x80040239
				_msg.Debug(string.Format("Error getting label point from polygon {0}.",
				                         ToString((IGeometry) area)), e);

				return false;
			}

			return true;
		}

		[CanBeNull]
		public static IBox Get2DBox([CanBeNull] IEnvelope envelope,
		                            double expansionDistance = 0)
		{
			if (envelope == null || envelope.IsEmpty)
			{
				return null;
			}

			double xmin;
			double ymin;
			double xmax;
			double ymax;
			envelope.QueryCoords(out xmin, out ymin,
			                     out xmax, out ymax);

			if (Math.Abs(expansionDistance) > double.Epsilon)
			{
				xmin = xmin - expansionDistance;
				ymin = ymin - expansionDistance;
				xmax = xmax + expansionDistance;
				ymax = ymax + expansionDistance;
			}

			return new Box(new Pnt2D(xmin, ymin),
			               new Pnt2D(xmax, ymax));
		}

		#region Non-public members

		private static double GetSmallButValidToleranceFactor([NotNull] IGeometry geometry)
		{
			const double defaultResolutionToTolerance = 0.1;

			double resolutionToTolerance =
				geometry.SpatialReference != null
					? GetXyResolution(geometry) / GetXyTolerance(geometry)
					: defaultResolutionToTolerance;

			// add safety margin to make sure only points on (almost) completely straight lines are weeded
			const double safetyMarginFactor = 10000;
			double toleranceFactor = resolutionToTolerance / safetyMarginFactor;

			return toleranceFactor;
		}

		/// <summary>
		/// Ensures the non-linear segments of the polycurve are linearized using very small
		/// segments (potentially non-simple)!
		/// </summary>
		/// <param name="polycurve"></param>
		/// <param name="maximumTolerance">The maximum tolerance that will subsequently be used for
		/// generalization. The actual tolerance applied in the linearization is much smaller.</param>
		public static void EnsureLinearized(IPolycurve polycurve, double maximumTolerance)
		{
			if (HasNonLinearSegments(polycurve))
			{
				// NOTE: weed / generalize does not linearize correctly when the curve is a circle
				//		 most likely some IGeometryEnvironment.AutoDensifyTolerance tolerance
				//		 is used.
				//		 -> use minimal weed tolerance  to linearize before actual weeding
				//		    rather than changing auto-densify tolerance.
				//			Do not use the tolerance because there seems to be a different
				//			algorithm behind densify (the same tolerance results in less segments 
				//			copared to Generalize()!

				const double defaultToleranceFactor = 10000;

				double xyTolerance =
					polycurve.SpatialReference == null
						? maximumTolerance / defaultToleranceFactor
						: GetXyTolerance(polycurve);

				double smallButValidTolerance = GetSmallButValidToleranceFactor(polycurve) *
				                                xyTolerance;

				Linearize(polycurve, smallButValidTolerance);
			}
		}

		/// <summary>
		/// Gets the geometry bridge (singleton)
		/// </summary>
		/// <value>The geometry bridge.</value>
		[NotNull]
		public static IGeometryBridge GeometryBridge
		{
			get
			{
				if (_geometryBridge == null)
				{
					// Otherwise the cast could fail in unit tests:
					_geometryBridge = ComUtils.Create<GeometryEnvironmentClass, IGeometryBridge>();
				}

				return Assert.NotNull(_geometryBridge);
			}
		}

		private static bool EnsureSchemaM<T>([NotNull] T immutableGeometry,
		                                     bool schemaHasM,
		                                     [NotNull] out T awareGeometry)
			where T : IGeometry
		{
			var mAware = (IMAware) immutableGeometry;

			if (schemaHasM == mAware.MAware)
			{
				awareGeometry = immutableGeometry;
				return false;
			}

			awareGeometry = GeometryFactory.Clone(immutableGeometry);

			EnsureSchemaM(awareGeometry, schemaHasM);

			return true;
		}

		/// <summary>
		/// Ensures that the Z-awareness of the provided geometry conforms
		/// to the provided GeometryDef.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="immutableGeometry">The immutable geometry.</param>
		/// <param name="schemaHasZ">if set to <c>true</c> the target schema is Z aware.</param>
		/// <param name="defaultZ">Z value to be assigned to vertices which are NaN (e.g. due
		/// to changed Z-awareness) and whose Z values cannot be interpolated.</param>
		/// <param name="awareGeometry">geometry in correct z-awareness</param>
		/// <returns>
		/// Whether the geometry had to be adapted. If
		/// <c>true</c> if the output is a copy, <c>false</c> if the output is
		/// the same instance as the input which already had the correct z-awareness.
		/// </returns>
		private static bool EnsureSchemaZ<T>([NotNull] T immutableGeometry,
		                                     bool schemaHasZ,
		                                     double defaultZ,
		                                     [NotNull] out T awareGeometry)
			where T : IGeometry
		{
			return EnsureSchemaZ(immutableGeometry, schemaHasZ, out awareGeometry, defaultZ);
		}

		private static bool EnsureSchemaZ<T>([NotNull] T immutableGeometry,
		                                     bool schemaHasZ,
		                                     [NotNull] out T awareGeometry,
		                                     double defaultZ) where T : IGeometry
		{
			var sourceGeometryZAware = (IZAware) immutableGeometry;

			if (schemaHasZ == sourceGeometryZAware.ZAware)
			{
				awareGeometry = immutableGeometry;
				return false;
			}

			awareGeometry = GeometryFactory.Clone(immutableGeometry);

			EnsureSchemaZ(awareGeometry, schemaHasZ, defaultZ);

			return true;
		}

		[NotNull]
		private static string HandleToStringException(Exception e)
		{
			string msg = string.Format("Error converting to string: {0}",
			                           e.Message);
			_msg.Debug(msg, e);
			return msg;
		}

		private static void AppendTotalPointCount([NotNull] StringBuilder sb,
		                                          [CanBeNull] IGeometry geometry)
		{
			var points = geometry as IPointCollection;
			if (points == null)
			{
				return;
			}

			sb.AppendFormat("Total point count: {0}", points.PointCount);
			sb.AppendLine();
		}

		private static void AppendHasNonLinearSegments([NotNull] StringBuilder sb,
		                                               [CanBeNull] IGeometry geometry)
		{
			var segments = geometry as ISegmentCollection;
			if (segments == null)
			{
				return;
			}

			sb.AppendFormat("Non-linear segments: {0}",
			                HasNonLinearSegments(segments)
				                ? "yes"
				                : "no");
			sb.AppendLine();
		}

		private static void AppendMinimumSegmentLength([NotNull] StringBuilder sb,
		                                               [CanBeNull] IGeometry geometry)
		{
			var segCol = geometry as ISegmentCollection;
			if (segCol == null)
			{
				return;
			}

			if (segCol.SegmentCount <= 0)
			{
				return;
			}

			double minimumLength = double.MaxValue;

			IEnumSegment enumSegments = segCol.EnumSegments;
			enumSegments.Reset();

			ISegment segment;
			int outPartIndex = -1;
			int segmentIndex = -1;
			enumSegments.Next(out segment, ref outPartIndex, ref segmentIndex);
			while (segment != null)
			{
				if (! segment.IsEmpty && segment.Length < minimumLength)
				{
					minimumLength = segment.Length;
				}

				// release the segment, otherwise "pure virtual function call" occurs 
				// when there are certain circular arcs (IsLine == true ?)
				Marshal.ReleaseComObject(segment);

				enumSegments.Next(out segment, ref outPartIndex, ref segmentIndex);
			}

			sb.AppendFormat("Minimum segment length: {0}", minimumLength);
			sb.AppendLine();
		}

		private static void AppendParts([NotNull] StringBuilder sb,
		                                [NotNull] IGeometryCollection collection)
		{
			int partCount = collection.GeometryCount;

			int skippedPartCount = partCount - _maxToStringPartCount;
			const int maxPartIndex = _maxToStringPartCount - 1;
			var totalPointsWritten = 0;

			sb.AppendFormat("Part count: {0}", partCount);
			sb.AppendLine();

			for (var partIndex = 0; partIndex < partCount; partIndex++)
			{
				if (partIndex > maxPartIndex)
				{
					break;
				}

				if (partIndex == maxPartIndex && partCount > maxPartIndex + 1)
				{
					sb.AppendFormat("... omitted {0} part{1}",
					                skippedPartCount,
					                skippedPartCount == 1
						                ? string.Empty
						                : "s");
					sb.AppendLine();
				}
				else
				{
					sb.AppendFormat("Part {0}:", partIndex + 1);
					sb.AppendLine();
					IGeometry part = collection.Geometry[partIndex];

					IPointCollection points = AppendPart(sb, part, totalPointsWritten);

					totalPointsWritten = totalPointsWritten + points.PointCount;
				}
			}
		}

		[NotNull]
		private static IPointCollection AppendPart([NotNull] StringBuilder sb,
		                                           [NotNull] IGeometry part,
		                                           int totalPointsWritten)
		{
			var points = (IPointCollection) part;

			int maxPointCount = Math.Max(0,
			                             _maxToStringPointCount -
			                             totalPointsWritten);

			sb.Append(ToString(points, maxPointCount));
			return points;
		}

		private static void AppendMAware([NotNull] StringBuilder sb,
		                                 [CanBeNull] IGeometry geometry)
		{
			var mAware = geometry as IMAware;
			if (mAware == null)
			{
				return;
			}

			sb.AppendFormat("M Aware: {0}", mAware.MAware);
			sb.AppendLine();
			if (mAware.MAware)
			{
				sb.AppendFormat("M Simple: {0}", mAware.MSimple);
				sb.AppendLine();
			}
		}

		private static void AppendZAware([NotNull] StringBuilder sb,
		                                 [CanBeNull] IGeometry geometry)
		{
			var zAware = geometry as IZAware;
			if (zAware == null)
			{
				return;
			}

			sb.AppendFormat("Z Aware: {0}", zAware.ZAware);
			sb.AppendLine();
			if (zAware.ZAware)
			{
				sb.AppendFormat("Z Simple: {0}", zAware.ZSimple);
				sb.AppendLine();
			}
		}

		private static void AppendPointIDAware([NotNull] StringBuilder sb,
		                                       [CanBeNull] IGeometry geometry)
		{
			var pointIDAware = geometry as IPointIDAware;
			if (pointIDAware == null)
			{
				return;
			}

			sb.AppendFormat("PointID aware: {0}", pointIDAware.PointIDAware);
			sb.AppendLine();
			if (pointIDAware.PointIDAware)
			{
				sb.AppendFormat("PointID Simple: {0}", pointIDAware.PointIDSimple);
				sb.AppendLine();
			}
		}

		private static void AppendIsSimple([NotNull] StringBuilder sb,
		                                   [CanBeNull] IGeometry geometry)
		{
			try
			{
				string value;
				if (geometry is ITopologicalOperator3 topoOp3)
				{
					sb.AppendFormat("Is known simple: {0}", topoOp3.IsKnownSimple);
					sb.AppendLine();

					topoOp3.IsKnownSimple_2 = false;

					esriNonSimpleReasonEnum reason;
					topoOp3.get_IsSimpleEx(out reason);

					value = string.Format("{0} ({1})", topoOp3.IsSimple, reason);
				}
				else if (geometry is ITopologicalOperator topoOp)
				{
					value = topoOp.IsSimple.ToString();
				}
				else
				{
					value = "n/a";
				}

				sb.AppendFormat("Simple: {0}", value);
				sb.AppendLine();
			}
			catch (Exception e)
			{
				_msg.Debug("Error in simple check.", e);

				sb.AppendFormat(
					"Simple: Unable to determine if geometry simple or not ({0}).",
					e.Message);
				sb.AppendLine();
			}
		}

		private static void AppendMultiPatchTriangles([NotNull] StringBuilder sb,
		                                              [NotNull] ITriangles triangles)
		{
			AppendPoints(sb, (IPointCollection) triangles, _maxToStringPointCount);
		}

		private static void AppendMultiPatchTriangleStrip([NotNull] StringBuilder sb,
		                                                  [NotNull] ITriangleStrip
			                                                  triangleStrip)
		{
			AppendPoints(sb, (IPointCollection) triangleStrip, _maxToStringPointCount);
		}

		private static void AppendMultiPatchTriangleFan([NotNull] StringBuilder sb,
		                                                [NotNull] ITriangleFan triangleFan)
		{
			AppendPoints(sb, (IPointCollection) triangleFan, _maxToStringPointCount);
		}

		private static void AppendMultiPatchRing([NotNull] StringBuilder sb,
		                                         [NotNull] IRing ring,
		                                         [NotNull] IMultiPatch multiPatch)
		{
			sb.AppendFormat("Is closed: {0}", ring.IsClosed);
			sb.AppendLine();

			sb.AppendFormat("Is exterior: {0}", ring.IsExterior);
			sb.AppendLine();

			var isBeginningRing = false;
			sb.AppendFormat("Ring role: {0}",
			                multiPatch.GetRingType(ring, ref isBeginningRing));
			sb.AppendLine();

			sb.AppendFormat("Is beginning ring: {0}", isBeginningRing);
			sb.AppendLine();

			var points = (IPointCollection) ring;

			const bool printPointID = true;
			AppendPoints(sb, points, printPointID, _maxToStringPointCount);
		}

		private static void AppendPoints([NotNull] StringBuilder sb,
		                                 [NotNull] IPointCollection points,
		                                 int maxPointCount)
		{
			const bool printPointID = false;
			AppendPoints(sb, points, printPointID, maxPointCount);
		}

		private static void AppendPoints([NotNull] StringBuilder sb,
		                                 [NotNull] IPointCollection points,
		                                 bool printPointID,
		                                 int maxPointCount)
		{
			sb.AppendFormat("Point count: {0}", points.PointCount);
			sb.AppendLine();

			var maware = points as IMAware;
			bool printM = maware != null && maware.MAware;

			var zaware = points as IZAware;
			bool printZ = zaware != null && zaware.ZAware;

			int pointCount = points.PointCount;

			int startSkipIndex = maxPointCount - 1;
			int endSkipIndex = pointCount - 1; // always write the end point
			int skippedCount = pointCount - maxPointCount;

			IPoint templatePoint = new PointClass();

			for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
			{
				if (pointIndex > startSkipIndex && pointIndex < endSkipIndex)
				{
					// skip point
					continue;
				}

				if (pointIndex == startSkipIndex)
				{
					sb.AppendFormat("... skipping {0} point{1} ...",
					                skippedCount, skippedCount == 1
						                              ? string.Empty
						                              : "s");
				}
				else
				{
					points.QueryPoint(pointIndex, templatePoint);

					AppendPoint(templatePoint, sb, pointIndex, printZ, printM,
					            printPointID);
				}

				sb.AppendLine();
			}
		}

		private static void AppendPoint([NotNull] IPoint point,
		                                [NotNull] StringBuilder sb,
		                                int pointIndex,
		                                bool printZ,
		                                bool printM,
		                                bool printPointID)
		{
			double x;
			double y;
			point.QueryCoords(out x, out y);

			sb.AppendFormat("{0}: {1} {2}", pointIndex, x, y);

			if (printZ)
			{
				sb.AppendFormat(" z:{0}", point.Z);
			}

			if (printM)
			{
				sb.AppendFormat(" m:{0}", point.M);
			}

			if (printPointID)
			{
				sb.AppendFormat(" id:{0}", point.ID);
			}
		}

		private static string ToString([CanBeNull] IPointCollection points,
		                               int maxPointCount)
		{
			var sb = new StringBuilder();

			if (points == null)
			{
				sb.AppendLine("Point collection is null");
			}
			else
			{
				AppendPoints(sb, points, maxPointCount);
			}

			return sb.ToString();
		}

		private static T Interpolate<T>([NotNull] T geometry,
		                                [NotNull] ISurface surface, bool drape,
		                                double stepSize,
		                                [CanBeNull] IDrapeZ draper,
		                                double drapeTolerance)
			where T : IGeometry
		{
			IGeometry result;

			try
			{
				if (geometry.IsEmpty)
				{
					result = GeometryFactory.Clone(geometry);
					MakeZAware(result);
				}
				else
				{
					if (drape || draper != null)
					{
						if (draper != null)
						{
							result = draper.DrapeGeometry(surface, geometry,
							                              drapeTolerance);
						}
						else
						{
							object stepSizeObj = stepSize > 0
								                     ? stepSize
								                     : Type.Missing;

							IGeometry simpleGeometry;
							EnsureSimple(geometry, out simpleGeometry);

							// Use clone to make sure original geo is not modified
							// TODO: remove after ISurface.InterpolateShape guarantees immutability of input
							IGeometry clonedGeometry = GeometryFactory.Clone(simpleGeometry);

							surface.InterpolateShape(clonedGeometry, out result, ref stepSizeObj);

							AssertValidInterpolationResult(surface, clonedGeometry, result);
						}
					}
					else
					{
						// TODO: Workaround, because InterpolateShapeVertices
						// sets the SpatialReference of the input to null!
						IGeometry clonedGeometry = GeometryFactory.Clone(geometry);

						surface.InterpolateShapeVertices(clonedGeometry, out result);

						AssertValidInterpolationResult(surface, clonedGeometry, result);
					}

					// can't assume simplicity
					Simplify(result);
				}
			}
			catch (Exception)
			{
				_msg.Debug("Interpolate(geometry):");
				_msg.Debug(ToString(geometry));
				throw;
			}

			return (T) result;
		}

		private static void AssertValidInterpolationResult([NotNull] ISurface surface,
		                                                   [NotNull] IGeometry input,
		                                                   [CanBeNull] IGeometry result)
		{
			if (result != null)
			{
				return;
			}

			_msg.DebugFormat("Surface domain contains input: {0}",
			                 Contains(surface.Domain, input));

			throw new InvalidOperationException(
				"Interpolation failed (null result returned)");
		}

		/// <summary>
		/// Gets the extrapolated Z value.
		/// </summary>
		/// <param name="point">The point to get the extrapolated Z value for.</param>
		/// <param name="source">The source.</param>
		/// <param name="searchTolerance">The search tolerance.</param>
		/// <param name="distanceToZSource">The distance to the Z source.</param>
		/// <returns></returns>
		private static double GetExtrapolatedZValue([NotNull] IPoint point,
		                                            [NotNull] IGeometry source,
		                                            double searchTolerance,
		                                            out double distanceToZSource)
		{
			Assert.ArgumentNotNull(point, nameof(point));
			Assert.ArgumentNotNull(source, nameof(source));
			var curve = source as ICurve;
			Assert.NotNull(curve, "source must implement ICurve");

			if (source.IsEmpty)
			{
				distanceToZSource = -1;
				return double.NaN;
			}

			AllowIndexing(source);

			// Search nearest point to source
			IPoint nearestPointOnSource = new PointClass();
			double distanceFromCurve = GetDistanceFromCurve(point, curve, nearestPointOnSource);

			distanceToZSource = distanceFromCurve;

			// Search all other similar "nearest" points;
			double searchRadius = searchTolerance + distanceFromCurve;
			IDictionary<ISegment, IPoint> segments =
				GetSegmentsInRadius(point, searchRadius, source);

			if (segments.Count == 0)
			{
				return GetZValueFromGeometry(source, nearestPointOnSource,
				                             searchTolerance);
			}

			double zValueSum = double.NaN;
			var nrZValues = 0;

			foreach (ISegment segment in segments.Keys)
			{
				IPoint segmentPoint = segments[segment];
				double tmpValue = GetZValueFromSegment(segmentPoint, segment);

				if (double.IsNaN(tmpValue))
				{
					continue;
				}

				if (double.IsNaN(zValueSum))
				{
					zValueSum = tmpValue;
				}
				else
				{
					zValueSum += tmpValue;
				}

				nrZValues++;
			}

			return nrZValues > 0
				       ? zValueSum / nrZValues
				       : double.NaN;
		}

		public static double GetDistanceFromCurve([NotNull] IPoint point,
		                                          [NotNull] ICurve curve,
		                                          [NotNull] IPoint nearestPointOnCurve)
		{
			double distanceAlongCurve = -1;
			double distanceFromCurve = -1;
			var rightSide = false;

			curve.QueryPointAndDistance(esriSegmentExtension.esriNoExtension,
			                            point, false, nearestPointOnCurve,
			                            ref distanceAlongCurve,
			                            ref distanceFromCurve,
			                            ref rightSide);
			return distanceFromCurve;
		}

		[NotNull]
		public static IDictionary<ISegment, IPoint> GetSegmentsInRadius(
			[NotNull] IPoint searchPoint,
			double searchRadius,
			[NotNull] IGeometry geometry)
		{
			var result = new Dictionary<ISegment, IPoint>();

			if (geometry is ISegmentCollection)
			{
				IPoint outPoint = new PointClass();
				double distanceAlongCurve = -1, distanceFromCurve = -1;
				var rightSide = false;

				var segments = (ISegmentCollection) geometry;

				IEnvelope searchEnvelope = GetExpandedEnvelope(searchPoint, searchRadius);

				AllowIndexing(geometry);

				IEnumSegment enumSegments = segments.IndexedEnumSegments[searchEnvelope];
				bool recycling = enumSegments.IsRecycling;

				ISegment segment;

				int partIndex = -1;
				int segmentIndex = -1;
				enumSegments.Next(out segment, ref partIndex, ref segmentIndex);

				int segmentCount = segments.SegmentCount;
				var currentSegment = 0;

				while (segment != null && currentSegment < segmentCount)
				{
					segment.QueryPointAndDistance(esriSegmentExtension.esriNoExtension,
					                              searchPoint, false, outPoint,
					                              ref distanceAlongCurve,
					                              ref distanceFromCurve,
					                              ref rightSide);

					if (distanceFromCurve <= searchRadius)
					{
						result.Add(recycling
							           ? GeometryFactory.Clone(segment)
							           : segment,
						           GeometryFactory.Clone(outPoint));
					}

					currentSegment++;

					if (recycling)
					{
						// release the segment, otherwise "pure virtual function call" occurs 
						// when there are certain circular arcs (IsLine == true ?)
						Marshal.ReleaseComObject(segment);
					}

					enumSegments.Next(out segment, ref partIndex, ref segmentIndex);
				}
			}

			return result;
		}

		[NotNull]
		public static IEnvelope GetExpandedEnvelope([NotNull] IGeometry geometry,
		                                            double envelopeExpand)
		{
			Assert.ArgumentNotNull(geometry, nameof(geometry));

			// Envelope accessor returns a copy
			IEnvelope extent = geometry.Envelope;

			extent.Expand(envelopeExpand, envelopeExpand, false);

			return extent;
		}

		private static double GetTargetsZValue(
			[NotNull] IPoint vertex,
			[NotNull] IEnumerable<IGeometry> targets,
			MultiTargetSubMode mode)
		{
			return GetTargetsZValue(vertex, targets, mode, double.NaN);
		}

		private static double GetTargetsZValue(
			[NotNull] IPoint vertex,
			[NotNull] IEnumerable<IGeometry> targets,
			MultiTargetSubMode mode, double radius)
		{
			var zValueCount = 0;
			double zValue = double.NaN;
			foreach (IGeometry target in targets)
			{
				double tmpZValue = double.IsNaN(radius)
					                   ? GetZValueFromGeometry(target, vertex)
					                   : GetZValueFromGeometry(target, vertex, radius);

				if (double.IsNaN(tmpZValue))
				{
					continue;
				}

				switch (mode)
				{
					case MultiTargetSubMode.Lowest:
						if (double.IsNaN(zValue) || zValue > tmpZValue)
						{
							zValue = tmpZValue;
						}

						break;

					case MultiTargetSubMode.Highest:
						if (double.IsNaN(zValue) || zValue < tmpZValue)
						{
							zValue = tmpZValue;
						}

						break;

					case MultiTargetSubMode.Average:
						if (double.IsNaN(zValue))
						{
							zValue = tmpZValue;
						}
						else
						{
							zValue += tmpZValue;
						}

						zValueCount++;
						break;
				}
			}

			if (mode == MultiTargetSubMode.Average && zValueCount > 1)
			{
				zValue = zValue / zValueCount;
			}

			return zValue;
		}

		[NotNull]
		private static double[] GetLinearizedSegmentAngles(
			[NotNull] ISegmentCollection segments,
			[CanBeNull] ICollection<int> ignoredSegmentIndexes,
			double ignoredAngleValue)
		{
			// assume a single part

			WKSPoint[] vertices = GetWKSPoints((IPointCollection4) segments);

			Assert.ArgumentCondition(vertices.Length >= 2, "Invalid vertex count: {0}",
			                         vertices.Length);

			var result = new double[vertices.Length];

			if (vertices.Length == 2)
			{
				result[0] = double.NaN;
				result[1] = double.NaN;

				return result;
			}

			double x2;
			double y2;

			var hasCurrentNonIgnoredSegment = false;
			double currentSegmentDx = double.NaN;
			double currentSegmentDy = double.NaN;
			double currentSegmentLengthSquared = double.NaN;

			var hasLastNonIgnoredSegment = false;
			double lastNonIgnoredSegmentDx = double.NaN;
			double lastNonIgnoredSegmentDy = double.NaN;
			double lastNonIgnoredSegmentLengthSquared = double.NaN;

			var ignoreCurrentSegment = false;

			int minVertex;

			var curve = segments as ICurve;
			bool isClosedLoop = curve != null && curve.IsClosed;

			if (isClosedLoop)
			{
				// use start point of last segment as previous vertex
				WKSPoint startPointOfLastSegment = vertices[vertices.Length - 2];

				x2 = startPointOfLastSegment.X;
				y2 = startPointOfLastSegment.Y;
				minVertex = 1;

				if (ignoredSegmentIndexes != null && ignoredSegmentIndexes.Count > 0)
				{
					// find last non-ignored segment
					for (int vertexIndex = vertices.Length - 1; vertexIndex > 0; vertexIndex--)
					{
						if (ignoredSegmentIndexes.Contains(vertexIndex - 1))
						{
							// next segment from end is ignored
							ignoreCurrentSegment = true;
							continue;
						}

						hasLastNonIgnoredSegment = true;
						lastNonIgnoredSegmentDx =
							vertices[vertexIndex].X - vertices[vertexIndex - 1].X;
						lastNonIgnoredSegmentDy =
							vertices[vertexIndex].Y - vertices[vertexIndex - 1].Y;
						lastNonIgnoredSegmentLengthSquared =
							lastNonIgnoredSegmentDx * lastNonIgnoredSegmentDx +
							lastNonIgnoredSegmentDy * lastNonIgnoredSegmentDy;
						break;
					}
				}
			}
			else
			{
				x2 = double.NaN;
				y2 = double.NaN;

				minVertex = 2;
			}

			for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
			{
				int segmentIndex = vertexIndex - 1;

				// previous vertex
				double x1 = x2;
				double y1 = y2;

				// current vertex
				WKSPoint vertex = vertices[vertexIndex];
				x2 = vertex.X;
				y2 = vertex.Y;

				// previous segment (segment ending in previous vertex)
				bool ignorePreviousSegment = ignoreCurrentSegment;

				if (! ignorePreviousSegment)
				{
					hasLastNonIgnoredSegment = hasCurrentNonIgnoredSegment;
					lastNonIgnoredSegmentDx = currentSegmentDx;
					lastNonIgnoredSegmentDy = currentSegmentDy;
					lastNonIgnoredSegmentLengthSquared = currentSegmentLengthSquared;
				}

				// current segment (segment ending in current vertex)
				ignoreCurrentSegment = ignoredSegmentIndexes != null &&
				                       (segmentIndex < 0 && ignorePreviousSegment ||
				                        ignoredSegmentIndexes.Contains(segmentIndex));

				if (ignoreCurrentSegment)
				{
					if (vertexIndex > 0)
					{
						result[vertexIndex - 1] = ignoredAngleValue;
					}

					continue;
				}

				hasCurrentNonIgnoredSegment = true;
				currentSegmentDx = x2 - x1;
				currentSegmentDy = y2 - y1;
				currentSegmentLengthSquared = currentSegmentDx * currentSegmentDx +
				                              currentSegmentDy * currentSegmentDy;

				if (vertexIndex < minVertex)
				{
					result[vertexIndex] = double.NaN;
					continue;
				}

				result[vertexIndex - 1] =
					hasLastNonIgnoredSegment
						? GetSegmentAngle(lastNonIgnoredSegmentDx, lastNonIgnoredSegmentDy,
						                  lastNonIgnoredSegmentLengthSquared,
						                  currentSegmentDx, currentSegmentDy,
						                  currentSegmentLengthSquared)
						: ignoredAngleValue;
			}

			result[vertices.Length - 1] = isClosedLoop
				                              ? result[0]
				                              : double.NaN;

			return result;
		}

		private static double GetSegmentAngle(double dx01, double dy01, double l01,
		                                      double dx12, double dy12, double l12)
		{
			double prod = dx01 * dx12 + dy01 * dy12;
			double cos2 = prod * prod / (l01 * l12);

			return prod < 0
				       ? Math.Acos(Math.Sqrt(cos2)) // acute angle
				       : Math.PI - Math.Acos(Math.Sqrt(cos2));
		}

		//[NotNull]
		//private static double[] GetLinearizedSegmentAngles(
		//    [NotNull] ISegmentCollection segments)
		//{
		//    // assume a single part

		//    WKSPoint[] vertices = GetPointArray2D((IPointCollection4) segments);

		//    Assert.ArgumentCondition(vertices.Length >= 2, "Invalid vertex count: {0}",
		//                             vertices.Length);

		//    var result = new double[vertices.Length];

		//    if (vertices.Length == 2)
		//    {
		//        result[0] = double.NaN;
		//        result[1] = double.NaN;

		//        return result;
		//    }

		//    double x2 = 0;
		//    double y2 = 0;

		//    double dx1 = 0;
		//    double dy1 = 0;

		//    double l12 = 0;

		//    int minVertex = 2;

		//    bool isClosedLoop = ((ICurve) segments).IsClosed;

		//    if (isClosedLoop)
		//    {
		//        // use start point of last segment as previous vertex
		//        WKSPoint startPointOfLastSegment = vertices[vertices.Length - 2];

		//        x2 = startPointOfLastSegment.X;
		//        y2 = startPointOfLastSegment.Y;
		//        minVertex = 1;
		//    }

		//    for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
		//    {
		//        double dx0 = dx1;
		//        double dy0 = dy1;
		//        double l01 = l12;

		//        double x1 = x2;
		//        double y1 = y2;

		//        WKSPoint vertex = vertices[vertexIndex];

		//        x2 = vertex.X;
		//        y2 = vertex.Y;

		//        dx1 = x2 - x1;
		//        dy1 = y2 - y1;

		//        l12 = dx1 * dx1 + dy1 * dy1;

		//        if (vertexIndex < minVertex)
		//        {
		//            result[vertexIndex] = double.NaN;
		//            continue;
		//        }

		//        double prod = dx0 * dx1 + dy0 * dy1;
		//        double cos2 = prod * prod / (l01 * l12);

		//        double angle = prod < 0
		//                        ? Math.Acos(Math.Sqrt(cos2)) // acute angle
		//                        : Math.PI - Math.Acos(Math.Sqrt(cos2));

		//        result[vertexIndex - 1] = angle;
		//    }

		//    result[vertices.Length - 1] = isClosedLoop
		//                                    ? result[0]
		//                                    : double.NaN;

		//    return result;
		//}

		public static double GetZValueFromSegment([NotNull] IPoint point,
		                                          [NotNull] ISegment segment)
		{
			// shorthand alias:
			IPoint fromPoint = GetZValueFromSegmentFromPoint;
			IPoint toPoint = GetZValueFromSegmentToPoint;

			segment.QueryFromPoint(fromPoint);
			segment.QueryToPoint(toPoint);

			if (double.IsNaN(fromPoint.Z) || double.IsNaN(toPoint.Z))
			{
				return double.NaN;
			}

			double fromDistance = GetPointDistance(point, fromPoint);

			if (Math.Abs(fromDistance) < double.Epsilon)
			{
				return fromPoint.Z;
			}

			if (Math.Abs(fromDistance - segment.Length) < double.Epsilon)
			{
				return toPoint.Z;
			}

			double fromZValue = fromPoint.Z;
			double toZValue = toPoint.Z;

			double zDifference = toZValue - fromZValue;

			return fromZValue + fromDistance / segment.Length * zDifference;
		}

		private static IGeometry GetPartBySecondOpinionHitTest(
			[NotNull] IGeometryCollection congruentGeometry,
			[NotNull] IGeometry searchPath,
			int secondTestPointIdx,
			[NotNull] IGeometry firstOpinion,
			double xyTolerance)
		{
			if (((IPointCollection) searchPath).PointCount <= secondTestPointIdx)
			{
				return firstOpinion;
			}

			IGeometry secondOpinion =
				GetHitGeometryPart(
					((IPointCollection) searchPath).Point[secondTestPointIdx],
					(IGeometry) congruentGeometry, xyTolerance);

			Assert.NotNull(secondOpinion,
			               "GetPartBySecondOpinionHitTest: No part could be associated with intersection.");

			if (firstOpinion == secondOpinion)
			{
				return secondOpinion;
			}

			double firstOpinionLengthDiff =
				Math.Abs(((ICurve) firstOpinion).Length - ((ICurve) searchPath).Length);

			double secondOpinionLengthDiff =
				Math.Abs(((ICurve) secondOpinion).Length - ((ICurve) searchPath).Length);

			if (firstOpinionLengthDiff < secondOpinionLengthDiff)
			{
				return firstOpinion;
			}

			_msg.DebugFormat("Using second opinion hit test to determine associated part.");

			return secondOpinion;
		}

		private static IEnumerable<KeyValuePair<IPoint, double>>
			GetPointsOrderedDecendingAlongPolycurve(IPointCollection points,
			                                        IPolycurve polycurve,
			                                        double maxDistanceFromCurve,
			                                        bool projectedOnto)
		{
			List<KeyValuePair<IPoint, double>> pointsToUse = GetDistancesAlongPolycurve(
				points, polycurve, maxDistanceFromCurve, projectedOnto);

			// descending sort order
			pointsToUse.Sort((x, y) => y.Value.CompareTo(x.Value));

			return pointsToUse;
		}

		/// <summary>
		/// Provides work-around for exceptions accessing in-memory modified polygons such as
		/// in TOP-4484 (ring count), TOP-5320 (connected component bag).
		/// </summary>
		/// <param name="polygon"></param>
		/// <param name="allowSimplify"></param>
		/// <returns></returns>
		private static IPolygon GetRingCountablePolygon(IPolygon polygon, bool allowSimplify)
		{
			IPolygon simplified = allowSimplify
				                      ? polygon
				                      : GeometryFactory.Clone(polygon);

			if (! HasNonLinearSegments(polygon))
			{
				// Remedy: read label point. However, in some cases, such as very small areas, even getting 
				// the label point presumably performs a simplify which results in the polygon to become empty (s. CanGetExteriorRingCountForNonSimplePolygon)
				// Therefore the clone must be used if allowSimplify is false.
				if (! TryGetLabelPoint(simplified, out IPoint _))
				{
					_msg.Debug(
						"Getting the label point failed. Most likely the polygon was very small.");
					// The geometry typically has already been simplified/emptied by IArea.LabelPoint 
					// Just to be sure:
					Simplify(simplified, allowReorder: true);
				}
				else
				{
					_msg.Debug("Calculated new label point to get the exterior ring count");
				}

				return simplified;
			}

			// Calculating the label point is not enough for non-linear geometries:
			Simplify(simplified, allowReorder: true);

			_msg.Debug("The geometry was simplified to get the exterior ring count.");

			return simplified;
		}

		#endregion

		#region Congruence Tests

		/// <summary>
		/// Check if two given polycurves are congruent within the
		/// curves' tolerance multiplied by the given tolerance factor.
		/// The two polycurves must have the same spatial reference.
		/// </summary>
		/// <param name="polycurveOne">The first polycurve.</param>
		/// <param name="polycurveTwo">The second polycurve.</param>
		/// <param name="toleranceFactor">The tolerance factor.</param>
		/// <returns>True if the polycurves are congruent; otherwise, false.</returns>
		public static bool AreCongruentWithinTolerance(
			[NotNull] IPolycurve polycurveOne,
			[NotNull] IPolycurve polycurveTwo,
			double toleranceFactor)
		{
			Assert.ArgumentNotNull(polycurveOne, nameof(polycurveOne));
			Assert.ArgumentNotNull(polycurveTwo, nameof(polycurveTwo));

			if (polycurveOne == polycurveTwo)
			{
				return true; // same instance
			}

			const bool comparePrecisionAndTolerance = true;
			bool compareVCS = IsZAware(polycurveOne) || IsZAware(polycurveTwo);
			bool sameSR = SpatialReferenceUtils.AreEqual(
				polycurveOne.SpatialReference, polycurveTwo.SpatialReference,
				comparePrecisionAndTolerance, compareVCS);

			Assert.ArgumentCondition(sameSR, "Polycurves must have the same SRef");

			// Note:
			// We could compare the Length of the two curves as a quick test,
			// but how big should the threshold for "not congruent" be?
			// This test is probably best done in the calling software...

			if (! HaveSameEnvelope(polycurveOne, polycurveTwo, toleranceFactor))
			{
				return false;
			}

			// Cheap tests could not disprove congruence. Now do the real test!
			// Note that ITopologicalOperator.SymmetricDifference will
			// not work because z-only differences are relevant!

			// Work on copies of the given geometries:
			IPolycurve polyCurveOneCopy = GeometryFactory.Clone(polycurveOne);
			IPolycurve polyCurveTwoCopy = GeometryFactory.Clone(polycurveTwo);

			try
			{
				WKSPointZ[] verticesOne, verticesTwo;
				WeedAllRings(polyCurveOneCopy, toleranceFactor, out verticesOne);
				WeedAllRings(polyCurveTwoCopy, toleranceFactor, out verticesTwo);

				if (verticesOne == null && verticesTwo == null)
				{
					return AreEqual(polyCurveOneCopy, polyCurveTwoCopy);
				}

				if (verticesOne == null || verticesTwo == null)
				{
					return false;
				}

				double xyTolerance = Math.Max(GetXyTolerance(polycurveOne),
				                              GetXyTolerance(polycurveTwo));

				double zTolerance = Math.Max(GetZTolerance(polycurveOne),
				                             GetZTolerance(polycurveTwo));

				return WKSPointZUtils.HaveSameVertices(verticesOne, verticesTwo,
				                                       xyTolerance * toleranceFactor,
				                                       zTolerance * toleranceFactor);
			}
			finally
			{
				Marshal.ReleaseComObject(polyCurveOneCopy);
				Marshal.ReleaseComObject(polyCurveTwoCopy);
			}
		}

		/// <summary>
		/// Weed the given polycurve to the given tolerance factor and
		/// return the vertices that remain after weeding.
		/// </summary>
		/// <param name="polycurve">The polycurve.</param>
		/// <param name="toleranceFactor">The weed tolerance factor.</param>
		/// <param name="vertices">The vertices that remain after the weed.</param>
		/// <remarks>
		/// Weed does a Douglas-Poiker, which always leaves the first
		/// and last vertex unchanged. This is fine for lines but bad
		/// for closed rings. Therefore, if the polycurve is a closed
		/// ring (or set of rings), manually check if the first/last
		/// vertex should be weeded.
		/// </remarks>
		private static void WeedAllRings([NotNull] IPolycurve polycurve,
		                                 double toleranceFactor,
		                                 [CanBeNull] out WKSPointZ[] vertices)
		{
			var zAware = polycurve as IZAware;
			if (zAware != null && zAware.ZAware)
			{
				Weed3D(polycurve, toleranceFactor);
			}
			else
			{
				Weed(polycurve, toleranceFactor);
			}

			// Weed() does a Douglas-Poiker on each part of the IPolycurve.
			// Therefore, first and last vertex remain unchanged, which is
			// fine for lines and bad for rings/polygons.

			if (polycurve.IsClosed)
			{
				int totalPointCount = ((IPointCollection4) polycurve).PointCount;
				var remainingPoints = new List<WKSPointZ>(totalPointCount);

				var parts = polycurve as IGeometryCollection;
				if (parts != null)
				{
					int partCount = parts.GeometryCount;
					for (var index = 0; index < partCount; index++)
					{
						IGeometry part = parts.Geometry[index];

						WeedFromToPoint(part, toleranceFactor, remainingPoints);
					}
				}
				else
				{
					WeedFromToPoint(polycurve, toleranceFactor, remainingPoints);
				}

				vertices = new WKSPointZ[remainingPoints.Count];
				remainingPoints.CopyTo(vertices);
			}
			else
			{
				// polycurve is not closed: reordering is not an issue
				// and therefore the remaining vertices of no interest.
				vertices = null;
			}
		}

		private static void WeedFromToPoint(
			[NotNull] IGeometry geometry,
			double toleranceFactor,
			[NotNull] List<WKSPointZ> remainingPoints)
		{
			WKSPointZ[] pointZs = GetWKSPointZs(geometry);
			//bool weeded = WeedFromToPoint(geometry, toleranceFactor);

			double xyTolerance = GetXyTolerance(geometry);
			double zTolerance = GetZTolerance(geometry);

			bool weeded = WeedFromToPoint(pointZs, xyTolerance, zTolerance,
			                              toleranceFactor);

			if (weeded)
			{
				// Drop FromPoint and ToPoint (they were weeded)
				int count = pointZs.Length - 2;
				remainingPoints.AddRange(CollectionUtils.SubArray(pointZs, 1, count));
			}
			else
			{
				remainingPoints.AddRange(pointZs);
			}
		}

		private static bool WeedFromToPoint([NotNull] WKSPointZ[] pointZs,
		                                    double xyTolerance,
		                                    double zTolerance,
		                                    double toleranceFactor)
		{
			if (pointZs.Length <= 3)
			{
				return false;
			}

			if (! WKSPointZUtils.ArePointsEqual(pointZs, 0, pointZs.Length - 1, xyTolerance,
			                                    zTolerance))
			{
				return false;
			}

			WKSPointZ p = pointZs[0];
			WKSPointZ a = pointZs[1];
			WKSPointZ b = pointZs[pointZs.Length - 2];

			double xyLimit = xyTolerance * toleranceFactor;
			double xyLimitSquared = xyLimit * xyLimit;

			double dd = DistanceSquaredXY(p.X, p.Y, a.X, a.Y, b.X, b.Y);
			if (dd <= xyLimitSquared)
			{
				if (double.IsNaN(zTolerance))
				{
					return true;
				}

				double zLimit = zTolerance * toleranceFactor;
				double zLimitSquared = zLimit * zLimit;

				double zz =
					DistanceSquaredZ(p.X, p.Y, p.Z, a.X, a.Y, a.Z, b.X, b.Y, b.Z);

				if (double.IsNaN(zz))
				{
					return true;
				}

				return zz <= zLimitSquared;
			}

			return false;
		}

		private static bool HaveSameEnvelope([NotNull] IGeometry geometry1,
		                                     [NotNull] IGeometry geometry2,
		                                     double toleranceFactor)
		{
			Assert.ArgumentNotNull(geometry1, nameof(geometry1));
			Assert.ArgumentNotNull(geometry2, nameof(geometry2));

			double xyTolerance = Math.Max(GetXyTolerance(geometry1),
			                              GetXyTolerance(geometry2));
			double zTolerance = Math.Max(GetZTolerance(geometry1),
			                             GetZTolerance(geometry2));

			IEnvelope env1 = geometry1.Envelope;
			IEnvelope env2 = geometry2.Envelope;

			// Hmm... seem not to work on envelopes?!
			//bool isSameInXY = AreEqualInXY(env1, env2);
			//bool isSameInXYZ = AreEqual(env1, env2);

			if (! IsSamePoint(env1.LowerLeft, env2.LowerLeft,
			                  xyTolerance * toleranceFactor,
			                  zTolerance * toleranceFactor))
			{
				return false;
			}

			if (! IsSamePoint(env1.UpperRight, env2.UpperRight,
			                  xyTolerance * toleranceFactor,
			                  zTolerance * toleranceFactor))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Check if the given polycurve has been restructured
		/// by comparing it with the old (previous) shape.
		/// Be careful about Z-awareness.
		/// </summary>
		/// <remarks>
		/// This method is sensitiv to the ordering of vertices
		/// and parts within the two polycurves and therefore
		/// tends to give false negatives.
		/// </remarks>
		public static bool AreCongruentWithinToleranceOld(
			[NotNull] IPolycurve newPolyCurve,
			[NotNull] IPolycurve oldPolyCurve,
			double toleranceFactor)
		{
			// Create copies because shape will be modified:
			IPolycurve newPolyCurveCopy = GeometryFactory.Clone(newPolyCurve);
			IPolycurve oldPolyCurveCopy = GeometryFactory.Clone(oldPolyCurve);

			try
			{
				// Weed3D bombs on non-Z-aware geometries, so distinguish here:
				var zAware = newPolyCurveCopy as IZAware;
				if (zAware != null && zAware.ZAware)
				{
					((IPolycurve3D) newPolyCurveCopy).Weed3D(toleranceFactor);
					((IPolycurve3D) oldPolyCurveCopy).Weed3D(toleranceFactor);
				}
				else
				{
					newPolyCurveCopy.Weed(toleranceFactor);
					oldPolyCurveCopy.Weed(toleranceFactor);
				}

				return AreEqual(newPolyCurveCopy, oldPolyCurveCopy);
			}
			finally
			{
				Marshal.ReleaseComObject(newPolyCurveCopy);
				Marshal.ReleaseComObject(oldPolyCurveCopy);
			}
		}

		private static double GetDefaultSearchRadius([NotNull] IGeometry geometry)
		{
			// TODO should be based on xmax/ymax 
			return _defaultSearchRadius;
		}

		#region Apply weed to Polyline constructed from two segments

		//private static bool WeedFromToPoint(IGeometry geometry, double weedToleranceFactor)
		//{
		//    bool weeded = false;

		//    ISegmentCollection segments = geometry as ISegmentCollection;

		//    if (segments != null)
		//    {
		//        ISegment firstSeg = segments.get_Segment(0);
		//        ISegment lastSeg = segments.get_Segment(-1);

		//        if (AreEqual(firstSeg.FromPoint, lastSeg.ToPoint))
		//        {
		//            bool isZAware = IsZAware(geometry);

		//            // Splice lastSeg and firstSeg (in this order!):
		//            IPolyline fromToPolyline = CreatePolylineFromSegments(
		//                lastSeg, firstSeg, geometry.SpatialReference, isZAware);

		//            segments = fromToPolyline as ISegmentCollection;
		//            Assert.NotNull(segments, "Polyline is not ISegmentCollection");

		//            int segmentsBefore = segments.SegmentCount;
		//            Weed(fromToPolyline, weedToleranceFactor);
		//            int segmentsAfter = segments.SegmentCount;

		//            weeded = segmentsAfter < segmentsBefore;
		//        }
		//    }

		//    return weeded;
		//}

		//private static IPolyline CreatePolylineFromSegments(
		//    ISegment seg1, ISegment seg2, ISpatialReference spatialReference, bool isZAware)
		//{
		//    IPolyline polyline = new PolylineClass();
		//    polyline.SpatialReference = spatialReference;

		//    IZAware zAware = polyline as IZAware;
		//    if (zAware != null)
		//    {
		//        zAware.ZAware = isZAware;
		//    }

		//    ISegmentCollection segColl = (ISegmentCollection)polyline;

		//    object missing = Type.Missing;
		//    segColl.AddSegment(seg1, ref missing, ref missing);
		//    segColl.AddSegment(seg2, ref missing, ref missing);
		//    segColl.SegmentsChanged();

		//    return polyline;
		//}

		#endregion

		#endregion
	}
}
