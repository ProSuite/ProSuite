using System.Collections.Generic;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface ISelectionSet
	{
		IName FullName { get; }

		ITable Target { get; }

		void MakePermanent();

		long Count { get; }

		void Add(long oid);

		void AddList(long count, ref long oidList);

		void Combine(ISelectionSet otherSet, esriSetOperation setOp, out ISelectionSet resultSet);

		void Search(IQueryFilter queryFilter, bool recycling, out IEnumerable<IRow> cursor);

		ISelectionSet Select(
			IQueryFilter queryFilter,
			esriSelectionType selType,
			esriSelectionOption selOption,
			IWorkspace selectionContainer);

		void Refresh();

		//IEnumIDs Ds { get; }

		void RemoveList(long count, ref long oidList);
	}

	public enum esriSelectionType
	{
		esriSelectionTypeIDSet = 1,
		esriSelectionTypeSnapshot = 2,
		esriSelectionTypeHybrid = 3,
	}

	public enum esriSelectionOption
	{
		esriSelectionOptionNormal = 1,
		esriSelectionOptionOnlyOne = 2,
		esriSelectionOptionEmpty = 3,
	}

	public enum esriSetOperation
	{
		esriSetUnion = 1,
		esriSetIntersection = 2,
		esriSetDifference = 3,
		esriSetSymDifference = 4,
	}
}
