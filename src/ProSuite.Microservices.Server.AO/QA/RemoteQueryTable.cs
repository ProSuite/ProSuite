using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geodatabase.TableBased;
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

		#region Implementation of ITableBased

		public IList<IReadOnlyTable> GetInvolvedTables()
		{
			return _baseTables;
		}

		public IEnumerable<Involved> GetInvolvedRows(IReadOnlyRow forTransformedRow)
		{
			Func<string, int> findFieldFunc =
				fieldName => forTransformedRow.Table.FindField(fieldName);

			return TableBasedUtils.GetInvolvedRowsFromJoinedRow(
				forTransformedRow, _baseTables, findFieldFunc);
		}

		#endregion
	}
}
