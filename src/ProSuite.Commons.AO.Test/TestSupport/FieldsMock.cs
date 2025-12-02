using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	public class FieldsMock : IFields
	{
		internal List<IField> FieldList { get; } = new List<IField>();

		public void AddFields(params IField[] fields)
		{
			foreach (IField field in fields)
			{
				FieldList.Add(field);
			}
		}

		public int FindField(string name)
		{
			return FieldList.FindIndex(
				field => field.Name.Equals(
					name,
					StringComparison.OrdinalIgnoreCase));
		}

		public int FindFieldByAliasName(string name)
		{
			return FieldList.FindIndex(
				field => field.AliasName.Equals(
					name, StringComparison.OrdinalIgnoreCase));
		}

#if Server11 || ARCGIS_12_0_OR_GREATER
		public void FindFieldIgnoreQualification(ISQLSyntax sqlSyntax, string Name, out int Index)
		{
			throw new NotImplementedException();
		}
#endif

		public int FieldCount => FieldList.Count;

		public IField get_Field(int index)
		{
			return FieldList[index];
		}
	}
}
