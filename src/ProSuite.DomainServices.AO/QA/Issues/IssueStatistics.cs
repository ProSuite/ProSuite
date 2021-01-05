using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Exceptions;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueStatistics : IIssueStatistics
	{
		[NotNull] private readonly Dictionary<IssueGroupKey, Value> _issueGroupIssueCounts =
			new Dictionary<IssueGroupKey, Value>();

		[CanBeNull] private IDictionary<IssueGroupKey, List<ExceptionObject>> _usedExceptions;

		[NotNull] private readonly List<ExceptionCategory> _exceptionCategories =
			new List<ExceptionCategory>();

		public void AddIssue([NotNull] Issue issue)
		{
			UpdateIssueGroups(issue);

			if (issue.Allowable)
			{
				WarningCount++;
			}
			else
			{
				ErrorCount++;
			}
		}

		internal void IncludeUsedExceptions(
			[NotNull] IDictionary<IssueGroupKey, List<ExceptionObject>> usedExceptions)
		{
			ExceptionCount = usedExceptions.Values.Sum(
				exceptionObjects => exceptionObjects.Count);

			_usedExceptions = usedExceptions;
			// TODO build list of ALL exception categories, sorted

			var set = new HashSet<ExceptionCategory>();
			foreach (List<ExceptionObject> eos in usedExceptions.Values)
			{
				foreach (ExceptionObject eo in eos)
				{
					set.Add(new ExceptionCategory(eo.ExceptionCategory));
				}
			}

			_exceptionCategories.Clear();
			_exceptionCategories.AddRange(set);
			_exceptionCategories.Sort();
		}

		public IList<ExceptionCategory> ExceptionCategories => _exceptionCategories;

		private void UpdateIssueGroups([NotNull] Issue issue)
		{
			var key = new IssueGroupKey(issue);

			Value value;
			if (! _issueGroupIssueCounts.TryGetValue(key, out value))
			{
				value = new Value();
				_issueGroupIssueCounts.Add(key, value);
			}

			value.IssueCount++;
		}

		public int IssueCount => WarningCount + ErrorCount;

		public int WarningCount { get; private set; }

		public int ErrorCount { get; private set; }

		public int ExceptionCount { get; private set; }

		public IEnumerable<IssueGroup> GetIssueGroups()
		{
			foreach (IssueGroupKey issueGroupKey in GetAllIssueGroups())
			{
				yield return new IssueGroup(issueGroupKey.QualityCondition,
				                            issueGroupKey.IssueCode,
				                            issueGroupKey.AffectedComponent,
				                            issueGroupKey.IssueDescription,
				                            issueGroupKey.Allowable,
				                            issueGroupKey.StopCondition,
				                            GetIssueCount(issueGroupKey),
				                            GetUsedExceptions(issueGroupKey));
			}
		}

		[NotNull]
		private IEnumerable<IssueGroupKey> GetAllIssueGroups()
		{
			var result = new HashSet<IssueGroupKey>(_issueGroupIssueCounts.Keys);

			if (_usedExceptions != null)
			{
				result.UnionWith(_usedExceptions.Keys);
			}

			return result;
		}

		private int GetIssueCount([NotNull] IssueGroupKey issueGroupKey)
		{
			Value value;
			return _issueGroupIssueCounts.TryGetValue(issueGroupKey, out value)
				       ? value.IssueCount
				       : 0;
		}

		[CanBeNull]
		private IEnumerable<ExceptionObject> GetUsedExceptions(
			[NotNull] IssueGroupKey issueGroupKey)
		{
			if (_usedExceptions == null)
			{
				return null;
			}

			List<ExceptionObject> usedExceptions;
			return _usedExceptions.TryGetValue(issueGroupKey, out usedExceptions)
				       ? usedExceptions
				       : null;
		}

		private class Value
		{
			public int IssueCount;
		}
	}
}
