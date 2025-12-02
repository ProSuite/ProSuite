using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class GdbFields : IFields, IReadOnlyList<ITableField>
	{
		private readonly List<IField> _fields = new List<IField>();

		public int FindField(string name)
		{
			return _fields.FindIndex(
				field => field.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}

		public int FindFieldByAliasName(string name)
		{
			return _fields.FindIndex(
				field => field.AliasName.Equals(name, StringComparison.OrdinalIgnoreCase));
		}

		public int FieldCount => _fields.Count;

		public IField get_Field(int index)
		{
			return _fields[index];
		}

		public int AddField(IField field)
		{
			if (string.IsNullOrEmpty(field.Name))
			{
				throw new ArgumentException("The field has no name");
			}

			if (_fields.Any(
				    f => f.Name.Equals(field.Name, StringComparison.InvariantCultureIgnoreCase)))
			{
				throw new ArgumentException(
					$"The field list already contains a field with name {field.Name}");
			}

			_fields.Add(field);
			return _fields.Count - 1;
		}

		public List<int> AddFields(params IField[] fields)
		{
			List<int> added = new List<int>();
			foreach (var field in fields)
			{
				added.Add(AddField(field));
			}

			return added;
		}

#if Server11 || ARCGIS_12_0_OR_GREATER
		void IFields.FindFieldIgnoreQualification(ISQLSyntax sqlSyntax, string Name, out int Index)
		{
			throw new NotImplementedException();
		}
#endif

		#region Implementation of IEnumerable

		public IEnumerator<ITableField> GetEnumerator()
		{
			return _fields.Select(FieldUtils.ToTableField)
			              .Cast<ITableField>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Implementation of IReadOnlyCollection<out ITableFields>

		int IReadOnlyCollection<ITableField>.Count => _fields.Count;

		#endregion

		#region Implementation of IReadOnlyList<out ITableFields>

		public ITableField this[int index]
		{
			get
			{
				IField f = _fields[index];

				return FieldUtils.ToTableField(f);
			}
		}

		#endregion
	}
}
