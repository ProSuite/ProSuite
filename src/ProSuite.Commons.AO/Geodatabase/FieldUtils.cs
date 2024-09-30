using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using FieldType = ProSuite.Commons.GeoDb.FieldType;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class FieldUtils
	{
		[NotNull]
		public static IFieldsEdit CreateFields(params IField[] fields)
		{
			return CreateFields((IEnumerable<IField>) fields);
		}

		[NotNull]
		public static IFieldsEdit CreateFields([NotNull] IEnumerable<IField> fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			IFieldsEdit result = new FieldsClass();

			foreach (IField field in fields)
			{
				result.AddField(field);
			}

			return result;
		}

		/// <summary>
		/// Creates an Object ID field.
		/// </summary>
		/// <param name="oidFieldName">Name of the oid field.</param>
		/// <param name="aliasName">Alias name for the field.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateOIDField([NotNull] string oidFieldName = "OBJECTID",
		                                    [CanBeNull] string aliasName = null)
		{
			Assert.ArgumentNotNullOrEmpty(oidFieldName, nameof(oidFieldName));

			IFieldEdit field = new FieldClass();

			field.IsNullable_2 = false;
			field.Editable_2 = false;
			field.Name_2 = oidFieldName;
			field.Required_2 = true;
			field.Type_2 = esriFieldType.esriFieldTypeOID;

			if (! string.IsNullOrEmpty(aliasName))
			{
				field.AliasName_2 = aliasName;
			}

			return field;
		}

		/// <summary>
		/// Creates a shape field.
		/// </summary>
		/// <param name="geometryType">Type of the geometry.</param>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <param name="gridSize">Spatial index grid size.</param>
		/// <param name="hasZ">if set to <c>true</c> the geometries will be z-aware.</param>
		/// <param name="hasM">if set to <c>true</c> the geometries will be m-aware.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateShapeField(esriGeometryType geometryType,
		                                      [NotNull] ISpatialReference spatialReference,
		                                      double gridSize = 0,
		                                      bool hasZ = false,
		                                      bool hasM = false)
		{
			if (geometryType == esriGeometryType.esriGeometryMultiPatch && ! hasZ)
			{
				throw new ArgumentException("Multipatch geometries must have Z values.");
			}

			return CreateShapeField(GetShapeFieldName(), geometryType, spatialReference,
			                        gridSize, hasZ, hasM);
		}

		/// <summary>
		/// Creates a shape field.
		/// </summary>
		/// <param name="fieldName">Name of the shape field.</param>
		/// <param name="geometryType">Type of the geometry.</param>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <param name="gridSize1">Spatial index grid size.</param>
		/// <param name="hasZ">if set to <c>true</c> the shape field will be z-aware.</param>
		/// <param name="hasM">if set to <c>true</c> the shape field will be m-aware.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateShapeField([NotNull] string fieldName,
		                                      esriGeometryType geometryType,
		                                      [NotNull] ISpatialReference spatialReference,
		                                      double gridSize1 = 0,
		                                      bool hasZ = false,
		                                      bool hasM = false)
		{
			return CreateShapeField(fieldName, geometryType, spatialReference, gridSize1, 0, 0,
			                        hasZ, hasM);
		}

		/// <summary>
		/// Creates a shape field, using the default field name.
		/// </summary>
		/// <param name="geometryType">Type of the geometry.</param>
		/// <param name="spatialReference">The spatial reference.</param>
		/// <param name="gridSize1">The grid size1.</param>
		/// <param name="gridSize2">The grid size2.</param>
		/// <param name="gridSize3">The grid size3.</param>
		/// <param name="hasZ">if set to <c>true</c> the shape field will be z-aware.</param>
		/// <param name="hasM">if set to <c>true</c> the shape field will be m-aware.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateShapeField(esriGeometryType geometryType,
		                                      [NotNull] ISpatialReference spatialReference,
		                                      double gridSize1,
		                                      double gridSize2,
		                                      double gridSize3,
		                                      bool hasZ = false,
		                                      bool hasM = false)
		{
			return CreateShapeField(GetShapeFieldName(), geometryType, spatialReference,
			                        gridSize1, gridSize2, gridSize3, hasZ, hasM);
		}

		[NotNull]
		public static IField CreateShapeField([NotNull] string fieldName,
		                                      esriGeometryType geometryType,
		                                      [NotNull] ISpatialReference spatialReference,
		                                      double gridSize1,
		                                      double gridSize2,
		                                      double gridSize3,
		                                      bool hasZ = false,
		                                      bool hasM = false)
		{
			Assert.ArgumentNotNull(fieldName, nameof(fieldName));
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			IFieldEdit fieldShape = new FieldClass();

			fieldShape.Type_2 = esriFieldType.esriFieldTypeGeometry;
			fieldShape.Name_2 = fieldName;

			fieldShape.GeometryDef_2 = CreateGeometryDef(geometryType, spatialReference,
			                                             gridSize1, gridSize2, gridSize3,
			                                             hasZ, hasM);

			return fieldShape;
		}

		/// <summary>
		/// Creates a text field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="size">Size of the text field.</param>
		/// <param name="aliasName">Alias name for the field (optional).</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateTextField([NotNull] string fieldName,
		                                     int size,
		                                     [CanBeNull] string aliasName = null)
		{
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			IFieldEdit field = new FieldClass();

			field.Name_2 = fieldName;
			field.Type_2 = esriFieldType.esriFieldTypeString;
			field.Length_2 = size;

			if (! string.IsNullOrEmpty(aliasName))
			{
				field.AliasName_2 = aliasName;
			}

			return field;
		}

		/// <summary>
		/// Creates a date/time field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="aliasName">Name of the alias.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateDateField([NotNull] string fieldName,
		                                     [CanBeNull] string aliasName = null)
		{
			return CreateField(fieldName, esriFieldType.esriFieldTypeDate, aliasName);
		}

		/// <summary>
		/// Creates an integer field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="aliasName">Name of the alias.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateIntegerField([NotNull] string fieldName,
		                                        [CanBeNull] string aliasName = null)
		{
			return CreateField(fieldName, esriFieldType.esriFieldTypeInteger, aliasName);
		}

		/// <summary>
		/// Creates a small integer field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateSmallIntegerField([NotNull] string fieldName)
		{
			return CreateSmallIntegerField(fieldName, fieldName);
		}

		/// <summary>
		/// Creates a small integer field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="aliasName">Name of the alias.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateSmallIntegerField([NotNull] string fieldName,
		                                             [CanBeNull] string aliasName)
		{
			return CreateField(fieldName, esriFieldType.esriFieldTypeSmallInteger, aliasName);
		}

		/// <summary>
		/// Creates a double field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="aliasName">Name of the alias.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateDoubleField([NotNull] string fieldName,
		                                       [CanBeNull] string aliasName = null)
		{
			return CreateField(fieldName, esriFieldType.esriFieldTypeDouble, aliasName);
		}

		/// <summary>
		/// Creates a single field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="aliasName">Name of the alias.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateSingleField([NotNull] string fieldName,
		                                       [CanBeNull] string aliasName = null)
		{
			return CreateField(fieldName, esriFieldType.esriFieldTypeSingle, aliasName);
		}

		/// <summary>
		/// Creates a GUID field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="aliasName">Name of the alias.</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateGuidField([NotNull] string fieldName,
		                                     [CanBeNull] string aliasName = null)
		{
			return CreateField(fieldName, esriFieldType.esriFieldTypeGUID, aliasName);
		}

		/// <summary>
		/// Creates a BLOB field.
		/// </summary>
		/// <param name="fieldName">The field name (required).</param>
		/// <param name="aliasName">The alias name (optional).</param>
		/// <returns></returns>
		/// <remarks>
		/// ArcCatalog shows a Length property for BLOB fields.
		/// It is not clear, what this property is used for.
		/// <para/>
		/// Experimentation shows that the value entered is ignored
		/// and the property always shows as zero (tested with ArcGIS 10.1
		/// against SDE (Oracle), a file GDB and a personal GDB).
		/// </remarks>
		public static IField CreateBlobField([NotNull] string fieldName,
		                                     [CanBeNull] string aliasName = null)
		{
			return CreateField(fieldName, esriFieldType.esriFieldTypeBlob, aliasName);
		}

		/// <summary>
		/// Creates a field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="fieldType">Type of the field.</param>
		/// <param name="aliasName">Alias name for the field (optional).</param>
		/// <returns></returns>
		[NotNull]
		public static IField CreateField([NotNull] string fieldName,
		                                 esriFieldType fieldType,
		                                 [CanBeNull] string aliasName = null)
		{
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			IFieldEdit field = new FieldClass();

			field.Name_2 = fieldName;
			field.Type_2 = fieldType;

			if (StringUtils.IsNotEmpty(aliasName))
			{
				field.AliasName_2 = aliasName;
			}

			return field;
		}

		[NotNull]
		public static string GetDisplayName([NotNull] IField field,
		                                    bool allowProperCasing)
		{
			Assert.ArgumentNotNull(field, nameof(field));

			string name = field.Name;
			string aliasName = field.AliasName;

			return GetDisplayName(name, aliasName, allowProperCasing);
		}

		[NotNull]
		public static string GetDisplayName([NotNull] string name,
		                                    [NotNull] string aliasName,
		                                    bool allowProperCasing)
		{
			if (allowProperCasing && Equals(name, aliasName) && Equals(name, name.ToUpper()))
			{
				// there is no alias name defined, and the field name is all uppercase

				return StringUtils.ToProperCase(name.Replace("_", " "));
			}

			return aliasName;
		}

		[NotNull]
		public static string GetFieldTypeDisplayText(esriFieldType fieldType)
		{
			switch (fieldType)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
					return "Short Integer";
				case esriFieldType.esriFieldTypeInteger:
					return "Long Integer";
				case esriFieldType.esriFieldTypeSingle:
					return "Float";
				case esriFieldType.esriFieldTypeDouble:
					return "Double";
				case esriFieldType.esriFieldTypeString:
					return "Text";
				case esriFieldType.esriFieldTypeDate:
					return "Date";
				case esriFieldType.esriFieldTypeOID:
					return "Object ID";
				case esriFieldType.esriFieldTypeGeometry:
					return "Geometry";
				case esriFieldType.esriFieldTypeBlob:
					return "Blob";
				case esriFieldType.esriFieldTypeRaster:
					return "Raster";
				case esriFieldType.esriFieldTypeGUID:
					return "Guid";
				case esriFieldType.esriFieldTypeGlobalID:
					return "Global ID";
				case esriFieldType.esriFieldTypeXML:
					return "XML";
				default:
					return fieldType.ToString();
			}
		}

		public static esriFieldType GetFieldType(Type dataType)
		{
			if (dataType == typeof(int))
				return esriFieldType.esriFieldTypeInteger;
			if (dataType == typeof(short))
				return esriFieldType.esriFieldTypeSmallInteger;
			if (dataType == typeof(bool))
				return esriFieldType.esriFieldTypeSmallInteger;
			if (dataType == typeof(float))
				return esriFieldType.esriFieldTypeSingle;
			if (dataType == typeof(double))
				return esriFieldType.esriFieldTypeDouble;
			if (dataType == typeof(string))
				return esriFieldType.esriFieldTypeString;
			if (dataType == typeof(Guid))
				return esriFieldType.esriFieldTypeGUID;
			if (dataType == typeof(DateTime))
				return esriFieldType.esriFieldTypeDate;

			// can exist for datatable-expression columns, i.e. SUM(...)
			if (dataType == typeof(long))
				return esriFieldType.esriFieldTypeInteger;

			throw new NotImplementedException($"Unhandled type {dataType}");
		}

		[NotNull]
		public static object ConvertAttributeValue(
			[CanBeNull] object sourceValue,
			esriFieldType sourceType,
			esriFieldType targetType,
			[CanBeNull] IFormatProvider formatProvider = null)
		{
			if (sourceValue == null)
			{
				return DBNull.Value;
			}

			if (sourceType == targetType)
			{
				return sourceValue;
			}

			if (sourceValue is DBNull)
			{
				return sourceValue;
			}

			switch (targetType)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
					return Convert.ToInt16(sourceValue, formatProvider);

				case esriFieldType.esriFieldTypeOID:
				case esriFieldType.esriFieldTypeInteger:
					return Convert.ToInt32(sourceValue, formatProvider);

				case esriFieldType.esriFieldTypeSingle:
					return Convert.ToSingle(sourceValue, formatProvider);

				case esriFieldType.esriFieldTypeDouble:
					return Convert.ToDouble(sourceValue, formatProvider);

				case esriFieldType.esriFieldTypeString:
					return Convert.ToString(sourceValue, formatProvider) ??
					       string.Empty; // silence R#

				case esriFieldType.esriFieldTypeDate:
					return Convert.ToDateTime(sourceValue, formatProvider);

				case esriFieldType.esriFieldTypeGlobalID:
				case esriFieldType.esriFieldTypeGUID:
					string stringValue = Convert.ToString(sourceValue, formatProvider);
					return new Guid(stringValue ?? string.Empty); // silence R#

				case esriFieldType.esriFieldTypeXML:
				case esriFieldType.esriFieldTypeGeometry:
				case esriFieldType.esriFieldTypeBlob:
				case esriFieldType.esriFieldTypeRaster:
					throw new ArgumentException(
						$@"Unsupported target type for attribute conversion: {targetType}",
						nameof(targetType));

				default:
					throw new ArgumentOutOfRangeException(nameof(targetType));
			}
		}

		public static bool AreValuesEqual([CanBeNull] object v1,
		                                  [CanBeNull] object v2,
		                                  bool caseSensitive = true)
		{
			if (v1 is short)
			{
				if (v2 is int)
				{
					return v2.Equals(Convert.ToInt32(v1));
				}

				if (v2 is long)
				{
					return v2.Equals(Convert.ToInt64(v1));
				}
			}

			if (v1 is float f1)
			{
				if (v2 is float f2)
				{
					return MathUtils.AreSignificantDigitsEqual(f1, f2);
				}

				if (v2 is double d2)
				{
					return MathUtils.AreSignificantDigitsEqual(f1, d2);
				}
			}

			if (v1 is int)
			{
				if (v2 is short)
				{
					return Convert.ToInt32(v2).Equals(v1);
				}

				if (v2 is long)
				{
					return Convert.ToInt64(v1).Equals(v2);
				}
			}

			if (v1 is double d1)
			{
				if (v2 is double d2)
				{
					return MathUtils.AreSignificantDigitsEqual(d1, d2);
				}

				if (v2 is float f2)
				{
					return MathUtils.AreSignificantDigitsEqual(f2, d1);
				}
			}

			if (v1 is long)
			{
				if (v2 is short)
				{
					return Convert.ToInt64(v2).Equals(v1);
				}

				if (v2 is int)
				{
					return Convert.ToInt64(v2).Equals(v1);
				}
			}

			if (v1 is DBNull && v2 == null ||
			    v1 == null && v2 is DBNull)
			{
				// treat null and DBNull the same
				return true;
			}

			var v1String = v1 as string;
			var v2String = v2 as string;
			if (v1String != null && v2String != null)
			{
				return string.Equals(v1String, v2String,
				                     caseSensitive
					                     ? StringComparison.Ordinal
					                     : StringComparison.OrdinalIgnoreCase);
			}

			return Equals(v1, v2);
		}

		public static bool AreBlobValuesEqual([CanBeNull] object v1,
		                                      [CanBeNull] object v2)
		{
			if (v1 is DBNull && v2 == null ||
			    v1 == null && v2 is DBNull)
			{
				// treat null and DBNull the same
				return true;
			}

			var memBlobStream1 = v1 as IMemoryBlobStream;
			var memBlobStream2 = v2 as IMemoryBlobStream;

			if (memBlobStream1 == null || memBlobStream2 == null)
			{
				return Equals(memBlobStream1, memBlobStream2);
			}

			if (memBlobStream1.Size != memBlobStream2.Size)
			{
				return false;
			}

			var blobStream1 = (IMemoryBlobStreamVariant) memBlobStream1;
			var blobStream2 = (IMemoryBlobStreamVariant) memBlobStream2;

			blobStream1.ExportToVariant(out object bytesObj1);
			blobStream2.ExportToVariant(out object bytesObj2);

			byte[] bytesMain = (byte[]) bytesObj1;
			byte[] bytesTest = (byte[]) bytesObj2;

			// In .NET 6 we could use ReadOnlySpan<byte>.SequenceEqual() for better performance
			return bytesMain.SequenceEqual(bytesTest);
		}

		public static ITableField ToTableField([NotNull] IField field)
		{
			return new TableField(field.Name, (FieldType) field.Type,
			                      field.Length, field.AliasName);
		}

		[NotNull]
		internal static string GetShapeFieldName()
		{
			return "SHAPE";
		}

		[NotNull]
		private static IGeometryDef CreateGeometryDef(
			esriGeometryType geometryType,
			[NotNull] ISpatialReference spatialReference,
			double gridSize1,
			double gridSize2,
			double gridSize3,
			bool hasZ,
			bool hasM)
		{
			IGeometryDefEdit result = new GeometryDefClass();

			ISpatialReference highPrecisionSpatialReference;
			SpatialReferenceUtils.EnsureHighPrecision(spatialReference,
			                                          out highPrecisionSpatialReference);

			result.GeometryType_2 = geometryType;
			result.HasZ_2 = hasZ;
			result.HasM_2 = hasM;
			result.SpatialReference_2 = highPrecisionSpatialReference;

			ConfigureSpatialIndexGrid(result, gridSize1, gridSize2, gridSize3);

			return result;
		}

		private static void ConfigureSpatialIndexGrid(
			[NotNull] IGeometryDefEdit geometryDef,
			double gridSize1,
			double gridSize2,
			double gridSize3)
		{
			Assert.ArgumentNotNull(geometryDef, nameof(geometryDef));

			var gridCount = 0;
			if (gridSize1 > 0)
			{
				gridCount++;
			}

			if (gridSize2 > 0)
			{
				Assert.True(gridSize1 > 0, "Grid level 2 is defined, but grid level 1 is not");
				gridCount++;
			}

			if (gridSize3 > 0)
			{
				Assert.True(gridSize2 > 0, "Grid level 3 is defined, but grid level 2 is not");
				gridCount++;
			}

			geometryDef.GridCount_2 = gridCount;

			if (gridSize1 > 0)
			{
				geometryDef.set_GridSize(0, gridSize1);

				if (gridSize2 > 0)
				{
					geometryDef.set_GridSize(1, gridSize2);

					if (gridSize3 > 0)
					{
						geometryDef.set_GridSize(2, gridSize3);
					}
				}
			}
		}
	}
}
