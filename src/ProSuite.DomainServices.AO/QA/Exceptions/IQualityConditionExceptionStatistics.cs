using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public interface IQualityConditionExceptionStatistics
	{
		[NotNull]
		QualityCondition QualityCondition { get; }

		int ExceptionCount { get; }

		int ExceptionObjectCount { get; }

		int UnusedExceptionObjectCount { get; }

		int ExceptionObjectUsedMultipleTimesCount { get; }

		[NotNull]
		IEnumerable<ExceptionObject> UnusedExceptionObjects { get; }

		[NotNull]
		IEnumerable<ExceptionUsage> ExceptionObjectsUsedMultipleTimes { get; }

		[NotNull]
		ICollection<string> UnknownTableNames { get; }

		[NotNull]
		ICollection<ExceptionObject> GetExceptionObjectsInvolvingUnknownTableName(
			string tableName);
	}
}
