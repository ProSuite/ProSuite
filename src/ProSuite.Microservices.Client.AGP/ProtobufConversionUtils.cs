using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using Google.Protobuf;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry.EsriShape;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP
{
	public static class ProtobufConversionUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[CanBeNull]
		public static Geometry FromShapeMsg([CanBeNull] ShapeMsg shapeBuffer)
		{
			if (shapeBuffer == null) return null;

			SpatialReference sr = null;

			switch (shapeBuffer.SpatialReferenceCase)
			{
				case ShapeMsg.SpatialReferenceOneofCase.None:
					break;
				case ShapeMsg.SpatialReferenceOneofCase.SpatialReferenceEsriXml:
					if (! string.IsNullOrEmpty(shapeBuffer.SpatialReferenceEsriXml))
					{
						sr = SpatialReferenceBuilder.FromXML(shapeBuffer.SpatialReferenceEsriXml);
					}

					break;
				case ShapeMsg.SpatialReferenceOneofCase.SpatialReferenceWkid:

					sr = SpatialReferenceBuilder.CreateSpatialReference(
						shapeBuffer.SpatialReferenceWkid);

					break;
				default:
					throw new NotSupportedException(
						$"Unsupported spatial reference format: {shapeBuffer.SpatialReferenceCase}");
			}

			Geometry result;

			switch (shapeBuffer.FormatCase)
			{
				case ShapeMsg.FormatOneofCase.EsriShape:

					if (shapeBuffer.EsriShape.IsEmpty) return null;

					result = FromEsriShapeBuffer(shapeBuffer.EsriShape.ToByteArray(), sr);

					break;
				case ShapeMsg.FormatOneofCase.Wkb:

					throw new NotSupportedException(
						"WKB format is currently not supported in AGP client.");

				case ShapeMsg.FormatOneofCase.Envelope:

					result = FromEnvelopeMsg(shapeBuffer.Envelope, sr);

					break;
				default:
					throw new NotImplementedException(
						$"Unsupported format: {shapeBuffer.FormatCase}");
			}

			return result;
		}

		private static Envelope FromEnvelopeMsg([CanBeNull] EnvelopeMsg envProto,
		                                        [CanBeNull] SpatialReference spatialReference)
		{
			if (envProto == null)
			{
				return null;
			}

			var result =
				EnvelopeBuilder.CreateEnvelope(new Coordinate2D(envProto.XMin, envProto.YMin),
				                               new Coordinate2D(envProto.XMax, envProto.YMax),
				                               spatialReference);

			return result;
		}

		[CanBeNull]
		public static ShapeMsg ToShapeMsg([CanBeNull] Geometry geometry)
		{
			if (geometry == null) return null;

			Assert.ArgumentCondition(geometry.SpatialReference != null,
			                         "Spatial reference must not be null");

			var result = new ShapeMsg
			             {
				             EsriShape = ByteString.CopyFrom(geometry.ToEsriShape()),
				             SpatialReferenceEsriXml = geometry.SpatialReference.ToXML()
			             };

			return result;
		}

		public static GdbObjectMsg ToGdbObjectMsg([NotNull] Feature feature,
		                                          [NotNull] Geometry geometry)
		{
			var result = new GdbObjectMsg();

			FeatureClass featureClass = feature.GetTable();

			// Or use handle?
			result.ClassHandle = (int) featureClass.GetID();

			result.ObjectId = (int) feature.GetObjectID();

			result.Shape = ToShapeMsg(geometry);

			return result;
		}

		public static GdbObjectMsg ToGdbObjectMsg(Feature gdbFeature)
		{
			return ToGdbObjectMsg(gdbFeature, gdbFeature.GetShape());
		}

		public static void ToGdbObjectMsgList(
			IEnumerable<Feature> features,
			ICollection<GdbObjectMsg> resultGdbObjects,
			ICollection<ObjectClassMsg> resultGdbClasses)
		{
			Stopwatch watch = null;

			if (_msg.IsVerboseDebugEnabled)
			{
				watch = Stopwatch.StartNew();
			}

			var classIds = new HashSet<long>();
			foreach (Feature feature in features)
			{
				resultGdbObjects.Add(ToGdbObjectMsg(feature));

				if (classIds.Add(feature.GetTable().GetID()))
				{
					resultGdbClasses.Add(ToObjectClassMsg(feature.GetTable()));
				}
			}

			_msg.DebugStopTiming(watch, "Converted {0} features to DTOs", resultGdbObjects.Count);
		}

		public static ObjectClassMsg ToObjectClassMsg([NotNull] Table objectClass)
		{
			esriGeometryType geometryType = TranslateAGPShapeType(objectClass);

			string name = objectClass.GetName();
			string aliasName = objectClass.GetDefinition().GetAliasName();

			ObjectClassMsg result =
				new ObjectClassMsg()
				{
					Name = name,
					Alias = aliasName,
					ClassHandle = (int) objectClass.GetID(),
					GeometryType = (int) geometryType,
					WorkspaceHandle = 0 //workspace?.GetHashCode() ?? -1
				};

			return result;
		}

		private static esriGeometryType TranslateAGPShapeType(Table objectClass)
		{
			if (objectClass is FeatureClass fc)
			{
				var shapeType = fc.GetDefinition().GetShapeType();

				switch (shapeType)
				{
					case GeometryType.Point:
						return esriGeometryType.esriGeometryPoint;
					case GeometryType.Multipoint:
						return esriGeometryType.esriGeometryMultipoint;
					case GeometryType.Polyline:
						return esriGeometryType.esriGeometryPolyline;
					case GeometryType.Polygon:
						return esriGeometryType.esriGeometryPolygon;
					case GeometryType.Multipatch:
						return esriGeometryType.esriGeometryMultiPatch;
					case GeometryType.Envelope:
						return esriGeometryType.esriGeometryEnvelope;

					case GeometryType.GeometryBag:
						return esriGeometryType.esriGeometryBag;

					case GeometryType.Unknown:
						return esriGeometryType.esriGeometryAny;
				}
			}

			return esriGeometryType.esriGeometryNull;
		}

		public static List<Geometry> FromShapeMsgList(
			ICollection<ShapeMsg> shapeBufferList)
		{
			var geometryList = new List<Geometry>(shapeBufferList.Count);

			foreach (var selectableOverlap in shapeBufferList)
			{
				var geometry = FromShapeMsg(selectableOverlap);
				geometryList.Add(geometry);
			}

			return geometryList;
		}

		private static Geometry FromEsriShapeBuffer([NotNull] byte[] byteArray,
		                                            [CanBeNull] SpatialReference spatialReference)
		{
			var shapeType = EsriShapeFormatUtils.GetShapeType(byteArray);

			if (byteArray.Length == 5 && shapeType == EsriShapeType.EsriShapeNull)
				// in case the original geometry was empty, ExportToWkb does not store byte order nor geometry type.
				throw new ArgumentException(
					"The provided byte array represents an empty geometry with no geometry type information. Unable to create geometry");

			Geometry result;

			var geometryType = EsriShapeFormatUtils.TranslateEsriShapeType(shapeType);

			switch (geometryType)
			{
				case ProSuiteGeometryType.Point:
					result = MapPointBuilder.FromEsriShape(byteArray, spatialReference);
					break;
				case ProSuiteGeometryType.Polyline:
					result = PolylineBuilder.FromEsriShape(byteArray, spatialReference);
					break;
				case ProSuiteGeometryType.Polygon:
					result = PolygonBuilder.FromEsriShape(byteArray, spatialReference);
					break;
				case ProSuiteGeometryType.Multipoint:
					result = MultipointBuilder.FromEsriShape(byteArray, spatialReference);
					break;
				case ProSuiteGeometryType.MultiPatch:
					result = MultipatchBuilder.FromEsriShape(byteArray, spatialReference);
					break;
				case ProSuiteGeometryType.Bag:
					result = GeometryBagBuilder.FromEsriShape(
						byteArray, spatialReference); // experimental
					break;

				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported geometry type {shapeType}");
			}

			return result;
		}
	}
}
