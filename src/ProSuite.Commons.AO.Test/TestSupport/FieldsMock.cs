using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	public class FieldsMock : IFields
	{
		private readonly List<IField> _fields = new List<IField>();

		public void AddFields(params IField[] fields)
		{
			foreach (IField field in fields)
			{
				_fields.Add(field);
			}
		}

		public int FindField(string name)
		{
			return _fields.FindIndex(
				field => (field.Name.Equals(
					name,
					StringComparison.OrdinalIgnoreCase)));
		}

		public int FindFieldByAliasName(string name)
		{
			return _fields.FindIndex(
				field => (field.AliasName.Equals(
					name, StringComparison.OrdinalIgnoreCase)));
		}

		public int FieldCount => _fields.Count;

		public IField get_Field(int index)
		{
			return _fields[index];
		}
	}
}
