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

		public void AddFields(params IField[] fields)
		{
			foreach (var field in fields)
			{
				AddField(field);
			}
		}
	}
}
