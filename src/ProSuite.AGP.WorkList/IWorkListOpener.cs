using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public interface IWorkListOpener
	{
		bool CanUseProductionModelIssueSchema();

		Task OpenProductionModelIssueWorkEnvironmentAsync([CanBeNull] Geometry areaOfInterest);

		Task OpenFileGdbIssueWorkListAsync(Envelope areaOfInterest,
		                                   [CanBeNull] string issuesGdbPath = null,
		                                   bool removeExisting = false);
	}
}
