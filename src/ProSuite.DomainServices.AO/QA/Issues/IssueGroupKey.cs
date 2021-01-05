using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueGroupKey : IEquatable<IssueGroupKey>
	{
		public IssueGroupKey([NotNull] Issue issue)
		{
			QualityCondition = issue.QualityCondition;
			AffectedComponent = issue.AffectedComponent;

			// if the quality condition supports that: the issue description also
			IssueDescription = QualityCondition.CanGroupIssuesByDescription
				                   ? issue.Description
				                   : null;
			Allowable = issue.Allowable;
			StopCondition = issue.StopCondition;
			IssueCode = issue.IssueCode;
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

		public bool Equals(IssueGroupKey other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other.QualityCondition, QualityCondition) &&
			       Equals(other.IssueCode, IssueCode) &&
			       Equals(other.AffectedComponent, AffectedComponent) &&
			       Equals(other.IssueDescription, IssueDescription);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != typeof(IssueGroupKey))
			{
				return false;
			}

			return Equals((IssueGroupKey) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = QualityCondition.GetHashCode();
				result = (result * 397) ^ (IssueCode?.GetHashCode() ?? 0);
				result = (result * 397) ^ (AffectedComponent?.GetHashCode() ?? 0);
				result = (result * 397) ^ (IssueDescription?.GetHashCode() ?? 0);
				return result;
			}
		}
	}
}
