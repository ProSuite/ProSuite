using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public interface IExceptionStatistics
	{
		[NotNull]
		IWorkspace Workspace { get; }

		int ExceptionCount { get; }

		int ExceptionObjectCount { get; }

		int InactiveExceptionObjectCount { get; }

		int UnusedExceptionObjectCount { get; }

		int ExceptionObjectsUsedMultipleTimesCount { get; }

		[CanBeNull]
		IQualityConditionExceptionStatistics GetQualityConditionStatistics(
			[NotNull] QualityCondition qualityCondition);

		[NotNull]
		ICollection<IReadOnlyTable> TablesWithNonUniqueKeys { get; }

		[NotNull]
		ICollection<object> GetNonUniqueKeys([NotNull] IReadOnlyTable table);

		IDictionary<IssueGroupKey, List<ExceptionObject>> GetUsedExceptions();
	}
}
