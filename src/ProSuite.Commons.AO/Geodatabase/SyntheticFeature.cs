using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// An instantiable implementation of IFeature.
	/// Useful if you need a feature, but don't have one.
	/// </summary>
	/// <remarks>
	/// The methods <see cref="IRow.Store"/> and <see cref="IRow.Delete"/>
	/// are not implemented (they throw <see cref="NotImplementedException"/>).
	/// <para/>
	/// While <see cref="HasOID"/> mirrors <see cref="Class"/>.HasOID,
	/// the <see cref="OID"/> is <c>-1</c> unless another value is passed
	/// to the constructor. Be very careful when using a SyntheticFeature
	/// with <see cref="GdbObjectReference"/>.
	/// <para/>
	/// The <see cref="OID"/> and <see cref="Shape"/> properties
	/// are shortcuts into the internal array of field values:
	/// you get the same value as you would with <see cref="get_Value"/>
	/// for the appropriate field index.
	/// </remarks>
	public class SyntheticFeature : IFeature, IFeatureBuffer, IRowSubtypes
		// inherited: IObject, IRow, IRowBuffer
	{
		[NotNull] private readonly IFeatureClass _featureClass;
		[NotNull] private readonly IFields _fields;
		private readonly int _oidFieldIndex;
		private readonly int _shapeFieldIndex;
		private readonly int _subtypeFieldIndex;
		[NotNull] private readonly object[] _fieldValues;

		#region Construction

		public SyntheticFeature([NotNull] IFeatureClass schema, int oid = -1)
		{
			_featureClass = schema;
			_fields = schema.Fields;

			_oidFieldIndex = GetOidFieldIndex(schema);
			_shapeFieldIndex = GetShapeFieldIndex(schema);

			if (_shapeFieldIndex < 0)
			{
				throw new ArgumentException(@"FeatureClass has no Shape field", nameof(schema));
			}

			_subtypeFieldIndex = DatasetUtils.GetSubtypeFieldIndex(schema);

			// Notes:
			// A real feature, ie, the result of IFeatureClass.CreateFeature(),
			// has an OID (sequence number), an empty but non-null Shape with
			// the proper ZAware, MAware, and SpatialReference, as well as
			// per-field default values applied (todo subtype default values?)

			_fieldValues = GetInitialValues(_fields);

			if (_oidFieldIndex >= 0)
			{
				_fieldValues[_oidFieldIndex] = oid; // bypass set_Value
			}

			if (_shapeFieldIndex >= 0)
			{
				_fieldValues[_shapeFieldIndex] = GetInitialShape(schema); // bypass set_Value
			}
		}

		private static int GetOidFieldIndex(IFeatureClass schema)
		{
			try
			{
				string oidFieldName = schema.OIDFieldName;
				return string.IsNullOrEmpty(oidFieldName)
					       ? -1 // no OID field
					       : schema.FindField(oidFieldName);
			}
			catch
			{
				return -1;
			}
		}

		private static int GetShapeFieldIndex(IFeatureClass schema)
		{
			try
			{
				string shapeFieldName = schema.ShapeFieldName;
				return string.IsNullOrEmpty(shapeFieldName)
					       ? -1 // no SHAPE field
					       : schema.FindField(shapeFieldName);
			}
			catch
			{
				return -1;
			}
		}

		[NotNull]
		private static object[] GetInitialValues([NotNull] IFields fields)
		{
			int fieldCount = fields.FieldCount;
			var values = new object[fieldCount];

			for (var i = 0; i < fieldCount; i++)
			{
				IField field = fields.Field[i];

				object value = GetInitialValue(field);

				values[i] = value;
			}

			return values;
		}

		private static object GetInitialValue([NotNull] IField field)
		{
			object defaultValue = field.DefaultValue ?? DBNull.Value;

			if (field.IsNullable)
			{
				return defaultValue;
			}

			if (field.DefaultValue == DBNull.Value)
			{
				// We've a NOT NULL field but the default value is NULL:
				// make an educated guess... (ArcGIS doesn't distinguish
				// "no default value" from "use NULL as default value").

				// TODO If there's a domain, return rd.MinValue or cd.Value(0) (?)

				switch (field.Type)
				{
					case esriFieldType.esriFieldTypeSmallInteger:
					case esriFieldType.esriFieldTypeInteger:
						return 0;

					case esriFieldType.esriFieldTypeSingle:
					case esriFieldType.esriFieldTypeDouble:
						return 0.0;

					case esriFieldType.esriFieldTypeString:
						return string.Empty;

					case esriFieldType.esriFieldTypeDate:
						// Use the Win NT epoch (1601-01-01T00:00:00),
						// MinValue may be out of range for some systems
						return new DateTime(1601, 1, 1);

					case esriFieldType.esriFieldTypeOID:
						return -1; // sic

					case esriFieldType.esriFieldTypeGUID:
						return GuidString(Guid.Empty);

					//case esriFieldType.esriFieldTypeGeometry:
					//case esriFieldType.esriFieldTypeBlob:
					//case esriFieldType.esriFieldTypeRaster:
					//case esriFieldType.esriFieldTypeGlobalID:
					//case esriFieldType.esriFieldTypeXML:
					default:
						return DBNull.Value;
				}
			}

			return defaultValue;
		}

		[CanBeNull]
		private static IGeometry GetInitialShape([NotNull] IFeatureClass schema)
		{
			int shapeIndex = schema.FindField(schema.ShapeFieldName);
			if (shapeIndex < 0)
			{
				return null;
			}

			IField shapeField = schema.Fields.Field[shapeIndex];
			IGeometryDef geometryDef = shapeField.GeometryDef;
			if (geometryDef == null)
			{
				return null;
			}

			IGeometry shape = GetInitialShape(geometryDef.GeometryType);

			shape.SpatialReference = geometryDef.SpatialReference;

			if (shape is IZAware zAware)
			{
				zAware.ZAware = geometryDef.HasZ;
			}

			if (shape is IMAware mAware)
			{
				mAware.MAware = geometryDef.HasM;
			}

			return shape;
		}

		[NotNull]
		private static IGeometry GetInitialShape(esriGeometryType shapeType)
		{
			switch (shapeType)
			{
				case esriGeometryType.esriGeometryPoint:
					return new PointClass();

				case esriGeometryType.esriGeometryMultipoint:
					return new MultipointClass();

				case esriGeometryType.esriGeometryPolyline:
					return new PolylineClass();

				case esriGeometryType.esriGeometryPolygon:
					return new PolygonClass();

				case esriGeometryType.esriGeometryEnvelope:
					return new EnvelopeClass();

				case esriGeometryType.esriGeometryMultiPatch:
					return new MultiPatchClass();
			}

			throw new NotSupportedException(
				$"Shape type not supported: {shapeType}");
		}

		#endregion

		#region IFeature

		public IGeometry ShapeCopy => GeometryFactory.Clone(Shape);

		public IGeometry Shape
		{
			get => _fieldValues[_shapeFieldIndex] as IGeometry;
			set => _fieldValues[_shapeFieldIndex] = value ?? GetInitialShape(_featureClass);
		}

		public IEnvelope Extent => Shape.Envelope;

		public esriFeatureType FeatureType => _featureClass.FeatureType;

		#region IObject

		public IObjectClass Class => _featureClass;

		#region IRow

		public void Store()
		{
			throw new InvalidOperationException("Cannot store SyntheticFeature");
		}

		public void Delete()
		{
			throw new InvalidOperationException("Cannot delete SyntheticFeature");
		}

		public bool HasOID => _featureClass.HasOID;

#if ARCGIS_11_0_OR_GREATER
		public long OID
#else
		public int OID
#endif
		{
			get
			{
				if (_oidFieldIndex < 0)
				{
					// Weird: IRepRenderer.get_SymbolByFeature reads IFeature.OID, but -1 seems fine
					return -1;
				}

				// Use convert because it could be either an int or a long
				object fieldValue = _fieldValues[_oidFieldIndex];

#if ARCGIS_11_0_OR_GREATER
				return Convert.ToInt64(fieldValue);
#else
				return Convert.ToInt32(fieldValue);
#endif
			}
		}

		public ITable Table => (ITable) _featureClass;

		#region IRowBuffer

		public IFields Fields => _fields;

		public object get_Value(int index)
		{
			return _fieldValues[index];
		}

		public void set_Value(int index, object value)
		{
			IField field = _fields.Field[index];

			if (! field.Editable)
			{
				throw new InvalidOperationException($"Field {field.Name} is not editable");
			}

			if (value == null)
			{
				// Empirical: null seems to set field's default value
				value = GetInitialValue(field);
			}

			if (! field.IsNullable && (value == null || value == DBNull.Value))
			{
				throw new InvalidOperationException($"Field {field.Name} is not nullable");
			}

			// IField.CheckValue() catches some bad values, but it's a rather
			// weak test; for example, shapeField.CheckValue(123) is true
			if (! field.CheckValue(value))
			{
				throw new ArgumentOutOfRangeException(
					$"Value <{value}> is not valid for field {field.Name}");
			}

			try
			{
				value = ConvertToFieldType(value, field.Type);
			}
			catch (Exception ex)
			{
				throw new ArgumentException(
					string.Format("Field {0}: Cannot convert value '{1}' to {2}: {3}",
					              field.Name, value, field.Type, ex.Message), ex);
			}

			_fieldValues[index] = value;
		}

		private static object ConvertToFieldType(object value, esriFieldType fieldType)
		{
			if (value == null || value == DBNull.Value)
			{
				return DBNull.Value;
			}

			switch (fieldType)
			{
				case esriFieldType.esriFieldTypeSmallInteger:
					return Convert.ToInt16(value);
				case esriFieldType.esriFieldTypeInteger:
					return Convert.ToInt32(value);

				case esriFieldType.esriFieldTypeSingle:
					return Convert.ToSingle(value);
				case esriFieldType.esriFieldTypeDouble:
					return Convert.ToDouble(value);

				case esriFieldType.esriFieldTypeString:
					return Convert.ToString(value);

				case esriFieldType.esriFieldTypeDate:
					return Convert.ToDateTime(value);

				case esriFieldType.esriFieldTypeOID:
					return Convert.ToInt32(value);

				case esriFieldType.esriFieldTypeGUID:
				case esriFieldType.esriFieldTypeGlobalID:
					// Citing from Esri Help: "The Object ID and GUID data types store
					// registry style strings consisting of 36 characters enclosed in
					// curly brackets." Example: "{00000000-0000-0000-0000-000000000000}"
					if (value is Guid guid)
						return GuidString(guid);
					if (value is string s)
						return GuidString(new Guid(s));
					if (value is byte[] b) // Guid(b) complains if not 16 bytes
						return GuidString(new Guid(b));
					throw new ArgumentException("Value is neither Guid nor String nor Byte[16]");

				case esriFieldType.esriFieldTypeGeometry:
					if (value is IGeometry)
						return value;
					throw new ArgumentException("Value is not IGeometry");

				//case esriFieldType.esriFieldTypeBlob:
				//case esriFieldType.esriFieldTypeRaster:
				//case esriFieldType.esriFieldTypeXML:
				default:
					return value; // pass through
			}
		}

		private static string GuidString(Guid guid)
		{
			return guid.ToString("B"); // in braces, with dashes
		}

		#endregion

		#endregion

		#endregion

		#endregion

		#region IValidate

		// TODO Consider implementing IValidate

		//public IFields GetInvalidFields()
		//{
		//    return new FieldsClass();
		//}

		//public IEnumRule GetInvalidRules()
		//{
		//    throw new NotImplementedException();
		//}

		//public IEnumRule GetInvalidRulesByField(string fieldName)
		//{
		//    throw new NotImplementedException();
		//}

		//public bool Validate(out string errorMessage)
		//{
		//    errorMessage = string.Empty;
		//    return true;
		//}

		#endregion

		#region IRowSubtypes

		public void InitDefaultValues()
		{
			if (! (Class is ISubtypes subtypes) || ! subtypes.HasSubtype)
			{
				return; // silently ignore (or should we set class default values?)
			}

			int fieldCount = _fields.FieldCount;
			for (var i = 0; i < fieldCount; i++)
			{
				if (i == subtypes.SubtypeFieldIndex)
				{
					continue; // skip, this has already been set
				}

				IField field = _fields.Field[i];

				if (! field.Editable)
				{
					continue;
				}

				//if (field.Type == esriFieldType.esriFieldTypeOID) continue;
				//if (field.Type == esriFieldType.esriFieldTypeGeometry) continue;

				object value = subtypes.DefaultValue[SubtypeCode, field.Name];

				if (value == null || value == DBNull.Value)
				{
					continue; // No default value seems to be represented by NULL
				}

				if (! field.CheckValue(value))
				{
					continue;
				}

				IDomain domain = subtypes.Domain[SubtypeCode, field.Name];

				if (domain != null && ! domain.MemberOf(value))
				{
					continue;
				}

				set_Value(i, value);
			}
		}

		public int SubtypeCode
		{
			get
			{
				if (_subtypeFieldIndex < 0)
				{
					return 0; // Mimics behaviour of real features
				}

				return Convert.ToInt32(get_Value(_subtypeFieldIndex));
			}
			set
			{
				if (_subtypeFieldIndex < 0)
				{
					return; // Ignore if schema has no subtypes
				}

				set_Value(_subtypeFieldIndex, value);
			}
		}

		#endregion
	}
}
