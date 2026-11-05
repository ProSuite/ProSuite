using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class RemoteQueryTable : GdbTable, ITableBased
	{
		private readonly IList<IReadOnlyTable> _baseTables;

		public RemoteQueryTable(
			int? objectClassId,
			[NotNull] string name,
			[CanBeNull] string aliasName,
			[NotNull] Func<GdbTable, BackingDataset> createBackingDataset,
			[NotNull] IWorkspace workspace,
			[NotNull] IList<IReadOnlyTable> baseTables)
			: base(objectClassId, name, aliasName, createBackingDataset, workspace)
		{
			_baseTables = baseTables;
		}

		/// <summary>
		/// Finds a field in the same way as <see cref="ReadOnlyJoinedTable.FindField"/>.
		/// ReadOnlyJoinedTable is the joined table that the server uses when the server reads
		/// the data itself. With the same search, a quality condition finds the same fields
		/// with data from the client as with data that the server reads itself.
		/// </summary>
		public override int FindField(string name)
		{
			return TableBasedUtils.FindFieldInJoin(Fields, name, allowUnQualifyFieldNames: true);
		}

		#region Implementation of ITableBased

		public IList<IReadOnlyTable> GetInvolvedTables()
		{
			return _baseTables;
		}

		public IEnumerable<Involved> GetInvolvedRows(IReadOnlyRow forTransformedRow)
		{
			// A joined table contains an OBJECTID field from each base table. Removing the
			// table name from a name such as "GC_BEDROCK.OBJECTID" can find the OBJECTID field
			// of the other base table. This caused GEN-3538: an issue referenced the wrong
			// row. Therefore, the search for the OBJECTID fields does not remove qualifiers.
			Func<string, int> findFieldFunc =
				fieldName =>
					TableBasedUtils.FindFieldInJoin(Fields, fieldName,
					                                allowUnQualifyFieldNames: false);

			return TableBasedUtils.GetInvolvedRowsFromJoinedRow(
				forTransformedRow, _baseTables, findFieldFunc);
		}

		#endregion
	}
}
