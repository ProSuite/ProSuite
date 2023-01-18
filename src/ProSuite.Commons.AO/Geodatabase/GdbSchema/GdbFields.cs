using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public class GdbFields : IFields
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

#if Server11
		void IFields.FindFieldIgnoreQualification(ISQLSyntax sqlSyntax, string Name, out int Index)
		{
			throw new NotImplementedException();
		}
#endif
	}
}
