using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.QA;
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

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Converting geometry {0} to shape msg",
				                 GeometryUtils.ToString(geometry));
			}

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
			[NotNull] IObjectClass objectClass, bool includeFields = false)
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

			if (includeFields)
			{
				List<FieldMsg> fieldMessages = new List<FieldMsg>();

				for (int i = 0; i < objectClass.Fields.FieldCount; i++)
				{
					IField field = objectClass.Fields.Field[i];

					fieldMessages.Add(ToFieldMsg(field));
				}

				result.Fields.AddRange(fieldMessages);
			}

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
			GdbTableContainer container = CreateGdbTableContainer(objectClassMessages, null, out _);

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
			[NotNull] IEnumerable<ObjectClassMsg> objectClassMessages,
			[CanBeNull] Func<DataVerificationResponse, DataVerificationRequest> getRemoteDataFunc,
			out GdbWorkspace workspace)
		{
			GdbTableContainer container = null;
			int? workspaceHandle = null;
			workspace = null;

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

				Func<ITable, BackingDataset> createBackingDataset = null;

				if (getRemoteDataFunc != null)
				{
					createBackingDataset = (t) =>
						new RemoteDataset(t, getRemoteDataFunc,
						                  new ClassDef
						                  {
							                  ClassHandle = objectClassMsg.ClassHandle,
							                  WorkspaceHandle = objectClassMsg.WorkspaceHandle
						                  });
				}

				GdbTable gdbTable =
					FromObjectClassMsg(objectClassMsg, workspace, createBackingDataset);

				container.TryAdd(gdbTable);
			}

			return container;
		}

		public static IList<GdbWorkspace> CreateSchema(
			[NotNull] IEnumerable<ObjectClassMsg> objectClassMessages,
			[CanBeNull] ICollection<ObjectClassMsg> relClassMessages = null,
			Func<DataVerificationResponse, DataVerificationRequest> moreDataRequest = null)
		{
			var result = new List<GdbWorkspace>();
			foreach (IGrouping<int, ObjectClassMsg> classGroup in objectClassMessages.GroupBy(
				c => c.WorkspaceHandle))
			{
				GdbTableContainer gdbTableContainer =
					CreateGdbTableContainer(classGroup, moreDataRequest,
					                        out GdbWorkspace gdbWorkspace);

				result.Add(gdbWorkspace);

				if (relClassMessages == null)
				{
					continue;
				}

				foreach (ObjectClassMsg relTableMsg
					in relClassMessages.Where(
						r => r.WorkspaceHandle == gdbWorkspace.WorkspaceHandle))
				{
					GdbTable relClassTable = FromObjectClassMsg(relTableMsg, gdbWorkspace);

					gdbTableContainer.TryAddRelationshipClass(relClassTable);
				}
			}

			return result;
		}

		public static GdbTable FromObjectClassMsg(
			[NotNull] ObjectClassMsg objectClassMsg,
			[CanBeNull] IWorkspace workspace,
			[CanBeNull] Func<ITable, BackingDataset> createBackingDataset = null)
		{
			esriGeometryType geometryType = (esriGeometryType) objectClassMsg.GeometryType;

			GdbTable result;
			if (geometryType == esriGeometryType.esriGeometryNull)
			{
				result = new GdbTable(objectClassMsg.ClassHandle,
				                      objectClassMsg.Name, objectClassMsg.Alias,
				                      createBackingDataset, workspace);
			}
			else
			{
				result = new GdbFeatureClass(
					         objectClassMsg.ClassHandle,
					         objectClassMsg.Name,
					         (esriGeometryType) objectClassMsg.GeometryType,
					         objectClassMsg.Alias,
					         createBackingDataset, workspace)
				         {
					         SpatialReference =
						         FromSpatialReferenceMsg(objectClassMsg.SpatialReference)
				         };
			}

			if (objectClassMsg.Fields == null || objectClassMsg.Fields.Count <= 0)
			{
				return result;
			}

			foreach (FieldMsg fieldMsg in objectClassMsg.Fields)
			{
				IField field = FieldUtils.CreateField(fieldMsg.Name,
				                                      (esriFieldType) fieldMsg.Type,
				                                      fieldMsg.AliasName);

				if (field.Type == esriFieldType.esriFieldTypeString)
				{
					((IFieldEdit) field).Length_2 = fieldMsg.Length;
				}
				else if (field.Type == esriFieldType.esriFieldTypeGeometry)
				{
					var sr = FromSpatialReferenceMsg(objectClassMsg.SpatialReference);
					field = FieldUtils.CreateShapeField(geometryType, sr, 1000, true, false);
				}

				if (result.Fields.FindField(field.Name) < 0)
				{
					result.AddField(field);
				}
				else
				{
					_msg.DebugFormat("Field {0} is duplicate or has been added previously",
					                 field.Name);
				}
			}

			return result;
		}

		public static GdbFeature FromGdbFeatureMsg(
			[NotNull] GdbObjectMsg gdbObjectMsg,
			[NotNull] GdbTableContainer tableContainer)
		{
			var featureClass =
				(IFeatureClass) tableContainer.GetByClassId(gdbObjectMsg.ClassHandle);

			GdbFeature result = CreateGdbFeature(gdbObjectMsg, featureClass);

			return result;
		}

		public static GdbRow FromGdbObjectMsg(
			[NotNull] GdbObjectMsg gdbObjectMsg,
			[NotNull] ITable table)
		{
			GdbRow result;
			if (table is IFeatureClass featureClass)
			{
				result = CreateGdbFeature(gdbObjectMsg, featureClass);
			}
			else
			{
				result = new GdbRow(gdbObjectMsg.ObjectId, (IObjectClass) table);
			}

			ReadMsgValues(gdbObjectMsg, result, table);

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

		private static GdbFeature CreateGdbFeature(GdbObjectMsg gdbObjectMsg,
		                                           IFeatureClass featureClass)
		{
			ISpatialReference classSpatialRef = DatasetUtils.GetSpatialReference(featureClass);

			IGeometry shape = FromShapeMsg(gdbObjectMsg.Shape, classSpatialRef);

			var result = new GdbFeature(gdbObjectMsg.ObjectId, featureClass)
			             {
				             Shape = shape
			             };

			return result;
		}

		private static void ReadMsgValues(GdbObjectMsg gdbObjectMsg, GdbRow intoResult,
		                                  ITable table)
		{
			if (gdbObjectMsg.Values.Count == 0)
			{
				return;
			}

			Assert.AreEqual(table.Fields.FieldCount, gdbObjectMsg.Values.Count,
			                "GdbObject message values do not correspond to table schema");

			for (var index = 0; index < gdbObjectMsg.Values.Count; index++)
			{
				AttributeValue attributeValue = gdbObjectMsg.Values[index];

				switch (attributeValue.ValueCase)
				{
					case AttributeValue.ValueOneofCase.None:
						break;
					case AttributeValue.ValueOneofCase.DbNull:
						intoResult.set_Value(index, DBNull.Value);
						break;
					case AttributeValue.ValueOneofCase.ShortIntValue:
						intoResult.set_Value(index, attributeValue.ShortIntValue);
						break;
					case AttributeValue.ValueOneofCase.LongIntValue:
						intoResult.set_Value(index, attributeValue.LongIntValue);
						break;
					case AttributeValue.ValueOneofCase.FloatValue:
						intoResult.set_Value(index, attributeValue.FloatValue);
						break;
					case AttributeValue.ValueOneofCase.DoubleValue:
						intoResult.set_Value(index, attributeValue.DoubleValue);
						break;
					case AttributeValue.ValueOneofCase.StringValue:
						intoResult.set_Value(index, attributeValue.StringValue);
						break;
					case AttributeValue.ValueOneofCase.DateTimeTicksValue:
						intoResult.set_Value(
							index, new DateTime(attributeValue.DateTimeTicksValue));
						break;
					case AttributeValue.ValueOneofCase.UuidValue:
						var guid = new Guid(attributeValue.UuidValue.Value.ToByteArray());
						IUID uid = UIDUtils.CreateUID(guid);
						intoResult.set_Value(index, uid);
						break;
					case AttributeValue.ValueOneofCase.BlobValue:
						intoResult.set_Value(index, attributeValue.BlobValue);
						break;
					default:
						if (table.Fields.Field[index].Type == esriFieldType.esriFieldTypeGeometry)
						{
							// Leave empty, it is already assigned to the Shape property
							break;
						}

						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private static FieldMsg ToFieldMsg(IField field)
		{
			var result = new FieldMsg
			             {
				             Name = field.Name,
				             AliasName = field.AliasName,
				             Type = (int) field.Type,
				             Length = field.Length,
				             Precision = field.Precision,
				             Scale = field.Scale,
				             IsNullable = field.IsNullable,
				             IsEditable = field.Editable
			             };

			if (field.Domain?.Name != null)
			{
				result.DomainName = field.Domain.Name;
			}

			return result;
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
