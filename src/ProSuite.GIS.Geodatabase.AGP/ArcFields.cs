using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using ArcGIS.Core.Data;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.API;
using Field = ArcGIS.Core.Data.Field;

namespace ProSuite.GIS.Geodatabase.AGP;

public class ArcFields : IFields
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly IReadOnlyList<ArcField> _fields;

	private Dictionary<string, int> _fieldIndexByName;

	/// <summary>
	/// Initializes a new instance of the <see cref="ArcFields"/> class.
	/// </summary>
	/// <param name="fields">The Pro Sdk fields</param>
	/// <param name="geometryDefinition">The geometry definition, in case the fields belong to a feature class.</param>
	/// <param name="workspace">The optional workspace, to enable re-using domains.</param>
	public ArcFields([NotNull] IEnumerable<Field> fields,
	                 [CanBeNull] IGeometryDef geometryDefinition,
	                 [CanBeNull] IFeatureWorkspace workspace)
	{
		_fields = fields
		          .Select(f => f.FieldType == FieldType.Geometry
			                       ? new ArcGeometryField(f, Assert.NotNull(geometryDefinition),
			                                              workspace)
			                       : new ArcField(f, workspace))
		          .ToList();
	}

	public int FieldCount => _fields.Count;

	public IList<IField> Field => new ReadOnlyList<IField>(_fields.Cast<IField>().ToList());

	public IField this[int index] => _fields[index];

	public IField get_Field(int index)
	{
		return _fields[index];
	}

	public int FindField(string fieldName)
	{
		if (_fieldIndexByName == null)
		{
			_fieldIndexByName = new Dictionary<string, int>();

			int fieldIndex = 0;
			foreach (ArcField field in _fields)
			{
				_fieldIndexByName.Add(field.Name, fieldIndex++);
			}
		}

		if (! _fieldIndexByName.TryGetValue(fieldName, out int resultIndex))
		{
			// GOTOP-469: In some models the FeatureClassDefinition.GetShapeField() returns the Model name,
			// i.e. 'Shape' instead of 'SHAPE', which is the actual field name. The default FindField
			// implementation also finds when searching with different case search strings, i.e. the search
			// is case insensitive!
			for (int i= 0; i < _fields.Count; i++)
			{
				ArcField field = _fields[i];

				if (field.Name.ToUpper() == fieldName.ToUpper())
				{
					_msg.DebugFormat("Field {0} found in field list but only using case-insensitive search.", fieldName);
					return i;
				}
			}

			_msg.VerboseDebug(() => $"Field {fieldName} not found in feature class.");
			return -1;
		}

		return resultIndex;
	}

	public int FindFieldByAliasName(string aliasName)
	{
		return FindField(
			f => f.AliasName.Equals(aliasName, StringComparison.OrdinalIgnoreCase));
	}

	private int FindField([NotNull] Predicate<IField> predicate)
	{
		for (int i = 0; i < _fields.Count; i++)
		{
			ArcField field = _fields[i];

			if (predicate(field))
			{
				return i;
			}
		}

		return -1;
	}
}

public class ArcField : IField
{
	private readonly Field _proField;
	private readonly IDomain _cachedDomain;
	private readonly object _defaultValue;

	public ArcField(Field proField,
	                IFeatureWorkspace workspace = null)
	{
		_proField = proField;

		Domain domain = _proField.GetDomain();

		_cachedDomain = ArcGeodatabaseUtils.ToArcDomain(domain, workspace);

		_defaultValue = _proField.GetDefaultValue();
	}

	#region Implementation of IField

	public string Name => _proField.Name;

	public string AliasName => _proField.AliasName;

	public esriFieldType Type => (esriFieldType) _proField.FieldType;

	public IDomain Domain => _cachedDomain;

	public object DefaultValue => _defaultValue;
	//_proField.HasDefaultValue ? _proField.GetDefaultValue() : null;

	public int Length => _proField.Length;

	public int Precision => _proField.Precision;

	public int Scale => _proField.Scale;

	public bool IsNullable => _proField.IsNullable;

	public virtual IGeometryDef GeometryDef => null; // Implemented by ArcGeometryField subclass

	public int VarType => throw new NotImplementedException();

	public bool DomainFixed => _proField.IsDomainFixed;

	public bool Required => _proField.IsRequired;

	public bool Editable => _proField.IsEditable;

	public bool CheckValue(object value)
	{
		if (Type == esriFieldType.esriFieldTypeGUID)
		{
			try
			{
				new Guid((string) value);
				return true;
			}
			catch
			{
				return false;
			}
		}

		if (Type == esriFieldType.esriFieldTypeSmallInteger)
		{
			// value could be int16
			return short.TryParse(Convert.ToString(value), out _);
		}

		if (Type == esriFieldType.esriFieldTypeInteger)
		{
			// value could be int32
			return int.TryParse(Convert.ToString(value), out _);
		}

		if (Type == esriFieldType.esriFieldTypeBigInteger)
		{
			return long.TryParse(Convert.ToString(value), out _);
		}

		if (! TryGetType(Type, out Type type))
		{
			// Cannot check raster, XML, blob field:
			return true;
		}

		return type.IsInstanceOfType(value);
	}

	private static bool TryGetType(esriFieldType fieldType, out Type type)
	{
		switch (fieldType)
		{
			case esriFieldType.esriFieldTypeString:
				type = typeof(string);
				return true;

			case esriFieldType.esriFieldTypeInteger:
				type = typeof(int);
				return true;

			case esriFieldType.esriFieldTypeSmallInteger:
				type = typeof(short);
				return true;

			case esriFieldType.esriFieldTypeOID:
			case esriFieldType.esriFieldTypeBigInteger:
				type = typeof(long);
				return true;

			case esriFieldType.esriFieldTypeDouble:
				type = typeof(double);
				return true;

			case esriFieldType.esriFieldTypeSingle:
				type = typeof(float);
				return true;

			case esriFieldType.esriFieldTypeDate:
				type = typeof(DateTime);
				return true;

			case esriFieldType.esriFieldTypeGeometry:
				type = typeof(IGeometry);
				return true;

			case esriFieldType.esriFieldTypeGlobalID:
			case esriFieldType.esriFieldTypeGUID:
				type = typeof(Guid);
				return true;

			case esriFieldType.esriFieldTypeBlob:
			case esriFieldType.esriFieldTypeRaster:
			case esriFieldType.esriFieldTypeXML:
				type = null;
				return false;

			default:
				throw new ArgumentException($"Unexpected field type: {fieldType}");
		}
	}

	#endregion
}

public class ArcGeometryField : ArcField
{
	private readonly IGeometryDef _geometryDef;

	public ArcGeometryField([NotNull] Field proField,
	                        [NotNull] IGeometryDef geometryDef,
	                        [CanBeNull] IFeatureWorkspace workspace)
		: base(proField, workspace)
	{
		_geometryDef = geometryDef;
	}

	public override IGeometryDef GeometryDef => _geometryDef;
}

public abstract class ArcDomain : IDomain
{
	private readonly Domain _proDomain;

	private readonly string _domainName;
	private readonly esriFieldType _fieldType;

	protected ArcDomain(Domain proDomain)
	{
		_proDomain = proDomain;

		_domainName = proDomain.GetName();
		_fieldType = (esriFieldType) proDomain.GetFieldType();
	}

	#region Implementation of IDomain

	//public int DomainID
	//{
	//	get => _proDomain.DomainID;
	//	set => _proDomain.DomainID = value;
	//}

	public int DomainID
	{
		get => throw new NotImplementedException();
		set => throw new NotImplementedException();
	}

	public string Description
	{
		get => _proDomain.GetDescription();
		set => throw new NotImplementedException();
	}

	public esriFieldType FieldType
	{
		get => _fieldType;
		set => throw new NotImplementedException();
	}

	public esriMergePolicyType MergePolicy
	{
		get => (esriMergePolicyType) _proDomain.MergePolicy;
		set => throw new NotImplementedException();
	}

	public esriSplitPolicyType SplitPolicy
	{
		get => (esriSplitPolicyType) _proDomain.SplitPolicy;
		set => throw new NotImplementedException();
	}

	public string Name
	{
		get => _domainName;
		set => throw new NotImplementedException();
	}

	public string Owner
	{
		get => throw new NotImplementedException();
		set => throw new NotImplementedException();
	}

	public bool MemberOf(object value)
	{
		throw new NotImplementedException();
		//return _proDomain.MemberOf(value);
	}

	#endregion
}

public class ArcCodedValueDomain : ArcDomain, ICodedValueDomain
{
	private readonly CodedValueDomain _proCodedDomain;

	private readonly List<CodedValue> _codedValues;
	private readonly int _codeCount;

	public ArcCodedValueDomain(CodedValueDomain proDomain)
		: base(proDomain)
	{
		_proCodedDomain = proDomain;

		SortedList<object, string> codedValuePairs = proDomain.GetCodedValuePairs();
		_codeCount = codedValuePairs.Count;

		_codedValues = new List<CodedValue>(_codeCount);

		foreach (KeyValuePair<object, string> pair in codedValuePairs)
		{
			_codedValues.Add(new CodedValue(pair.Key, pair.Value));
		}
	}

	#region Implementation of ICodedValueDomain

	public int CodeCount => _codeCount;

	public string get_Name(int index)
	{
		if (index < 0 || index >= _codedValues.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		return _codedValues[index].Name;
	}

	public object get_Value(int index)
	{
		if (index < 0 || index >= _codedValues.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		return _codedValues[index].Value;
	}

	#endregion
}

public class ArcRangeDomain : ArcDomain, IRangeDomain
{
	private readonly RangeDomain _proRangeDomain;

	private readonly object _minValue;
	private readonly object _maxValue;

	public ArcRangeDomain(RangeDomain proDomain)
		: base(proDomain)
	{
		_proRangeDomain = proDomain;

		_minValue = _proRangeDomain.GetMinValue();
		_maxValue = _proRangeDomain.GetMaxValue();
	}

	#region Implementation of IRangeDomain

	public object MinValue
	{
		get => _minValue;
	}

	public object MaxValue
	{
		get => _maxValue;
	}

	#endregion
}
