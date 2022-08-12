using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.AO
{
	public static class ProtobufGdbUtils
	{
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

		public static GdbObjectMsg ToGdbObjectMsg(
			[NotNull] IObject featureOrObject,
			[CanBeNull] IGeometry geometry,
			int objectClassHandle,
			SpatialReferenceMsg.FormatOneofCase spatialRefFormat =
				SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid)
		{
			var result = new GdbObjectMsg();

			result.ClassHandle = objectClassHandle;

			result.ObjectId = featureOrObject.OID;

			ShapeMsg.FormatOneofCase format =
				geometry?.GeometryType == esriGeometryType.esriGeometryMultiPatch
					? ShapeMsg.FormatOneofCase.Wkb
					: ShapeMsg.FormatOneofCase.EsriShape;

			result.Shape = ProtobufGeometryUtils.ToShapeMsg(geometry, format, spatialRefFormat);

			return result;
		}

		public static GdbObjectMsg ToGdbObjectMsg([NotNull] IObject featureOrObject,
		                                          bool includeSpatialRef = false,
		                                          bool includeFieldValues = false)
		{
			var result = new GdbObjectMsg();

			result.ClassHandle = featureOrObject.Class.ObjectClassID;

			result.ObjectId = featureOrObject.OID;

			if (featureOrObject is IFeature feature)
			{
				IGeometry featureShape = GdbObjectUtils.GetFeatureShape(feature);

				ShapeMsg.FormatOneofCase shapeFormat =
					featureShape?.GeometryType == esriGeometryType.esriGeometryMultiPatch
						? ShapeMsg.FormatOneofCase.Wkb
						: ShapeMsg.FormatOneofCase.EsriShape;

				SpatialReferenceMsg.FormatOneofCase spatialRefFormat = includeSpatialRef
					? SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml
					: SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid;

				result.Shape =
					ProtobufGeometryUtils.ToShapeMsg(featureShape, shapeFormat, spatialRefFormat);
			}

			if (includeFieldValues)
			{
				for (int i = 0; i < featureOrObject.Fields.FieldCount; i++)
				{
					IField field = featureOrObject.Fields.Field[i];

					object valueObject = featureOrObject.Value[i];

					var attributeValue = new AttributeValue();

					result.Values.Add(attributeValue);

					if (valueObject == DBNull.Value || valueObject == null)
					{
						attributeValue.DbNull = true;
					}
					else
					{
						switch (field.Type)
						{
							case esriFieldType.esriFieldTypeSmallInteger:
								attributeValue.ShortIntValue = (int) valueObject;
								break;
							case esriFieldType.esriFieldTypeInteger:
								attributeValue.LongIntValue = (int) valueObject;
								break;
							case esriFieldType.esriFieldTypeSingle:
								attributeValue.ShortIntValue = (int) valueObject;
								break;
							case esriFieldType.esriFieldTypeDouble:
								attributeValue.DoubleValue = (double) valueObject;
								break;
							case esriFieldType.esriFieldTypeString:
								attributeValue.StringValue = (string) valueObject;
								break;
							case esriFieldType.esriFieldTypeDate:
								attributeValue.DateTimeTicksValue = ((DateTime) valueObject).Ticks;
								break;
							case esriFieldType.esriFieldTypeOID:
								attributeValue.ShortIntValue = (int) valueObject;
								break;
							case esriFieldType.esriFieldTypeGeometry:
								// Leave empty, it is sent through Shape property
								break;
							case esriFieldType.esriFieldTypeBlob:
								// TODO: Test and make this work
								attributeValue.BlobValue =
									ByteString.CopyFrom((byte[]) valueObject);
								break;
							case esriFieldType.esriFieldTypeRaster:
								// Not supported, ignore
								break;
							case esriFieldType.esriFieldTypeGUID:
								byte[] asBytes = new Guid((string) valueObject).ToByteArray();
								attributeValue.UuidValue =
									new UUID {Value = ByteString.CopyFrom(asBytes)};
								break;
							case esriFieldType.esriFieldTypeGlobalID:
								asBytes = new Guid((string) valueObject).ToByteArray();
								attributeValue.UuidValue =
									new UUID {Value = ByteString.CopyFrom(asBytes)};
								break;
							case esriFieldType.esriFieldTypeXML:
								// Not supported, ignore
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
				}
			}

			return result;
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
			return ToObjectClassMsg((ITable) objectClass, objectClass.ObjectClassID, includeFields);
		}

		public static ObjectClassMsg ToObjectClassMsg([NotNull] ITable table, int classHandle,
		                                              bool includeFields = false,
		                                              string aliasName = null)
		{
			esriGeometryType geometryType = esriGeometryType.esriGeometryNull;
			ISpatialReference spatialRef = null;

			if (table is IFeatureClass fc)
			{
				geometryType = fc.ShapeType;
				spatialRef = DatasetUtils.GetSpatialReference(fc);
			}

			IWorkspace workspace = ((IDataset) table).Workspace;

			ObjectClassMsg result =
				new ObjectClassMsg()
				{
					Name = DatasetUtils.GetName(table),
					ClassHandle = classHandle,
					SpatialReference = ProtobufGeometryUtils.ToSpatialReferenceMsg(
						spatialRef, SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml),
					GeometryType = (int) geometryType,
					WorkspaceHandle = workspace?.GetHashCode() ?? -1
				};

			if (aliasName == null)
			{
				aliasName = DatasetUtils.GetAliasName((IObjectClass) table);
			}

			CallbackUtils.DoWithNonNull(aliasName, s => result.Alias = s);

			if (includeFields)
			{
				List<FieldMsg> fieldMessages = new List<FieldMsg>();

				for (int i = 0; i < table.Fields.FieldCount; i++)
				{
					IField field = table.Fields.Field[i];

					fieldMessages.Add(ToFieldMsg(field));
				}

				result.Fields.AddRange(fieldMessages);
			}

			return result;
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
	}
}
