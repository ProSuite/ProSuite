extern alias EsriGeodatabase;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geodatabase.AO;

namespace ProSuite.ArcGIS.Geodatabase.AO
{
	public class ArcSelectionSet : ISelectionSet
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ISelectionSet _aoSelectionSet;

		public ArcSelectionSet(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ISelectionSet aoSelectionSet)
		{
			_aoSelectionSet = aoSelectionSet;
		}

		public EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ISelectionSet AoSelectionSet => _aoSelectionSet;

		#region Implementation of ISelectionSet

		public IName FullName => new ArcName(_aoSelectionSet.FullName);

		public ITable Target => ArcUtils.ToArcTable(_aoSelectionSet.Target);

		public void MakePermanent()
		{
			_aoSelectionSet.MakePermanent();
		}

		public long Count => _aoSelectionSet.Count;

		public void Add(long oid)
		{
			_aoSelectionSet.Add(oid);
		}

		public void AddList(long count, ref long oidList)
		{
			_aoSelectionSet.AddList(count, ref oidList);
		}

		public void Combine(ISelectionSet otherSet, esriSetOperation setOp, out ISelectionSet resultSet)
		{
			((ArcSelectionSet)otherSet).AoSelectionSet.Combine(
				_aoSelectionSet, (EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriSetOperation)setOp,
				out var aoResultSet);

			resultSet = new ArcSelectionSet(aoResultSet);
		}

		public void Search(IQueryFilter queryFilter, bool recycling, out IEnumerable<IRow> result)
		{
			var aoQueryFilter = ((ArcQueryFilter)queryFilter).AoQueryFilter;

			_aoSelectionSet.Search(aoQueryFilter, recycling, out var aoCursor);

			result = ArcUtils.GetArcRows(aoCursor);
		}

		public ISelectionSet Select(IQueryFilter queryFilter, esriSelectionType selType, esriSelectionOption selOption,
			IWorkspace selectionContainer)
		{
			var aoQueryFilter = ((ArcQueryFilter)queryFilter).AoQueryFilter;
			var aoWorkspace = ((ArcWorkspace)selectionContainer).AoWorkspace;

			var aoSelectionSet = _aoSelectionSet.Select(
				aoQueryFilter,
				(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriSelectionType)selType,
				(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.esriSelectionOption)selOption, aoWorkspace);

			return new ArcSelectionSet(aoSelectionSet);
		}

		public void Refresh()
		{
			_aoSelectionSet.Refresh();
		}

		public void RemoveList(long count, ref long oidList)
		{
			_aoSelectionSet.RemoveList(count, ref oidList);
		}

		#endregion
	}
}
