using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	[CLSCompliant(false)]
	public interface IExceptionStatistics
	{
		[NotNull]
		[CLSCompliant(false)]
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
		ICollection<ITable> TablesWithNonUniqueKeys { get; }

		[NotNull]
		ICollection<object> GetNonUniqueKeys([NotNull] ITable table);

		IDictionary<IssueGroupKey, List<ExceptionObject>> GetUsedExceptions();
	}
}
