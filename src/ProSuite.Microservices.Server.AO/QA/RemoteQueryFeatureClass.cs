using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class RemoteQueryFeatureClass : GdbFeatureClass, ITransformedTableBasedOnTables
	{
		[NotNull] private readonly IList<IReadOnlyTable> _baseTables;

		public RemoteQueryFeatureClass(
			int? objectClassId,
			[NotNull] string name,
			esriGeometryType shapeType,
			[CanBeNull] string aliasName,
			[NotNull] Func<GdbTable, BackingDataset> createBackingDataset,
			[NotNull] IWorkspace workspace,
			[NotNull] IList<IReadOnlyTable> baseTables)
			: base(objectClassId, name, shapeType, aliasName, createBackingDataset, workspace)
		{
			_baseTables = baseTables;
		}

		public IEnumerable<Involved> GetBaseRowReferences(IReadOnlyRow forTransformedRow)
		{
			return InvolvedRowUtils.GetInvolvedRowsFromJoinedRow(forTransformedRow, _baseTables);
		}
	}
}