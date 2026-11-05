using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.TablesBased
{
	public static class TableBasedUtils
	{
		public static IEnumerable<Involved> GetInvolvedRowsFromJoinedRow(
			[NotNull] IReadOnlyRow joinedRow,
			[NotNull] IEnumerable<IReadOnlyTable> involvedTables,
			[NotNull] Func<string, int> findFieldFunc)
		{
			foreach (IReadOnlyTable involvedTable in involvedTables)
			{
				string oidFieldName = involvedTable.OIDFieldName;

				string oidFieldQualified =
					string.IsNullOrEmpty(oidFieldName)
						? null // no OID field
						: DatasetUtils.QualifyFieldName(involvedTable, oidFieldName);

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
					yield return new InvolvedRow(involvedTable.Name, oidValue.Value);
				}
			}
		}

		/// <summary>
		/// Finds a field in a joined table and returns the index of the field, or -1.
		/// In a joined table, each field name starts with the name of the table that the field
		/// comes from, for example "TOPGIS_TLM.TLM_STRASSE.OBJEKTART". In this example, the
		/// qualifiers are "TOPGIS_TLM." (the owner) and "TLM_STRASSE." (the table name).
		/// The search has three steps:
		/// <list type="number">
		/// <item>Find the field whose name is exactly the specified name.</item>
		/// <item>Find the field whose name is the specified name with additional qualifiers in
		/// front. Example: the specified name "TLM_STRASSE.OBJEKTART" finds the field
		/// "TOPGIS_TLM.TLM_STRASSE.OBJEKTART". The field is only returned if exactly one field
		/// matches.</item>
		/// <item>Only if allowUnQualifyFieldNames is true: remove the first qualifier from the
		/// specified name and repeat step 2. Repeat until exactly one field matches or the
		/// specified name has no qualifier left.</item>
		/// </list>
		/// Step 3 exists so that the same quality condition works in an enterprise geodatabase
		/// and in a file geodatabase, for example in a checked-out copy. In a file geodatabase,
		/// the table names have no owner. The field is named "TLM_STRASSE.OBJEKTART", not
		/// "TOPGIS_TLM.TLM_STRASSE.OBJEKTART". A constraint that was written for the enterprise
		/// geodatabase contains "TOPGIS_TLM.TLM_STRASSE.OBJEKTART". Step 3 removes "TOPGIS_TLM."
		/// from the name in the constraint and then finds the field "TLM_STRASSE.OBJEKTART".
		/// TrTableJoinTest runs the same constraints in both kinds of geodatabase.
		/// Side effect of step 3: a constraint with a wrong table name is not reported as an
		/// error if only one table in the join has a field with the same name.
		/// </summary>
		public static int FindFieldInJoin([NotNull] ITable joinedTable,
		                                  [NotNull] string name,
		                                  bool allowUnQualifyFieldNames)
		{
			int index = joinedTable.FindField(name);
			if (index >= 0)
			{
				return index;
			}

			return FindUniqueFieldInJoin(joinedTable.Fields, name, allowUnQualifyFieldNames);
		}

		/// <summary>
		/// Finds a field in the fields of a joined table with the same three steps as
		/// <see cref="FindFieldInJoin(ITable,string,bool)"/>. Use this method for a joined
		/// table that is not an ArcObjects table, for example the joined table that the server
		/// builds from the schema that the client sends.
		/// </summary>
		public static int FindFieldInJoin([NotNull] IFields joinedTableFields,
		                                  [NotNull] string name,
		                                  bool allowUnQualifyFieldNames)
		{
			int index = joinedTableFields.FindField(name);
			if (index >= 0)
			{
				return index;
			}

			return FindUniqueFieldInJoin(joinedTableFields, name, allowUnQualifyFieldNames);
		}

		/// <summary>
		/// Performs steps 2 and 3 of the search that is described at
		/// <see cref="FindFieldInJoin(ITable,string,bool)"/>. Returns the index of the field,
		/// or -1 if no field matches or more than one field matches.
		/// </summary>
		private static int FindUniqueFieldInJoin([NotNull] IFields joinedTableFields,
		                                         [NotNull] string name,
		                                         bool allowUnQualifyFieldNames)
		{
			List<string> fieldNames = new List<string>();
			for (int iField = 0; iField < joinedTableFields.FieldCount; iField++)
			{
				fieldNames.Add(joinedTableFields.get_Field(iField).Name);
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

				// TODO: The following code removes the first qualifier of the search name.
				// Removing the owner (for example "TOPGIS_GC.") is intended. Removing the table
				// name is not: a wrong table name in a constraint is then not reported.
				// Example: GC_0103 (GeoCover) uses the relationship class name
				// GC_BEDR_GEOL_MAPPING_UNIT_ATT as table name. This relationship class is
				// one-to-many and has no table.
				// Improvement: remove only the owner. Minimum: log a warning if the table name
				// in the found field differs from the table name in the constraint.
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
