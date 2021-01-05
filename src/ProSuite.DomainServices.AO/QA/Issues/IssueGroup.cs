using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueGroup
	{
		[NotNull] private readonly List<ExceptionObject> _usedExceptionObjects;

		public IssueGroup([NotNull] QualityCondition qualityCondition,
		                  [CanBeNull] IssueCode issueCode,
		                  [CanBeNull] string affectedComponent,
		                  [CanBeNull] string issueDescription,
		                  bool allowable,
		                  bool stopCondition,
		                  int issueCount,
		                  [CanBeNull] IEnumerable<ExceptionObject> usedExceptionObjects =
			                  null)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			QualityCondition = qualityCondition;
			IssueCode = issueCode;
			AffectedComponent = affectedComponent;
			IssueDescription = issueDescription;
			Allowable = allowable;
			StopCondition = stopCondition;
			IssueCount = issueCount;

			_usedExceptionObjects = usedExceptionObjects?.ToList() ?? new List<ExceptionObject>();
		}

		[NotNull]
		public QualityCondition QualityCondition { get; }

		[CanBeNull]
		public IssueCode IssueCode { get; }

		[CanBeNull]
		public string AffectedComponent { get; }

		[CanBeNull]
		public string IssueDescription { get; }

		public bool Allowable { get; }

		public bool StopCondition { get; }

		public int IssueCount { get; }

		public int ExceptionCount => _usedExceptionObjects.Count;

		public int GetExceptionCount([NotNull] ExceptionCategory exceptionCategory)
		{
			return _usedExceptionObjects.Count(eo => Equals(
				                                   new ExceptionCategory(eo.ExceptionCategory),
				                                   exceptionCategory));
		}
	}
}
