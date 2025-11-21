using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Geodatabase;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.Shared.Ddx;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.AO
{
	public static class ProtobufGdbUtils
	{
		/// <summary>
		/// The name of the domain property of the FieldMsg that notifies the client that the
		/// respective field is the subtype field. This could be removed if the proto model is
		/// extended.
		/// </summary>
		public const string SubtypeDomainName = "__SubType__";

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
		                                          bool includeFieldValues = false,
		                                          string subFields = null)
		{
			IReadOnlyRow roRow = ReadOnlyRow.Create(featureOrObject);

			var result = ToGdbObjectMsg(roRow, includeSpatialRef,
			                            includeFieldValues, subFields);
			return result;
		}

		public static GdbObjectMsg ToGdbObjectMsg([NotNull] IDbRow featureOrRow,
		                                          bool includeSpatialRef = false,
		                                          bool includeFieldValues = false,
		                                          string subFields = null)
		{
			var result = new GdbObjectMsg();

			result.ObjectId = featureOrRow.OID;

			if (featureOrRow is IReadOnlyFeature feature)
			{
				// NOTE: Normal fields just return null if they have not been fetched due to sub-field restrictions.
				//       However, the Shape property E_FAILs.
				bool canGetShape =
					string.IsNullOrEmpty(subFields) || subFields == "*" ||
					StringUtils.Contains(subFields,
					                     ((IFeatureClass) feature.FeatureClass).ShapeFieldName,
					                     StringComparison.InvariantCultureIgnoreCase);

				if (canGetShape)
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
						ProtobufGeometryUtils.ToShapeMsg(featureShape, shapeFormat,
						                                 spatialRefFormat);
				}
			}

			IReadOnlyRow roRow = featureOrRow as IReadOnlyRow;
			Assert.NotNull(roRow, "Unsupported row type");

			IObjectClass objectClass = GetObjectClass(roRow);

			if (objectClass != null)
			{
				result.ClassHandle = objectClass.ObjectClassID;
			}

			if (includeFieldValues)
			{
				IReadOnlyList<ITableField> fields = featureOrRow.DbTable.TableFields;

				HashSet<string> requestedFields = null;
				if (! string.IsNullOrEmpty(subFields) && subFields != "*")
				{
					requestedFields = new HashSet<string>(StringUtils.SplitAndTrim(subFields, ','));
				}

				// NOTE: We want to maintain the order of the fields as they are defined by the subfields string.
				List<int> fieldsToReturn = GetFieldsIndexes(fields, requestedFields);

				foreach (int fieldIdx in fieldsToReturn)
				{
					ITableField field = fields[fieldIdx];

					if (requestedFields != null && ! requestedFields.Contains(field.Name))
					{
						continue;
					}

					object valueObject = featureOrRow.GetValue(fieldIdx);

					AttributeValue attributeValue = ToAttributeValueMsg(valueObject, field);

					result.Values.Add(attributeValue);
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

		public static ObjectClassMsg ToObjectClassMsg([NotNull] ITable table,
		                                              int classHandle,
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

			ISubtypes tableSubtypes = table as ISubtypes;

			int subtypeFieldIdx = -1;
			if (tableSubtypes != null && tableSubtypes.HasSubtype)
			{
				subtypeFieldIdx = tableSubtypes.SubtypeFieldIndex;
			}

			if (includeFields)
			{
				List<FieldMsg> fieldMessages = new List<FieldMsg>();

				IFields fields = table.Fields;

				for (int i = 0; i < fields.FieldCount; i++)
				{
					IField field = fields.Field[i];
					fieldMessages.Add(ToFieldMsg(field));
				}

				result.Fields.AddRange(fieldMessages);
			}

			// The subtype field name is needed in QaObjectAttributeConstraint
			if (subtypeFieldIdx >= 0)
			{
				result.Fields[subtypeFieldIdx].DomainName = SubtypeDomainName;
			}

			return result;
		}

		public static ObjectClassMsg ToObjectClassMsg([NotNull] Dataset dataset,
		                                              int modelId,
		                                              ISpatialReference spatialReference = null,
		                                              bool includeFields = false,
		                                              string aliasName = null)
		{
			int geometryType = (int) ProtoDataQualityUtils.GetGeometryType(dataset);

			ObjectClassMsg result =
				new ObjectClassMsg()
				{
					Name = dataset.Name,
					ClassHandle = dataset.Id,
					SpatialReference = ProtobufGeometryUtils.ToSpatialReferenceMsg(
						spatialReference,
						SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml),
					GeometryType = geometryType,
					WorkspaceHandle = -1,
					DdxModelId = modelId
				};

			if (aliasName == null)
			{
				aliasName = dataset.AliasName;
			}

			CallbackUtils.DoWithNonNull(aliasName, s => result.Alias = s);

			if (includeFields && dataset is IObjectDataset objectDataset)
			{
				List<FieldMsg> fieldMessages = new List<FieldMsg>();

				foreach (ObjectAttribute attribute in objectDataset.GetAttributes())
				{
					fieldMessages.Add(ToFieldMsg(attribute));
				}

				result.Fields.AddRange(fieldMessages);
			}

			return result;
		}

		public static ObjectClassMsg ToRelationshipClassMsg(
			[NotNull] IRelationshipClass relationshipClass)
		{
			ObjectClassMsg relTableMsg;
			if (relationshipClass.IsAttributed ||
			    relationshipClass.Cardinality == esriRelCardinality.esriRelCardinalityManyToMany)
			{
				// it's also a real table:
				var table = (ITable) relationshipClass;
				relTableMsg = ToObjectClassMsg(table, relationshipClass.RelationshipClassID, true);
			}
			else
			{
				// so far just the name is used
				relTableMsg =
					new ObjectClassMsg()
					{
						Name = DatasetUtils.GetName(relationshipClass),
						ClassHandle = relationshipClass.RelationshipClassID,
					};
			}

			IWorkspace workspace = ((IDataset) relationshipClass).Workspace;

			relTableMsg.WorkspaceHandle = workspace?.GetHashCode() ?? -1;

			return relTableMsg;
		}

		[NotNull]
		public static ConnectionMsg ToConnectionMsg([NotNull] ConnectionProvider connectionProvider)
		{
			string connectionString = null;

			if (connectionProvider is FilePathConnectionProviderBase filePathConnection)
			{
				connectionString = filePathConnection.Path;
			}
			else if (connectionProvider is SdeDirectConnectionProvider sdeDirectConnection)
			{
				connectionString = ToConnectionString(sdeDirectConnection);
			}
			else
				throw new ArgumentOutOfRangeException(
					$"Unsupported connection provider: {connectionProvider}");

			return new ConnectionMsg
			       {
				       ConnectionId = connectionProvider.Id,
				       ConnectionString = connectionString,
				       ConnectionType = (int) connectionProvider.ConnectionType,
				       Name = connectionProvider.Name
			       };
		}

		private static List<int> GetFieldsIndexes([NotNull] IReadOnlyList<ITableField> fields,
		                                          [CanBeNull] HashSet<string> fieldNames)
		{
			if (fieldNames == null)
			{
				return Enumerable.Range(0, fields.Count).ToList();
			}

			var fieldIndexesByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < fields.Count; i++)
			{
				fieldIndexesByName[fields[i].Name] = i;
			}

			return fieldNames
			       .Select(fieldName => fieldIndexesByName[fieldName])
			       .ToList();
		}

		private static IObjectClass GetObjectClass(IReadOnlyRow roRow)
		{
			// Consider moving this method to DatasetUtils or GdbObjectUtils

			IObjectClass objectClass = null;

			if (roRow is IObject obj)
			{
				objectClass = obj.Class;
			}

			if (roRow is ReadOnlyRow arcRow)
			{
				objectClass = arcRow.BaseRow.Table as IObjectClass;
			}

			return objectClass;
		}

		private static string ToConnectionString(
			[NotNull] SdeDirectConnectionProvider sdeDirectConnection)
		{
			var connectinProps = new Dictionary<string, string>();

			connectinProps["DBCLIENT"] = sdeDirectConnection.DbmsTypeName;
			connectinProps["INSTANCE"] = sdeDirectConnection.DatabaseName;

			if (sdeDirectConnection.DatabaseType == DatabaseType.SqlServer ||
			    sdeDirectConnection.DatabaseType == DatabaseType.PostgreSQL)
			{
				connectinProps["DATABASE"] = sdeDirectConnection.RepositoryName;
			}

			if (sdeDirectConnection.VersionName != null)
			{
				connectinProps["VERSION"] = sdeDirectConnection.VersionName;
			}

			if (sdeDirectConnection is SdeDirectDbUserConnectionProvider sdeDirectDbUser)
			{
				connectinProps["USER"] = sdeDirectDbUser.UserName;
				connectinProps["PASSWORD"] = sdeDirectDbUser.EncryptedPasswordValue;
			}

			return string.Join(";", connectinProps.Select(p => $"{p.Key}={p.Value}"));
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

		private static FieldMsg ToFieldMsg(ObjectAttribute field)
		{
			var result = new FieldMsg
			             {
				             Name = field.Name
			             };

			return result;
		}

		private static AttributeValue ToAttributeValueMsg([CanBeNull] object valueObject,
		                                                  [NotNull] ITableField field)
		{
			var attributeValue = new AttributeValue();

			if (valueObject == DBNull.Value || valueObject == null)
			{
				attributeValue.DbNull = true;
			}
			else
			{
				esriFieldType fieldType = (esriFieldType) field.FieldType;

				switch (fieldType)
				{
					case esriFieldType.esriFieldTypeSmallInteger:
						attributeValue.ShortIntValue = (short) valueObject;
						break;
					case esriFieldType.esriFieldTypeInteger:
						attributeValue.IntValue = (int) valueObject;
						break;
					case esriFieldType.esriFieldTypeSingle:
						attributeValue.FloatValue = (float) valueObject;
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
#if Server
						attributeValue.BigIntValue = Convert.ToInt64(valueObject);
#else
						attributeValue.IntValue = Convert.ToInt32(valueObject);
#endif
						break;
					case esriFieldType.esriFieldTypeGeometry:
						// Leave empty, it is sent through Shape property
						break;
					case esriFieldType.esriFieldTypeBlob:

						// The base row field is not officially part of the schema
						if (field.Name != InvolvedRowUtils.BaseRowField &&
						    valueObject is IMemoryBlobStreamVariant blobStream)
						{
							blobStream.ExportToVariant(out var variant);

							if (variant is byte[] bytes)
							{
								attributeValue.BlobValue =
									ByteString.CopyFrom(bytes);
							}
							else
							{
								throw new InvalidOperationException($"Unexpected variant type: {variant.GetType()}");
							}
						}

						break;
					case esriFieldType.esriFieldTypeRaster:
						// Not supported, ignore
						break;
					case esriFieldType.esriFieldTypeGUID:
						byte[] asBytes = new Guid((string) valueObject).ToByteArray();
						attributeValue.UuidValue =
							new UUID { Value = ByteString.CopyFrom(asBytes) };
						break;
					case esriFieldType.esriFieldTypeGlobalID:
						asBytes = new Guid((string) valueObject).ToByteArray();
						attributeValue.UuidValue =
							new UUID { Value = ByteString.CopyFrom(asBytes) };
						break;
					case esriFieldType.esriFieldTypeXML:
						// Not supported, ignore
						break;
#if Server
					case esriFieldType.esriFieldTypeBigInteger:
						attributeValue.BigIntValue = (long) valueObject;
						break;
#endif
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return attributeValue;
		}
	}
}
