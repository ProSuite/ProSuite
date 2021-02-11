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

		public static SpatialReference FromSpatialReferenceMsg(
			[CanBeNull] SpatialReferenceMsg spatialRefMsg,
			[CanBeNull] SpatialReference classSpatialRef = null)
		{
			if (spatialRefMsg == null)
			{
				return null;
			}

			switch (spatialRefMsg.FormatCase)
			{
				case SpatialReferenceMsg.FormatOneofCase.None:
					return null;
				case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml:

					string xml = spatialRefMsg.SpatialReferenceEsriXml;

					return string.IsNullOrEmpty(xml)
						       ? null
						       : SpatialReferenceBuilder.FromXML(xml);

				case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid:

					int wkId = spatialRefMsg.SpatialReferenceWkid;

					return classSpatialRef?.Wkid == wkId
						       ? classSpatialRef
						       : SpatialReferenceBuilder.CreateSpatialReference(wkId);

				case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkt:

					return SpatialReferenceBuilder.CreateSpatialReference(
						spatialRefMsg.SpatialReferenceWkt);

				default:
					throw new NotImplementedException(
						$"Unsupported spatial reference format: {spatialRefMsg.FormatCase}");
			}
		}

		public static SpatialReferenceMsg ToSpatialReferenceMsg(
			[CanBeNull] SpatialReference spatialReference,
			SpatialReferenceMsg.FormatOneofCase format)
		{
			if (spatialReference == null)
			{
				return null;
			}

			SpatialReferenceMsg result = new SpatialReferenceMsg();

			switch (format)
			{
				case SpatialReferenceMsg.FormatOneofCase.None:
					break;

				case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml:
					result.SpatialReferenceEsriXml = spatialReference.ToXML();
					break;

				case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid:
					result.SpatialReferenceWkid = spatialReference.Wkid;
					break;

				case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkt:
					result.SpatialReferenceWkt = spatialReference.Wkt;
					break;

				default:
					throw new NotImplementedException(
						$"Unsupported spatial reference format: {format}");
			}

			return result;
		}

		[CanBeNull]
		public static Geometry FromShapeMsg(
			[CanBeNull] ShapeMsg shapeMsg,
			[CanBeNull] SpatialReference knownSpatialReference = null)
		{
			if (shapeMsg == null) return null;

			if (shapeMsg.FormatCase == ShapeMsg.FormatOneofCase.None) return null;

			SpatialReference sr = knownSpatialReference ??
			                      FromSpatialReferenceMsg(shapeMsg.SpatialReference);

			Geometry result;

			switch (shapeMsg.FormatCase)
			{
				case ShapeMsg.FormatOneofCase.EsriShape:

					if (shapeMsg.EsriShape.IsEmpty) return null;

					result = FromEsriShapeBuffer(shapeMsg.EsriShape.ToByteArray(), sr);

					break;
				case ShapeMsg.FormatOneofCase.Wkb:

					throw new NotSupportedException(
						"WKB format is currently not supported in AGP client.");

				case ShapeMsg.FormatOneofCase.Envelope:

					result = FromEnvelopeMsg(shapeMsg.Envelope, sr);

					break;
				default:
					throw new NotImplementedException(
						$"Unsupported format: {shapeMsg.FormatCase}");
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

		/// <summary>
		/// Converts a geometry to its wire format.
		/// </summary>
		/// <param name="geometry">The geometry</param>
		/// <param name="useSpatialRefWkId">Whether only the spatial reference well-known
		/// id is to be transferred rather than the entire XML representation, which is
		/// a performance bottleneck for a large number of features.</param>
		/// <returns></returns>
		[CanBeNull]
		public static ShapeMsg ToShapeMsg([CanBeNull] Geometry geometry,
		                                  bool useSpatialRefWkId = false)
		{
			if (geometry == null) return null;

			SpatialReference spatialRef = geometry.SpatialReference;

			Assert.ArgumentCondition(spatialRef != null,
			                         "Spatial reference must not be null");

			var result = new ShapeMsg
			             {
				             EsriShape = ByteString.CopyFrom(geometry.ToEsriShape())
			             };

			result.SpatialReference = ToSpatialReferenceMsg(
				spatialRef,
				useSpatialRefWkId
					? SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid
					: SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml);

			return result;
		}

		public static GdbObjectMsg ToGdbObjectMsg([NotNull] Feature feature,
		                                          [NotNull] Geometry geometry,
		                                          bool useSpatialRefWkId)
		{
			var result = new GdbObjectMsg();

			FeatureClass featureClass = feature.GetTable();

			// Or use handle?
			result.ClassHandle = (int) featureClass.GetID();

			result.ObjectId = (int) feature.GetObjectID();

			result.Shape = ToShapeMsg(geometry, useSpatialRefWkId);

			return result;
		}

		public static GdbObjectMsg ToGdbObjectMsg(Feature gdbFeature,
		                                          bool useSpatialRefWkId)
		{
			return ToGdbObjectMsg(gdbFeature, gdbFeature.GetShape(), useSpatialRefWkId);
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

			var classesByClassId = new Dictionary<long, FeatureClass>();

			// Optimization (in Pro, the Map SR seems to be generally equal to the FCs SR, if they match)
			bool omitDetailedShapeSpatialRef = true;

			foreach (Feature feature in features)
			{
				FeatureClass featureClass = feature.GetTable();

				Geometry shape = feature.GetShape();

				// NOTE: The following calls are expensive:
				// - Geometry.GetShape() (internally, the feature's spatial creation seems costly)
				// - FeatureClassDefintion.GetSpatialReference()
				// In case of a large feature count, they should be avoided on a per-feature basis:

				if (! classesByClassId.ContainsKey(featureClass.GetID()))
				{
					resultGdbClasses.Add(ToObjectClassMsg(featureClass));

					classesByClassId.Add(featureClass.GetID(), featureClass);

					SpatialReference featureClassSpatialRef =
						featureClass.GetDefinition().GetSpatialReference();

					if (! SpatialReference.AreEqual(
						    featureClassSpatialRef, shape.SpatialReference, false, true))
					{
						omitDetailedShapeSpatialRef = false;
					}
				}
				else
				{
					// TODO: Better solution: hash class ID with workspace handle in ToObjectClassMsg()
					// Make sure they are from the same workspace to avoid conflicting class ids
					Assert.AreEqual(classesByClassId[featureClass.GetID()].GetDatastore().Handle,
					                featureClass.GetDatastore().Handle,
					                "Conflicting class id from different workspaces. Please report.");
				}

				resultGdbObjects.Add(ToGdbObjectMsg(feature, shape, omitDetailedShapeSpatialRef));
			}

			_msg.DebugStopTiming(watch, "Converted {0} features to DTOs", resultGdbObjects.Count);
		}

		public static ObjectClassMsg ToObjectClassMsg([NotNull] Table objectClass,
		                                              SpatialReference spatialRef = null)
		{
			esriGeometryType geometryType = TranslateAGPShapeType(objectClass);

			string name = objectClass.GetName();
			string aliasName = objectClass.GetDefinition().GetAliasName();

			if (spatialRef == null && objectClass is FeatureClass fc)
			{
				spatialRef = fc.GetDefinition().GetSpatialReference();
			}

			ObjectClassMsg result =
				new ObjectClassMsg()
				{
					Name = name,
					Alias = aliasName,
					ClassHandle = (int) objectClass.GetID(),
					SpatialReference = ToSpatialReferenceMsg(
						spatialRef, SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml),
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
