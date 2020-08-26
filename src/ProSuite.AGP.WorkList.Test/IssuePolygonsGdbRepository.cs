using System;
using System.Collections.Generic;
using System.Globalization;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Storage;

namespace ProSuite.AGP.WorkList.Test
{
	public class IssuePolygonsGdbRepository : GdbRepository<IssueItem, FeatureClass>
	{
		private readonly IList<GdbRowIdentity> _issueStates;
		private readonly string _issueStatePath;

		private AttributeReader _attributeReader {
			get
			{
				var tableDef = GdbTableDefinition as TableDefinition;
				return new AttributeReader(tableDef as TableDefinition,
				                           //Attributes.ObjectID,
				                           Attributes.QualityConditionName,
				                           Attributes.InvolvedObjects,
				                           Attributes.IssueSeverity,
				                           Attributes.IssueCode
				);
			}
		}

	public IssuePolygonsGdbRepository(string gdbPath, string className = null) : base(gdbPath, className)
		{
		}

		public IssuePolygonsGdbRepository(IssueWorkListDefinition workListDef, string className = null) : base(workListDef.FgdbPath, className)
		{
			// open XML repository for IssueWorklistDefinition?
			_issueStates = workListDef.VisitedItems;
			_issueStatePath = workListDef.Path;
		}

		public override IssueItem ParseRow(Row currentRow)
		{
			var item = new IssueItem((int)currentRow.GetObjectID(), currentRow, _attributeReader);
			item.InIssueInvolvedTables = ParseInvolvedTables(item.InvolvedObjects);
			return item;
		}

		public override Row CreateRow(IssueItem item)
		{
			return null;
		}

		private static readonly string[] _idValueSeparator = { "||" };
		//public IList<InvolvedTable> ParseInvolvedTables( string involvedTablesString,
		//	  IAlternateKeyConverter alternateKeyConverter = null)
		public IList<InvolvedTable> ParseInvolvedTables(string involvedTablesString)
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
													  fieldNameSeparatorIndex - tableNameStartIndex);
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

							rowReferences.Add(new OIDRowReference(oid));
						}
						// TODO algr: is actual?
						//else
						//{
						//	object key = alternateKeyConverter?.Convert(tableName, fieldName, id) ?? id;

						//	rowReferences.Add(new AlternateKeyRowReference(key));
						//}
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


}

