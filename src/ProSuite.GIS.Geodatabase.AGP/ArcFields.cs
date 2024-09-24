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

		///// <summary>
		///// Gets the fields.
		///// </summary>
		///// <param name="fields">The fields.</param>
		///// <returns></returns>
		//[NotNull]
		//public static IList<IField> GetFields([NotNull] EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFields fields)
		//{
		//	Assert.ArgumentNotNull(fields, nameof(fields));

		//	int fieldCount = fields.FieldCount;

		//	var result = new List<IField>(fieldCount);

		//	result.AddRange(EnumFields(fields).Select(f => new ArcField(f)));

		//	return result;
		//}

		//[NotNull]
		//public static IEnumerable<EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField> EnumFields([NotNull] EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFields fields)
		//{
		//	Assert.ArgumentNotNull(fields, nameof(fields));

		//	int fieldCount = fields.FieldCount;

		//	var result = new List<IField>(fieldCount);

		//	for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
		//	{
		//		EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField field = fields.Field[fieldIndex];

		//		yield return field;
		//	}
		//}
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

		public IDomain Domain => new ArcDomain(_proField.GetDomain());

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

	public class ArcDomain : IDomain
	{
		private readonly Domain _proDomain;

		public ArcDomain(Domain proDomain)
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
}
