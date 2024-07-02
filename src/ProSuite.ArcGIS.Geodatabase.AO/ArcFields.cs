extern alias EsriGeodatabase;
using EsriGeodatabase::ESRI.ArcGIS.Geodatabase;
using System.Collections.Generic;
using System.Linq;
using ProSuite.ArcGIS.Geodatabase.AO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ESRI.ArcGIS.Geodatabase.AO
{
	public class ArcFields : IFields
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFields _aoFields;

		public ArcFields(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFields aoFields)
		{
			_aoFields = aoFields;
		}

		public int FieldCount => _aoFields.FieldCount;

		public IList<IField> Field => GetFields(_aoFields);

		public IField get_Field(int index)
		{
			return new ArcField(_aoFields.get_Field(index));
		}

		public int FindField(string Name)
		{
			return _aoFields.FindField(Name);
		}

		public int FindFieldByAliasName(string Name)
		{
			return _aoFields.FindFieldByAliasName(Name);
		}


		/// <summary>
		/// Gets the fields.
		/// </summary>
		/// <param name="fields">The fields.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IField> GetFields([NotNull] EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFields fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			int fieldCount = fields.FieldCount;

			var result = new List<IField>(fieldCount);

			result.AddRange(EnumFields(fields).Select(f => new ArcField(f)));

			return result;
		}

		[NotNull]
		public static IEnumerable<EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField> EnumFields([NotNull] EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFields fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			int fieldCount = fields.FieldCount;

			var result = new List<IField>(fieldCount);

			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField field = fields.Field[fieldIndex];

				yield return field;
			}
		}
	}

	public class ArcField : IField
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField _aoField;

		public ArcField(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField aoField)
		{
			_aoField = aoField;
		}

		public EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IField AoField => _aoField;

		#region Implementation of IField

		public string Name => _aoField.Name;

		public string AliasName => _aoField.AliasName;

		public esriFieldType Type => (esriFieldType)_aoField.Type;

		public IDomain Domain => new ArcDomain(_aoField.Domain);

		public object DefaultValue => _aoField.DefaultValue;

		public int Length => _aoField.Length;

		public int Precision => _aoField.Precision;

		public int Scale => _aoField.Scale;

		public bool IsNullable => _aoField.IsNullable;

		public IGeometryDef GeometryDef => new ArcGeometryDef(_aoField.GeometryDef);

		public int VarType => _aoField.VarType;

		public bool DomainFixed => _aoField.DomainFixed;

		public bool Required => _aoField.Required;

		public bool Editable => _aoField.Editable;

		public bool CheckValue(object Value)
		{
			return _aoField.CheckValue(Value);
		}

		#endregion
	}

	public class ArcDomain : IDomain
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDomain _aoDomain;

		public ArcDomain(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDomain aoDomain)
		{
			_aoDomain = aoDomain;
		}

		#region Implementation of IDomain

		public int DomainID
		{
			get => _aoDomain.DomainID;
			set => _aoDomain.DomainID = value;
		}

		public string Description
		{
			get => _aoDomain.Description;
			set => _aoDomain.Description = value;
		}

		public esriFieldType FieldType
		{
			get => (esriFieldType)_aoDomain.FieldType;
			set => _aoDomain.FieldType = (EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriFieldType)value;
		}

		public esriMergePolicyType MergePolicy { get; set; }

		public esriSplitPolicyType SplitPolicy { get; set; }

		public string Name
		{
			get => _aoDomain.Name;
			set => _aoDomain.Name = value;
		}

		public string Owner
		{
			get => _aoDomain.Owner;
			set => _aoDomain.Owner = value;
		}

		public bool MemberOf(object Value)
		{
			return _aoDomain.MemberOf(Value);
		}

		#endregion
	}
}
