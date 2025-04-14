using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;
using Field = ArcGIS.Core.Data.Field;

namespace ProSuite.GIS.Geodatabase.AGP;

public class ArcFields : IFields
{
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

		return _fieldIndexByName.GetValueOrDefault(fieldName, -1);
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
		throw new NotImplementedException();
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
