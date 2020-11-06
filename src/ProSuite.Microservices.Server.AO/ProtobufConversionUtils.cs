using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.Shared;
using ProSuite.Microservices.Server.AO.Geodatabase;

namespace ProSuite.Microservices.Server.AO
{
	public static class ProtobufConversionUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[CanBeNull]
		public static IGeometry FromShapeMsg([CanBeNull] ShapeMsg shapeBuffer,
		                                     [CanBeNull] ISpatialReference classSpatialRef = null)
		{
			if (shapeBuffer == null) return null;

			IGeometry result;

			switch (shapeBuffer.FormatCase)
			{
				case ShapeMsg.FormatOneofCase.EsriShape:

					if (shapeBuffer.EsriShape.IsEmpty) return null;

					result = GeometryUtils.FromEsriShapeBuffer(shapeBuffer.EsriShape.ToByteArray());

					break;
				case ShapeMsg.FormatOneofCase.Wkb:

					WkbGeometryReader wkbReader = new WkbGeometryReader();
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

		[CanBeNull]
		public static ShapeMsg ToShapeMsg(
			[CanBeNull] IGeometry geometry,
			ShapeMsg.FormatOneofCase format = ShapeMsg.FormatOneofCase.EsriShape,
			SpatialReferenceMsg.FormatOneofCase spatialRefFormat =
				SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid)
		{
			if (geometry == null) return null;

			Assert.ArgumentCondition(format == ShapeMsg.FormatOneofCase.EsriShape,
			                         "Unsupported format");

			var highLevelGeometry =
				GeometryUtils.GetHighLevelGeometry(geometry, true);

			var result = new ShapeMsg
			             {
				             EsriShape =
					             ByteString.CopyFrom(
						             GeometryUtils.ToEsriShapeBuffer(highLevelGeometry))
			             };

			if (geometry.SpatialReference != null)
			{
				result.SpatialReference =
					ToSpatialReferenceMsg(geometry.SpatialReference, spatialRefFormat);
			}

			if (highLevelGeometry != geometry)
			{
				_msg.DebugFormat(
					"Geometry was converted to high-level geometry to encode.");

				Marshal.ReleaseComObject(highLevelGeometry);
			}

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

		[CanBeNull]
		public static IList<IPoint> PointsFromShapeProtoBuffer(ShapeMsg shapeBuffer)
		{
			var geometry = FromShapeMsg(shapeBuffer);

			if (geometry == null) return null;

			return GeometryUtils.GetPoints(geometry).ToList();
		}

		public static GdbObjectMsg ToGdbObjectMsg(IObject featureOrObject,
		                                          [CanBeNull] IGeometry geometry,
		                                          int objectClassHandle)
		{
			var result = new GdbObjectMsg();

			result.ClassHandle = objectClassHandle;

			result.ObjectId = featureOrObject.OID;

			result.Shape = ToShapeMsg(geometry);

			return result;
		}

		public static GdbObjectMsg ToGdbObjectMsg([NotNull] IFeature gdbFeature)
		{
			int classHandle = gdbFeature.Class.ObjectClassID;
			return ToGdbObjectMsg(gdbFeature, gdbFeature.Shape, classHandle);
		}

		public static void ToGdbObjectMsgList(
			IEnumerable<IFeature> features,
			ICollection<GdbObjectMsg> resultGdbObjects,
			HashSet<ObjectClassMsg> resultGdbClasses)
		{
			foreach (IFeature feature in features)
			{
				resultGdbObjects.Add(ToGdbObjectMsg(feature));
				resultGdbClasses.Add(ToObjectClassMsg(feature.Class));
			}
		}

		public static ObjectClassMsg ToObjectClassMsg(
			[NotNull] IObjectClass objectClass)
		{
			esriGeometryType geometryType = esriGeometryType.esriGeometryNull;
			ISpatialReference spatialRef = null;

			if (objectClass is IFeatureClass fc)
			{
				geometryType = fc.ShapeType;
				spatialRef = DatasetUtils.GetSpatialReference(fc);
			}

			IWorkspace workspace = ((IDataset) objectClass).Workspace;

			ObjectClassMsg result =
				new ObjectClassMsg()
				{
					Name = DatasetUtils.GetName(objectClass),
					Alias = DatasetUtils.GetAliasName(objectClass),
					ClassHandle = objectClass.ObjectClassID,
					SpatialReference = ToSpatialReferenceMsg(
						spatialRef, SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml),
					GeometryType = (int) geometryType,
					WorkspaceHandle = workspace?.GetHashCode() ?? -1
				};

			return result;
		}

		/// <summary>
		/// Converts a list of features which are assumed to come from a single
		/// workspace, i.e. all their object class IDs are unique within their
		/// workspace.
		/// </summary>
		/// <param name="gdbObjectMessages"></param>
		/// <param name="objectClassMessages"></param>
		/// <returns></returns>
		public static IList<IFeature> FromGdbObjectMsgList(
			[NotNull] ICollection<GdbObjectMsg> gdbObjectMessages,
			[NotNull] ICollection<ObjectClassMsg> objectClassMessages)
		{
			GdbTableContainer container = CreateGdbTableContainer(objectClassMessages);

			return FromGdbObjectMsgList(gdbObjectMessages, container);
		}

		public static IList<IFeature> FromGdbObjectMsgList(
			[NotNull] ICollection<GdbObjectMsg> gdbObjectMessages,
			[NotNull] GdbTableContainer container)
		{
			var result = new List<IFeature>(gdbObjectMessages.Count);

			Assert.NotNull(container, "No object class provided");

			foreach (GdbObjectMsg gdbObjectMsg in gdbObjectMessages)
			{
				GdbFeature remoteFeature = FromGdbFeatureMsg(gdbObjectMsg, container);

				result.Add(remoteFeature);
			}

			return result;
		}

		public static GdbTableContainer CreateGdbTableContainer(
			ICollection<ObjectClassMsg> objectClassMessages)
		{
			GdbTableContainer container = null;
			int? workspaceHandle = null;
			IWorkspace workspace = null;

			foreach (ObjectClassMsg objectClassMsg in objectClassMessages)
			{
				if (workspaceHandle == null)
				{
					workspaceHandle = objectClassMsg.WorkspaceHandle;

					container = new GdbTableContainer();

					workspace = new GdbWorkspace(container)
					            {
						            WorkspaceHandle = objectClassMsg.WorkspaceHandle
					            };
				}
				else
				{
					Assert.AreEqual(workspaceHandle, objectClassMsg.WorkspaceHandle,
					                "Not all features are from the same workspace");
				}

				if (objectClassMsg.WorkspaceHandle == -1)
				{
					workspace = null;
				}

				GdbFeatureClass fClass = FromFeatureClassMsg(objectClassMsg, workspace);

				container?.TryAdd(fClass);
			}

			return container;
		}

		private static GdbFeatureClass FromFeatureClassMsg(ObjectClassMsg objectClassMsg,
		                                                   IWorkspace workspace)
		{
			var result = new GdbFeatureClass(
				objectClassMsg.ClassHandle,
				objectClassMsg.Name,
				(esriGeometryType) objectClassMsg.GeometryType,
				objectClassMsg.Alias,
				null, workspace);

			result.SpatialReference = FromSpatialReferenceMsg(objectClassMsg.SpatialReference);

			return result;
		}

		public static GdbFeature FromGdbFeatureMsg(
			[NotNull] GdbObjectMsg gdbObjectMsg,
			[NotNull] GdbTableContainer tableContainer)
		{
			var gdbTable = (IFeatureClass) tableContainer.GetByClassId(gdbObjectMsg.ClassHandle);

			ISpatialReference classSpatialRef = DatasetUtils.GetSpatialReference(gdbTable);

			IGeometry shape = FromShapeMsg(gdbObjectMsg.Shape, classSpatialRef);

			var result = new GdbFeature(gdbObjectMsg.ObjectId, gdbTable)
			             {
				             Shape = shape
			             };

			return result;
		}

		public static GdbObjRefMsg ToGdbObjRefMsg(IFeature feature)
		{
			return new GdbObjRefMsg
			       {
				       ClassHandle = feature.Class.ObjectClassID,
				       ObjectId = feature.OID
			       };
		}

		public static GdbObjRefMsg ToGdbObjRefMsg(GdbObjectReference gdbObjectReference)
		{
			return new GdbObjRefMsg
			       {
				       ClassHandle = gdbObjectReference.ClassId,
				       ObjectId = gdbObjectReference.ObjectId
			       };
		}

		/// <summary>
		/// Return null, if the specified string is empty (i.e. the default value for string
		/// protocol buffers), or the input string otherwise.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string EmptyToNull(string value)
		{
			return string.IsNullOrEmpty(value) ? null : value;
		}
	}
}
