using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcSelectionSet : ISelectionSet
	{
		private readonly Selection _proSelection;
		private readonly Table _proTargetTable;

		public ArcSelectionSet(Selection proSelection, Table proTargetTable)
		{
			_proSelection = proSelection;
			_proTargetTable = proTargetTable;
		}

		public Selection ProSelection => _proSelection;

		#region Implementation of ISelectionSet

		public IName FullName => new ArcName(ArcUtils.ToArcTable(_proTargetTable));

		public ITable Target => ArcUtils.ToArcTable(_proTargetTable);

		public void MakePermanent()
		{
			throw new NotImplementedException();
			//_proSelection.MakePermanent();
		}

		public long Count => _proSelection.GetCount();

		public void Add(long oid)
		{
			_proSelection.Add(new[] { oid });
		}

		public void AddList(long count, ref long oidList)
		{
			throw new NotImplementedException();
			//_proSelection.AddList(count, ref oidList);
		}

		public void Combine(ISelectionSet otherSet, esriSetOperation setOp,
		                    out ISelectionSet resultSet)
		{
			Selection proResultSelection = ((ArcSelectionSet) otherSet).ProSelection.Combine(
				_proSelection, (SetOperation) setOp);

			resultSet = new ArcSelectionSet(proResultSelection, _proTargetTable);
		}

		public void Search(IQueryFilter queryFilter, bool recycling, out IEnumerable<IRow> result)
		{
			var aoQueryFilter = ((ArcQueryFilter) queryFilter).ProQueryFilter;

			RowCursor rowCursor = _proSelection.Search(aoQueryFilter, recycling);

			result = ArcUtils.GetArcRows(rowCursor);
		}

		public ISelectionSet Select(IQueryFilter queryFilter, esriSelectionType selType,
		                            esriSelectionOption selOption,
		                            IWorkspace selectionContainer)
		{
			var aoQueryFilter = ((ArcQueryFilter) queryFilter).ProQueryFilter;

			var aoSelectionSet = _proSelection.Select(
				aoQueryFilter, (SelectionOption) selOption);

			return new ArcSelectionSet(aoSelectionSet, _proTargetTable);
		}

		public void Refresh()
		{
			throw new NotImplementedException();
			//_proSelection.Refresh();
		}

		public void RemoveList(long count, ref long oidList)
		{
			throw new NotImplementedException();
			//_proSelection.Remove(count, ref oidList);
		}

		#endregion
	}
}
