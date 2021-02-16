using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionStatistics : IExceptionStatistics,
	                                   IExceptionEvaluationStatistics
	{
		[NotNull] private readonly IWorkspace _workspace;

		[NotNull] private readonly
			IDictionary<QualityCondition, QualityConditionExceptionStatistics>
			_conditionExceptionStatistics =
				new Dictionary<QualityCondition, QualityConditionExceptionStatistics>();

		[NotNull] private readonly IDictionary<ITable, HashSet<object>>
			_nonUniqueKeysByTable = new Dictionary<ITable, HashSet<object>>();

		[NotNull] private readonly IDictionary<string, int> _usedExceptionsByExceptionCategory =
			new Dictionary<string, int>();

		[NotNull] private readonly IDictionary<IssueGroupKey, List<ExceptionObject>> _usedExceptions
			=
			new Dictionary<IssueGroupKey, List<ExceptionObject>>();

		public ExceptionStatistics([NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			_workspace = workspace;
		}

		IWorkspace IExceptionStatistics.Workspace => _workspace;

		public int ExceptionObjectCount { get; private set; }

		public int InactiveExceptionObjectCount { get; private set; }

		public int ExceptionCount { get; private set; }

		public int UnusedExceptionObjectCount
		{
			get
			{
				return _conditionExceptionStatistics.Values.Sum(
					s => s.UnusedExceptionObjectCount);
			}
		}

		public int ExceptionObjectsUsedMultipleTimesCount
		{
			get
			{
				return _conditionExceptionStatistics.Values.Sum(
					s => s
						.ExceptionObjectUsedMultipleTimesCount);
			}
		}

		IQualityConditionExceptionStatistics IExceptionStatistics.
			GetQualityConditionStatistics(QualityCondition qualityCondition)
		{
			QualityConditionExceptionStatistics result;
			return ! _conditionExceptionStatistics.TryGetValue(qualityCondition, out result)
				       ? null
				       : result;
		}

		ICollection<ITable> IExceptionStatistics.TablesWithNonUniqueKeys
			=> _nonUniqueKeysByTable.Keys;

		ICollection<object> IExceptionStatistics.GetNonUniqueKeys(ITable table)
		{
			HashSet<object> keys;
			return _nonUniqueKeysByTable.TryGetValue(table, out keys)
				       ? (ICollection<object>) keys
				       : new List<object>();
		}

		void IExceptionEvaluationStatistics.AddUsedException(
			ExceptionObject exceptionObject, QualitySpecificationElement element,
			QaError qaError)
		{
			ExceptionCount++;

			var issueGroupKey = new IssueGroupKey(new Issue(element, qaError));

			List<ExceptionObject> usedExceptionObjects;
			if (! _usedExceptions.TryGetValue(issueGroupKey, out usedExceptionObjects))
			{
				usedExceptionObjects = new List<ExceptionObject>();
				_usedExceptions[issueGroupKey] = usedExceptionObjects;
			}

			usedExceptionObjects.Add(exceptionObject);

			string key = exceptionObject.ExceptionCategory?.Trim() ?? string.Empty;

			int count;
			count = _usedExceptionsByExceptionCategory.TryGetValue(key, out count)
				        ? count + 1
				        : 1;
			_usedExceptionsByExceptionCategory[key] = count;

			GetStatistics(element.QualityCondition).AddUsedException(exceptionObject);
		}

		void IExceptionEvaluationStatistics.AddExceptionObject(
			ExceptionObject exceptionObject, QualityCondition qualityCondition)
		{
			ExceptionObjectCount++;

			GetStatistics(qualityCondition).AddExceptionObject(exceptionObject);
		}

		public int GetUsageCount(ExceptionObject exceptionObject,
		                         QualityCondition qualityCondition)
		{
			return GetStatistics(qualityCondition).GetUsageCount(exceptionObject);
		}

		public void ReportInactiveException([NotNull] ExceptionObject exceptionObject)
		{
			InactiveExceptionObjectCount++;
		}

		public void ReportNonUniqueKey([NotNull] ITable table, [NotNull] object key)
		{
			HashSet<object> keys;
			if (! _nonUniqueKeysByTable.TryGetValue(table, out keys))
			{
				keys = new HashSet<object>();
				_nonUniqueKeysByTable.Add(table, keys);
			}

			keys.Add(key);
		}

		public void ReportExceptionInvolvingUnknownTable(
			[NotNull] ExceptionObject exceptionObject,
			[NotNull] string tableName,
			[NotNull] QualityCondition qualityCondition)
		{
			GetStatistics(qualityCondition).ReportExceptionInvolvingUnknownTable(
				exceptionObject, tableName);
		}

		[NotNull]
		private QualityConditionExceptionStatistics GetStatistics(
			[NotNull] QualityCondition qualityCondition)
		{
			QualityConditionExceptionStatistics result;
			if (! _conditionExceptionStatistics.TryGetValue(qualityCondition, out result))
			{
				result = new QualityConditionExceptionStatistics(qualityCondition);
				_conditionExceptionStatistics.Add(qualityCondition, result);
			}

			return result;
		}

		public IDictionary<IssueGroupKey, List<ExceptionObject>> GetUsedExceptions()
		{
			return _usedExceptions;
		}
	}
}
