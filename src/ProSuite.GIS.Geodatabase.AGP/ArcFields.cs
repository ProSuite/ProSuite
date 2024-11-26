using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;
using Field = ArcGIS.Core.Data.Field;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcFields : IFields
	{
		private readonly IList<ArcField> _fields;

		public ArcFields(IEnumerable<Field> fields, IGeometryDef geometryDefinition)
		{
			_fields = fields
			          .Select(f => new ArcField(
				                  f, f.FieldType == FieldType.Geometry ? geometryDefinition : null))
			          .ToList();
		}

		public int FieldCount => _fields.Count;

		public IList<IField> Field => new ReadOnlyList<IField>(_fields.Cast<IField>().ToList());

		public IField get_Field(int index)
		{
			return _fields[index];
		}

		public int FindField(string fieldName)
		{
			return FindField(
				f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
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

		public ArcField(Field proField, IGeometryDef geometryDefinition = null)
		{
			_proField = proField;

			GeometryDef = geometryDefinition;
		}

		public Field ProField => _proField;

		#region Implementation of IField

		public string Name => _proField.Name;

		public string AliasName => _proField.AliasName;

		public esriFieldType Type => (esriFieldType) _proField.FieldType;

		public IDomain Domain => ArcGeodatabaseUtils.ToArcDomain(_proField.GetDomain());

		public object DefaultValue =>
			_proField.HasDefaultValue ? _proField.GetDefaultValue() : null;

		public int Length => _proField.Length;

		public int Precision => _proField.Precision;

		public int Scale => _proField.Scale;

		public bool IsNullable => _proField.IsNullable;

		public IGeometryDef GeometryDef { get; }

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

	public abstract class ArcDomain : IDomain
	{
		private readonly Domain _proDomain;

		protected ArcDomain(Domain proDomain)
		{
			_proDomain = proDomain;
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
			get => (esriFieldType) _proDomain.GetFieldType();
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
			get => _proDomain.GetName();
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

		private SortedList<object, string> _codedValuePairs;

		public ArcCodedValueDomain(CodedValueDomain proDomain)
			: base(proDomain)
		{
			_proCodedDomain = proDomain;
		}

		private SortedList<object, string> CodedValuePairs
		{
			get { return _codedValuePairs ??= _proCodedDomain.GetCodedValuePairs(); }
		}

		#region Implementation of ICodedValueDomain

		public int CodeCount => _proCodedDomain.GetCount();

		public string get_Name(int index)
		{
			return CodedValuePairs.Values[index];
		}

		public object get_Value(int index)
		{
			return CodedValuePairs.Keys[index];
		}

		#endregion
	}

	public class ArcRangeDomain : ArcDomain, IRangeDomain
	{
		private readonly RangeDomain _proRangeDomain;

		public ArcRangeDomain(RangeDomain proDomain)
			: base(proDomain)
		{
			_proRangeDomain = proDomain;
		}

		#region Implementation of IRangeDomain

		public object MinValue
		{
			get => _proRangeDomain.GetMinValue();
		}

		public object MaxValue
		{
			get => _proRangeDomain.GetMaxValue();
		}

		#endregion
	}
}
