using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class RemoteQueryTable : GdbTable, ITransformedTableBasedOnTables
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

		public IEnumerable<Involved> GetBaseRowReferences(IReadOnlyRow forTransformedRow)
		{
			return InvolvedRowUtils.GetInvolvedRowsFromJoinedRow(forTransformedRow, _baseTables);
		}
	}
}