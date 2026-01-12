using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.AO
{
	public static class ProtobufGeometryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull]
		public static ShapeMsg ToShapeMsg(
			[CanBeNull] IGeometry geometry,
			ShapeMsg.FormatOneofCase format = ShapeMsg.FormatOneofCase.EsriShape,
			SpatialReferenceMsg.FormatOneofCase spatialRefFormat =
				SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid)
		{
			if (geometry == null) return null;

			Assert.ArgumentCondition(format == ShapeMsg.FormatOneofCase.EsriShape ||
			                         format == ShapeMsg.FormatOneofCase.Wkb,
			                         "Unsupported format");

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Converting geometry {0} to shape msg",
				                 GeometryUtils.ToString(geometry));
			}

			var highLevelGeometry =
				GeometryUtils.GetHighLevelGeometry(geometry, true);

			var result = new ShapeMsg();

			if (format == ShapeMsg.FormatOneofCase.EsriShape)
			{
				result.EsriShape = ByteString.CopyFrom(
					GeometryUtils.ToEsriShapeBuffer(highLevelGeometry));
			}
			else
			{
				var wkbWriter = new WkbGeometryWriter();
				byte[] wkb = wkbWriter.WriteGeometry(highLevelGeometry);
				result.Wkb = ByteString.CopyFrom(wkb);
			}

			if (geometry.SpatialReference != null)
			{
				result.SpatialReference =
					ToSpatialReferenceMsg(geometry.SpatialReference, spatialRefFormat);
			}

			if (highLevelGeometry != geometry)
			{
				_msg.VerboseDebug(() => "Geometry was converted to high-level geometry to encode.");

				Marshal.ReleaseComObject(highLevelGeometry);
			}

			return result;
		}

		[CanBeNull]
		public static IGeometry FromShapeMsg([CanBeNull] ShapeMsg shapeBuffer,
		                                     [CanBeNull] ISpatialReference classSpatialRef = null)
		{
			if (shapeBuffer == null) return null;

			if (shapeBuffer.FormatCase == ShapeMsg.FormatOneofCase.None) return null;

			IGeometry result;

			switch (shapeBuffer.FormatCase)
			{
				case ShapeMsg.FormatOneofCase.EsriShape:

					if (shapeBuffer.EsriShape.IsEmpty) return null;

					result = GeometryUtils.FromEsriShapeBuffer(shapeBuffer.EsriShape.ToByteArray());

					break;
				case ShapeMsg.FormatOneofCase.Wkb:

					WkbGeometryReader wkbReader = new WkbGeometryReader
					                              {
						                              GroupPolyhedraByPointId = true
					                              };

					result = wkbReader.ReadGeometry(
						new MemoryStream(shapeBuffer.Wkb.ToByteArray()));

					break;
				case ShapeMsg.FormatOneofCase.Envelope:

					result = FromEnvelopeMsg(shapeBuffer.Envelope);

					break;
				default:
					throw new NotImplementedException(
						$"Unsupported format: {shapeBuffer.FormatCase}");
			}

			result.SpatialReference =
				FromSpatialReferenceMsg(shapeBuffer.SpatialReference, classSpatialRef);

			return result;
		}

		public static List<T> FromShapeMsgList<T>(
			[NotNull] ICollection<ShapeMsg> shapeBufferList,
			[CanBeNull] ISpatialReference classSpatialRef = null) where T : IGeometry
		{
			var geometryList = new List<T>(shapeBufferList.Count);

			foreach (var selectableOverlap in shapeBufferList)
			{
				T geometry = (T) FromShapeMsg(selectableOverlap, classSpatialRef);
				geometryList.Add(geometry);
			}

			return geometryList;
		}

		public static EnvelopeMsg ToEnvelopeMsg([CanBeNull] IEnvelope envelope)
		{
			if (envelope == null || envelope.IsEmpty)
			{
				return null;
			}

			var result = new EnvelopeMsg();

			if (envelope.IsEmpty)
			{
				result.XMin = double.NaN;
				result.YMin = double.NaN;
				result.XMax = double.NaN;
				result.YMax = double.NaN;
			}
			else
			{
				result.XMin = envelope.XMin;
				result.YMin = envelope.YMin;
				result.XMax = envelope.XMax;
				result.YMax = envelope.YMax;
			}

			return result;
		}

		public static IEnvelope FromEnvelopeMsg(EnvelopeMsg envProto)
		{
			if (envProto == null) return null;

			IEnvelope result = new EnvelopeClass();

			result.XMin = envProto.XMin;
			result.YMin = envProto.YMin;
			result.XMax = envProto.XMax;
			result.YMax = envProto.YMax;

			return result;
		}

		public static ISpatialReference FromSpatialReferenceMsg(
			[CanBeNull] SpatialReferenceMsg spatialRefMsg,
			[CanBeNull] ISpatialReference classSpatialRef = null)
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
						       : SpatialReferenceUtils.FromXmlString(xml);

				case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid:

					int wkId = spatialRefMsg.SpatialReferenceWkid;

					return classSpatialRef?.FactoryCode == wkId
						       ? classSpatialRef
						       : SpatialReferenceUtils.CreateSpatialReference(wkId);

				case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkt:

					return SpatialReferenceUtils.ImportFromESRISpatialReference(
						spatialRefMsg.SpatialReferenceWkt);

				default:
					throw new NotImplementedException(
						$"Unsupported spatial reference format: {spatialRefMsg.FormatCase}");
			}
		}

		public static SpatialReferenceMsg ToSpatialReferenceMsg(
			[CanBeNull] ISpatialReference spatialReference,
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
					result.SpatialReferenceEsriXml = SpatialReferenceUtils.ToXmlString(
						spatialReference);
					break;
				case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid:
					result.SpatialReferenceWkid = spatialReference.FactoryCode;
					break;
				case SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkt:
					result.SpatialReferenceWkt =
						SpatialReferenceUtils.ExportToESRISpatialReference(spatialReference);
					break;
				default:
					throw new NotImplementedException(
						$"Unsupported spatial reference format: {format}");
			}

			return result;
		}
	}
}
