using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.TableBased
{
	public static class TableBasedUtils
	{
		public static IEnumerable<Involved> GetInvolvedRowsFromJoinedRow(
			[NotNull] IReadOnlyRow joinedRow,
			[NotNull] IEnumerable<IReadOnlyTable> baseTables,
			[NotNull] Func<string, int> findFieldFunc)
		{
			foreach (IReadOnlyTable baseTable in baseTables)
			{
				string oidFieldName = baseTable.OIDFieldName;

				string oidFieldQualified =
					string.IsNullOrEmpty(oidFieldName)
						? null // no OID field
						: DatasetUtils.QualifyFieldName(baseTable, oidFieldName);

				if (oidFieldName == null)
				{
					continue;
				}

				int oidFieldIdx = findFieldFunc(oidFieldQualified);

				if (oidFieldIdx == -1)
				{
					continue;
				}

				long? oidValue =
					GdbObjectUtils.ReadRowOidValue(joinedRow, oidFieldIdx);

				if (oidValue != null)
				{
					yield return new InvolvedRow(baseTable.Name, oidValue.Value);
				}
			}
		}

		/// <summary>
		/// Finds the specified field in the provided joined table. The field can be
		/// unqualified and if it is unique, it will return the field index of the
		/// respective qualified field in the table. Optionally, the specified field name
		/// can be unqualified first. TODO: What for? 
		/// </summary>
		/// <param name="joinedTable"></param>
		/// <param name="name"></param>
		/// <param name="allowUnQualifyFieldNames"></param>
		/// <returns></returns>
		public static int FindFieldInJoin([NotNull] ITable joinedTable,
		                                  [NotNull] string name,
		                                  bool allowUnQualifyFieldNames)
		{
			int index = joinedTable.FindField(name);
			if (index >= 0)
			{
				return index;
			}

			List<string> fieldNames = new List<string>();
			for (int iField = 0; iField < joinedTable.Fields.FieldCount; iField++)
			{
				fieldNames.Add(joinedTable.Fields.get_Field(iField).Name);
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
				if (sepIdx >= 0 && allowUnQualifyFieldNames)
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
