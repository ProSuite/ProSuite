using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using Google.Protobuf;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.API;
using ProSuite.Microservices.Client;
using ProSuite.Microservices.Definitions.Shared.Commons;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using FieldType = ArcGIS.Core.Data.FieldType;

namespace ProSuite.Microservices.Client.AGP.DataProvider
{
	/// <summary>
	/// Utility class for converting between GDB objects and protobuf message types.
	/// </summary>
	public static class GdbObjectConversionUtils
	{
		// TODO: Consolidate and remove duplication with ProtobufConversionUtils
		//       after performance optimizations are completed.

		/// <summary>
		/// Converts an IObjectClass to an ObjectClassMsg protobuf message.
		/// </summary>
		/// <param name="table">The object class to convert.</param>
		/// <param name="includeFields">Whether to include field definitions.</param>
		/// <param name="aliasName">Optional alias name to use.</param>
		/// <returns>The converted ObjectClassMsg.</returns>
		[NotNull]
		public static ObjectClassMsg ToObjectClassMsg([NotNull] IObjectClass table,
		                                              bool includeFields = false,
		                                              string aliasName = null)
		{
			esriGeometryType geometryType = esriGeometryType.esriGeometryNull;
			SpatialReference spatialRef = null;

			if (table is IFeatureClass fc)
			{
				geometryType = fc.ShapeType;
				spatialRef = fc.SpatialReference?.NativeImplementation as SpatialReference;
			}

			IWorkspace workspace = table.Workspace;

			ObjectClassMsg result =
				new ObjectClassMsg
				{
					Name = table.Name,
					ClassHandle = table.ObjectClassID,
					SpatialReference = ProtobufConversionUtils.ToSpatialReferenceMsg(
						spatialRef, SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml),
					GeometryType = (int) geometryType,
					WorkspaceHandle = workspace?.GetHashCode() ?? -1,
					WorkspaceDbType = workspace?.NativeImplementation is Datastore datastore
						                  ? (int) WorkspaceUtils.GetWorkspaceDbType(datastore)
						                  : 0
				};

			if (aliasName == null)
			{
				aliasName = table.AliasName;
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
					IField field = fields[i];
					fieldMessages.Add(ToFieldMsg(field));
				}

				result.Fields.AddRange(fieldMessages);
			}

			// The subtype field name is needed in QaObjectAttributeConstraint
			if (subtypeFieldIdx >= 0)
			{
				result.Fields[subtypeFieldIdx].DomainName = ProtobufGeoDbUtils.SubtypeDomainName;
			}

			return result;
		}

		/// <summary>
		/// Converts an IField to a FieldMsg protobuf message.
		/// </summary>
		/// <param name="field">The field to convert.</param>
		/// <returns>The converted FieldMsg.</returns>
		[NotNull]
		public static FieldMsg ToFieldMsg([NotNull] IField field)
		{
			var result = new FieldMsg
			             {
				             Name = field.Name,
				             AliasName = field.AliasName ?? string.Empty,
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
		/// Converts a Row to a GdbObjectMsg protobuf message.
		/// </summary>
		/// <param name="featureOrRow">The row or feature to convert.</param>
		/// <param name="readShape">Whether to read geometry data.</param>
		/// <param name="includeSpatialRef">Whether to include spatial reference in geometry.</param>
		/// <param name="fieldsToReturn">Optional list of field indexes to include.</param>
		/// <param name="allFields">All fields in the table (required if fieldsToReturn is specified).</param>
		/// <returns>The converted GdbObjectMsg.</returns>
		[NotNull]
		public static GdbObjectMsg ToGdbObjectMsg([NotNull] Row featureOrRow,
		                                          bool readShape,
		                                          bool includeSpatialRef,
		                                          [CanBeNull] List<int> fieldsToReturn,
		                                          [CanBeNull] IReadOnlyList<Field> allFields)
		{
			var result = new GdbObjectMsg();

			result.ObjectId = featureOrRow.GetObjectID();

			if (featureOrRow is Feature feature)
			{
				if (readShape)
				{
					Geometry featureShape = feature.GetShape();

					result.Shape =
						ProtobufConversionUtils.ToShapeMsg(featureShape, ! includeSpatialRef);
				}
			}

			Table table = featureOrRow.GetTable();

			result.ClassHandle = GeometryProcessingUtils.GetUniqueClassId(table);

			if (fieldsToReturn != null)
			{
				Assert.NotNull(allFields);

				foreach (int fieldIdx in fieldsToReturn)
				{
					Field field = allFields[fieldIdx];

					if (field.FieldType == FieldType.Geometry)
					{
						// Fetched above into separate Shape property -> Add dummy
						result.Values.Add(new AttributeValue());

						continue;
					}

					object valueObject = featureOrRow[fieldIdx];

					AttributeValue attributeValue =
						ProtobufConversionUtils.ToAttributeValueMsg(valueObject, field);

					result.Values.Add(attributeValue);
				}

				return result;
			}

			return result;
		}


		/// <summary>
		/// Creates a ClassDef from an ObjectClassMsg.
		/// </summary>
		/// <param name="objectClassMsg">The ObjectClassMsg to convert.</param>
		/// <returns>The created ClassDef.</returns>
		[NotNull]
		public static ClassDef CreateClassDef([NotNull] ObjectClassMsg objectClassMsg)
		{
			// The server follows the same rule in ProtobufConversionUtils.GetContainerHandle
			// (ProSuite.Microservices.Server.AO): the workspace handle is the data dictionary
			// model ID, or the workspace handle of the message if the model ID is 0.
			return new ClassDef
			       {
				       ClassHandle = objectClassMsg.ClassHandle,
				       WorkspaceHandle = objectClassMsg.DdxModelId != 0
					                         ? objectClassMsg.DdxModelId
					                         : objectClassMsg.WorkspaceHandle
			       };
		}

		/// <summary>
		/// Gets field indexes from field definitions based on requested field names.
		/// </summary>
		/// <param name="fields">All available fields.</param>
		/// <param name="fieldNames">Requested field names (null for all fields).</param>
		/// <returns>List of field indexes in the order of requested field names.</returns>
		[NotNull]
		public static List<int> GetFieldsIndexes([NotNull] IReadOnlyList<Field> fields,
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
			       .Select(fieldName => GetFieldIndex(fieldName, fieldIndexesByName))
			       .ToList();
		}

		private static int GetFieldIndex(
			[NotNull] string fieldName,
			[NotNull] IReadOnlyDictionary<string, int> fieldIndexesByName)
		{
			if (fieldIndexesByName.TryGetValue(fieldName, out int fieldIndex))
			{
				return fieldIndex;
			}

			throw new KeyNotFoundException(
				$"The field '{fieldName}' was not found. Available fields: " +
				$"{string.Join(", ", fieldIndexesByName.Keys)}");
		}
	}
}
