using System;
using System.Collections.Generic;
using System.Globalization;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.AGP.WorkList;

// TODO: drop! move to IssueWorkListUtils
public static class IssueUtils
{
	// should this be un IssueUtils or IssueAttributeReader ?
	private static readonly string[] _idValueSeparator = { "||" };

	public static IList<InvolvedTable> ParseInvolvedTables(
		string involvedTablesString, bool hasGeometry)
	{
		// can be extended with data source identifier to allow disambiguation between datasets of same name from different data sources

		const char tableHeaderStart = '[';
		const string tableStringSeparator = ";[";
		const char tableHeaderEnd = ']';
		const char fieldNameSeparator = ':';

		string[] tableStrings = involvedTablesString.Split(
			new[] { tableStringSeparator }, StringSplitOptions.RemoveEmptyEntries);

		var result = new List<InvolvedTable>(tableStrings.Length);

		if (string.IsNullOrEmpty(involvedTablesString))
		{
			return result;
		}

		const string errorFormat = "Invalid involved tables string: '{0}'";
		var index = 0;
		foreach (string tableString in tableStrings)
		{
			int tableHeaderEndIndex = tableString.IndexOf(tableHeaderEnd, 1);
			Assert.True(tableHeaderEndIndex > 0, errorFormat, involvedTablesString);

			int fieldNameSeparatorIndex = tableString.IndexOf(fieldNameSeparator, 1);
			int tableNameStartIndex;
			if (index == 0)
			{
				Assert.AreEqual(tableHeaderStart, tableString[0], errorFormat,
				                involvedTablesString);
				tableNameStartIndex = 1;
			}
			else
			{
				tableNameStartIndex = 0;
			}

			string fieldName;
			string tableName;
			if (fieldNameSeparatorIndex > 0 && fieldNameSeparatorIndex < tableHeaderEndIndex)
			{
				// there is a key field name
				fieldName = tableString.Substring(
					fieldNameSeparatorIndex + 1,
					tableHeaderEndIndex - fieldNameSeparatorIndex - 1);
				tableName = tableString.Substring(tableNameStartIndex,
				                                  fieldNameSeparatorIndex -
				                                  tableNameStartIndex);
			}
			else
			{
				// no key field name
				fieldName = null;
				tableName = tableString.Substring(tableNameStartIndex,
				                                  tableHeaderEndIndex - tableNameStartIndex);
			}

			List<RowReference> rowReferences;

			if (tableString.Length > tableHeaderEndIndex + 1)
			{
				string idString = tableString.Substring(tableHeaderEndIndex + 1);
				string[] ids = idString.Split(_idValueSeparator,
				                              StringSplitOptions.RemoveEmptyEntries);

				rowReferences = new List<RowReference>(ids.Length);

				foreach (string id in ids)
				{
					if (fieldName == null)
					{
						int oid;
						try
						{
							oid = Convert.ToInt32(id, CultureInfo.InvariantCulture);
						}
						catch (FormatException formatException)
						{
							throw new AssertionException(
								string.Format(errorFormat, involvedTablesString),
								formatException);
						}

						rowReferences.Add(new OIDRowReference(oid, hasGeometry));
					}
				}
			}
			else
			{
				rowReferences = new List<RowReference>(0);
			}

			result.Add(new InvolvedTable(tableName, rowReferences, fieldName));
			index++;
		}

		return result;
	}
}
