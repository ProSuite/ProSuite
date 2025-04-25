using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IRowCache
	{
		void Invalidate();

		void Invalidate(IEnumerable<Table> tables);

		void ProcessChanges([NotNull] Dictionary<Table, List<long>> inserts,
		                    [NotNull] Dictionary<Table, List<long>> deletes,
		                    [NotNull] Dictionary<Table, List<long>> updates);

		bool CanContain([NotNull] Table table);
	}
}
