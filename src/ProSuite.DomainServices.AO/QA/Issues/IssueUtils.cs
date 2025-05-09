using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public static class IssueUtils
	{
		private static readonly string[] _idValueSeparator = { "||" };

		[NotNull]
		public static IEnumerable<InvolvedTable> GetInvolvedTables(
			[NotNull] IEnumerable<InvolvedRow> involvedRows)
		{
			var result = new List<InvolvedTable>();

			IEnumerable<KeyValuePair<string, List<InvolvedRow>>> rowsByTableName =
				InvolvedRowUtils.GroupByTableName(involvedRows);

			foreach (KeyValuePair<string, List<InvolvedRow>> pair in rowsByTableName)
			{
				List<InvolvedRow> involvedRowsForTable = pair.Value;
				var rowReferences = new List<RowReference>(involvedRowsForTable.Count);

				foreach (InvolvedRow involvedRow in involvedRowsForTable)
				{
					if (! involvedRow.RepresentsEntireTable)
					{
						rowReferences.Add(new OIDRowReference(involvedRow.OID));
					}
				}

				result.Add(new InvolvedTable(pair.Key, rowReferences));
			}

			return result;
		}

		[NotNull]
		public static string FormatInvolvedTables(
			[NotNull] IEnumerable<InvolvedTable> involvedTables)
		{
			// can be extended with data source identifier to allow disambiguation between datasets of same name from different data sources

			var sb = new StringBuilder();

			foreach (InvolvedTable involvedTable in involvedTables)
			{
				if (sb.Length > 0)
				{
					sb.Append(";");
				}

				sb.AppendFormat("[{0}", involvedTable.TableName);

				ICollection<RowReference> references = involvedTable.RowReferences;
				if (references.Count <= 0)
				{
					sb.Append("]");
					continue;
				}

				if (! string.IsNullOrEmpty(involvedTable.KeyField))
				{
					sb.AppendFormat(":{0}]{1}", involvedTable.KeyField, Format(references));
				}
				else
				{
					sb.AppendFormat("]{0}", Format(references));
				}
			}

			return sb.ToString();
		}

		[NotNull]
		private static string Format([NotNull] IEnumerable<RowReference> references)
		{
			return StringUtils.Concatenate(
				references,
				reference => string.Format(
					CultureInfo.InvariantCulture, "{0}", reference.Key), "||");
		}

		/// <summary>
		/// Parses a forrmatted string for involved tables.
		/// </summary>
		/// <param name="involvedTablesString">The involved tables string to parse</param>
		/// <param name="alternateKeyConverter">Allows conversion of parsed alternate keys (strings) to the data type of the referenced key field</param>
		/// <returns></returns>
		[NotNull]
		public static IList<InvolvedTable> ParseInvolvedTables(
			[NotNull] string involvedTablesString,
			[CanBeNull] IAlternateKeyConverter alternateKeyConverter = null)
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
							long oid;
							try
							{
								oid = Convert.ToInt64(id, CultureInfo.InvariantCulture);
							}
							catch (FormatException formatException)
							{
								throw new AssertionException(
									string.Format(errorFormat, involvedTablesString),
									formatException);
							}

							rowReferences.Add(new OIDRowReference(oid));
						}
						else
						{
							object key = alternateKeyConverter?.Convert(tableName, fieldName, id) ??
							             id;

							rowReferences.Add(new AlternateKeyRowReference(key));
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

		public static void GetValues([CanBeNull] IEnumerable<object> values,
		                             out double? doubleValue1,
		                             out double? doubleValue2,
		                             out string textValue)
		{
			doubleValue1 = null;
			doubleValue2 = null;
			textValue = null;

			if (values == null)
			{
				return;
			}

			foreach (object value in values)
			{
				if (value == null)
				{
					continue;
				}

				if (doubleValue1 == null)
				{
					doubleValue1 = TryGetDoubleValue(value);
					if (doubleValue1 != null)
					{
						continue;
					}
				}

				if (doubleValue2 == null)
				{
					doubleValue2 = TryGetDoubleValue(value);
					if (doubleValue2 != null)
					{
						continue;
					}
				}

				if (textValue == null)
				{
					var stringValue = value as string;
					if (stringValue != null)
					{
						textValue = stringValue;
						continue;
					}

					textValue = Convert.ToString(value, CultureInfo.InvariantCulture);
				}

				// if we get here then the value could not be assigned to an output
			}
		}

		private static double? TryGetDoubleValue([NotNull] object value)
		{
			if (value is double)
			{
				return (double) value;
			}

			return value is float || value is int || value is short || value is decimal
				       ? (double?) Convert.ToDouble(value)
				       : null;
		}
	}
}
