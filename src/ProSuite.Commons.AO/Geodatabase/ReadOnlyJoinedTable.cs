
using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyJoinedTable : ReadOnlyTable
	{
		protected static ReadOnlyJoinedTable CreateReadOnlyJoinedTable(ITable table)
		{
			return new ReadOnlyJoinedTable(table);
		}

		protected ReadOnlyJoinedTable(ITable joinedTable):
			base(joinedTable)
		{}

		public override int FindField(string name)
		{
			return FindField(BaseTable, name);
		}

		public static int FindField(ITable baseTable, string name)
		{
			int index = baseTable.FindField(name);
			if (index >= 0)
			{
				return index;
			}

			List<string> fieldNames = new List<string>();
			for (int iField = 0; iField < baseTable.Fields.FieldCount; iField++)
			{
				fieldNames.Add(baseTable.Fields.get_Field(iField).Name);
			}

			string searchName = name;
			while (searchName != null)
			{
				List<int> matchIndices = new List<int>();
				for (int iField = 0; iField < fieldNames.Count; iField++)
				{
					string fieldName = fieldNames[iField];
					if (fieldName.Equals(searchName, StringComparison.InvariantCultureIgnoreCase)
					    || fieldName.EndsWith($".{searchName}",
					                          StringComparison.InvariantCultureIgnoreCase))
					{
						matchIndices.Add(iField);
					}
				}

				if (matchIndices.Count == 1)
				{
					return matchIndices[0];
				}

				int sepIdx = searchName.IndexOf('.');
				if (sepIdx >= 0)
				{
					searchName = searchName.Substring(sepIdx + 1);
				}
				else
				{
					searchName = null;
				}
			}

			return -1;
		}
	}
}
