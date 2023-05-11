using System;
using System.Collections.Generic;
using System.Text;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;

namespace ProSuite.DomainModel.AO.QA
{
	public static class RowParser
	{
		private const char _delimiter = ';';
		private const string _overflow = "+";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static string Format([CanBeNull] IEnumerable<InvolvedRow> involvedRows,
		                            int maxLength = -1)
		{
			if (involvedRows == null)
			{
				return string.Empty;
			}

			var sb = new StringBuilder();

			AppendFormat(sb, involvedRows, maxLength);

			return sb.ToString();
		}

		[NotNull]
		public static string Format([NotNull] ITest test,
		                            int testIndex,
		                            [CanBeNull] IEnumerable<InvolvedRow> involvedRows,
		                            int maxLength = -1)
		{
			if (involvedRows == null)
			{
				return string.Empty;
			}

			var sb = new StringBuilder();

			sb.AppendFormat("{0}{1}{2}{1}", testIndex, _delimiter, test.GetType());

			AppendFormat(sb, involvedRows, maxLength);

			return sb.ToString();
		}

		[NotNull]
		public static InvolvedRows Parse([NotNull] string involvedObjectsString)
		{
			Assert.ArgumentNotNull(involvedObjectsString, nameof(involvedObjectsString));

			// TODO: make more explicit, distribute test and involved objects
			//       in separate fields
			// General idea: if the first two entries is a <testIndex, TestName> tupel
			//               they have to be removed here (used in TestIndex)

			IList<string> values = involvedObjectsString.Split(_delimiter);

			var result = new InvolvedRows();

			var valueIndex = 0;
			if (valueIndex < values.Count && int.TryParse(values[valueIndex], out int _))
			{
				// values[valueIdx + 1] is TestName
				valueIndex += 2;
			}

			string activeTable = null;
			var tableRowCount = 0;
			while (valueIndex < values.Count)
			{
				string tableNameOrOid = values[valueIndex];
				int oid;
				if (int.TryParse(tableNameOrOid, out oid))
				{
					if (activeTable == null)
					{
						throw new InvalidOperationException("Invalid involvedObjectsString: " +
						                                    involvedObjectsString);
					}

					result.Add(new InvolvedRow(activeTable, oid));
					tableRowCount++;
				}
				else if (tableNameOrOid == _overflow)
				{
					if (valueIndex != values.Count)
					{
						_msg.DebugFormat("'{0}' not as last entry", _overflow);
					}

					result.HasAdditionalRows = true;
					activeTable = tableNameOrOid;
					tableRowCount = 0;
				}
				else
				{
					if (tableRowCount == 0 && activeTable != null)
					{
						_msg.DebugFormat("'{0}' (no OID) follows table '{1}'", tableNameOrOid,
						                 activeTable);
					}

					activeTable = tableNameOrOid;
					tableRowCount = 0;
				}

				valueIndex++;
			}

			return result;
		}

		public static int GetTestIndex([NotNull] string involvedObjectsString)
		{
			string[] values = involvedObjectsString.Split(_delimiter);

			if (values.Length > 0)
			{
				int testIndex;
				if (int.TryParse(values[0], out testIndex))
				{
					return testIndex;
				}
			}

			return -1;
		}

		private static void AppendFormat([NotNull] StringBuilder sb,
		                                 [NotNull] IEnumerable<InvolvedRow> involvedRows,
		                                 int maxLength)
		{
			// purpose of rowsDict:
			// Sort the involvedRows so, that all rows of a table follow each other, 
			// but otherwise the sequence stays as in involvedRows
			var rowsDict = new Dictionary<string, IList<InvolvedRow>>();
			foreach (InvolvedRow involvedRow in involvedRows)
			{
				IList<InvolvedRow> tableRows;
				if (! rowsDict.TryGetValue(involvedRow.TableName, out tableRows))
				{
					tableRows = new List<InvolvedRow>();
					rowsDict.Add(involvedRow.TableName, tableRows);
				}

				tableRows.Add(involvedRow);
			}

			string lastTableName = null;
			foreach (IList<InvolvedRow> tableRows in rowsDict.Values)
			{
				foreach (InvolvedRow row in tableRows)
				{
					string append;
					if (row.TableName != lastTableName)
					{
						append = string.Format("{0}{1}{2}{1}", row.TableName, _delimiter, row.OID);
					}
					else
					{
						append = string.Format("{0}{1}", row.OID, _delimiter);
					}

					if (maxLength > 0 && sb.Length + append.Length > maxLength)
					{
						sb.Append(_overflow);
						break;
					}

					sb.Append(append);

					lastTableName = row.TableName;
				}
			}
		}
	}
}
