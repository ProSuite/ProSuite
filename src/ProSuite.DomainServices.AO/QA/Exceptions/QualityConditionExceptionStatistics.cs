using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class QualityConditionExceptionStatistics :
		IQualityConditionExceptionStatistics
	{
		[NotNull] private readonly IDictionary<ExceptionObject, ExceptionUsage>
			_usedExceptionObjects =
				new Dictionary<ExceptionObject, ExceptionUsage>();

		[NotNull] private readonly HashSet<ExceptionObject> _pendingExceptionObjects =
			new HashSet<ExceptionObject>();

		private int? _unusedExceptionObjectCount;

		[NotNull] private readonly IDictionary<string, List<ExceptionObject>>
			_exceptionObjectsInvolvingUnknownTable =
				new Dictionary<string, List<ExceptionObject>>(StringComparer.OrdinalIgnoreCase);

		#region Constructor

		public QualityConditionExceptionStatistics(
			[NotNull] QualityCondition qualityCondition)
		{
			QualityCondition = qualityCondition;
		}

		#endregion

		public QualityCondition QualityCondition { get; }

		public int ExceptionCount { get; private set; }

		public int ExceptionObjectCount { get; private set; }

		public int UnusedExceptionObjectCount
		{
			get
			{
				if (_unusedExceptionObjectCount == null)
				{
					// count only the unused exception objects that intersect the area of interest
					_unusedExceptionObjectCount =
						_pendingExceptionObjects.Count(e => e.IntersectsAreaOfInterest);
				}

				return _unusedExceptionObjectCount.Value;
			}
		}

		public int ExceptionObjectUsedMultipleTimesCount
			=> ExceptionObjectsUsedMultipleTimes.Count();

		public IEnumerable<ExceptionObject> UnusedExceptionObjects
		{
			get
			{
				// exclude the exception objects located in the tolerance buffer outside the area of interest
				return _pendingExceptionObjects.Where(e => e.IntersectsAreaOfInterest);
			}
		}

		public IEnumerable<ExceptionUsage> ExceptionObjectsUsedMultipleTimes
			=> _usedExceptionObjects.Values.Where(usage => usage.UsageCount > 1);

		public ICollection<string> UnknownTableNames
			=> _exceptionObjectsInvolvingUnknownTable.Keys;

		public ICollection<ExceptionObject> GetExceptionObjectsInvolvingUnknownTableName(
			string tableName)
		{
			Assert.ArgumentNotNullOrEmpty(tableName, nameof(tableName));

			List<ExceptionObject> list;
			return _exceptionObjectsInvolvingUnknownTable.TryGetValue(tableName, out list)
				       ? list
				       : new List<ExceptionObject>();
		}

		public void AddUsedException([NotNull] ExceptionObject exceptionObject)
		{
			ExceptionCount++;

			_pendingExceptionObjects.Remove(exceptionObject);

			ExceptionUsage exceptionUsage;
			if (! _usedExceptionObjects.TryGetValue(exceptionObject, out exceptionUsage))
			{
				exceptionUsage = new ExceptionUsage(exceptionObject);
				_usedExceptionObjects.Add(exceptionObject, exceptionUsage);
			}

			exceptionUsage.AddUsage(); // add the usage
		}

		public void AddExceptionObject([NotNull] ExceptionObject exceptionObject)
		{
			bool added = _pendingExceptionObjects.Add(exceptionObject);
			_unusedExceptionObjectCount = null;

			Assert.True(added, "Exception already added: {0}", exceptionObject);

			ExceptionObjectCount++;
		}

		public void ReportExceptionInvolvingUnknownTable(
			[NotNull] ExceptionObject exceptionObject,
			[NotNull] string tableName)
		{
			Assert.ArgumentNotNull(exceptionObject, nameof(exceptionObject));
			Assert.ArgumentNotNullOrEmpty(tableName, nameof(tableName));

			List<ExceptionObject> list;
			if (! _exceptionObjectsInvolvingUnknownTable.TryGetValue(tableName, out list))
			{
				list = new List<ExceptionObject>();
				_exceptionObjectsInvolvingUnknownTable.Add(tableName, list);
			}

			list.Add(exceptionObject);
		}

		public int GetUsageCount([NotNull] ExceptionObject exceptionObject)
		{
			ExceptionUsage usage;
			return _usedExceptionObjects.TryGetValue(exceptionObject, out usage)
				       ? usage.UsageCount
				       : 0;
		}
	}
}
