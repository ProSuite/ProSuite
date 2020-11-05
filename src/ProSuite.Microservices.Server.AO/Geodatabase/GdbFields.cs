using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Microservices.Server.AO.Geodatabase
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

		public void AddFields(params IField[] fields)
		{
			foreach (var field in fields) _fields.Add(field);
		}
	}
}
