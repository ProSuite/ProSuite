using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase.Distributed
{
	public interface ICheckInDelta
	{
		/// <summary>
		/// The version in the replica parent to which the delta is applied.
		/// </summary>
		IWorkspace TargetWorkspace { get; }

		/// <summary>
		/// The names of the modified classes.
		/// </summary>
		IList<string> ModifiedObjectClasses { get; }

		/// <summary>
		/// Returns the changed rows in the specified workspace.
		/// </summary>
		/// <param name="workspace"></param>
		/// <returns></returns>
		IEnumerable<IRow> GetChangedRows(IWorkspace workspace);
	}
}
