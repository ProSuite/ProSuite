using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Standalone
{
	public class IssueProcessor
	{
		[NotNull] private readonly Dictionary<QualityCondition, IssueStats>
			_issueStatsByCondition = new Dictionary<QualityCondition, IssueStats>();

		[NotNull] private readonly RowsWithStopConditions _rowsWithStopConditions =
			new RowsWithStopConditions();

		[NotNull] private readonly IDictionary<ITest, QualitySpecificationElement>
			_elementsByTest;

		[CanBeNull] private readonly IGeometry _testPerimeter;
		[CanBeNull] private readonly IExceptionObjectEvaluator _exceptionObjectEvaluator;
		[NotNull] private readonly IIssueWriter _issueWriter;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public IssueProcessor(
			[NotNull] IIssueWriter issueWriter,
			[NotNull] IDictionary<ITest, QualitySpecificationElement> elementsByTest,
			[CanBeNull] IGeometry testPerimeter,
			[CanBeNull] IExceptionObjectEvaluator exceptionObjectEvaluator)
		{
			Assert.ArgumentNotNull(issueWriter, nameof(issueWriter));
			Assert.ArgumentNotNull(elementsByTest, nameof(elementsByTest));

			_issueWriter = issueWriter;
			_elementsByTest = elementsByTest;
			_testPerimeter = testPerimeter;
			_exceptionObjectEvaluator = exceptionObjectEvaluator;
		}

		public event EventHandler<IssueFoundEventArgs> IssueFound;

		public void Process([NotNull] QaErrorEventArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));

			QaError qaError = args.QaError;

			QualitySpecificationElement element = _elementsByTest[qaError.Test];

			QualityCondition qualityCondition = element.QualityCondition;

			if (element.StopOnError)
			{
				foreach (InvolvedRow involvedRow in qaError.InvolvedRows)
				{
					var stopInfo = new StopInfo(qualityCondition, qaError.Description);
					_rowsWithStopConditions.Add(involvedRow.TableName, involvedRow.OID,
					                            stopInfo);
				}
			}

			if (IsIssueGeometryOutsideTestPerimeter(qaError, qualityCondition))
			{
				args.Cancel = true;
				return;
			}

			IssueStats issueStats = GetIssueStats(qualityCondition);

			if (ExistsExceptionFor(qaError, element))
			{
				issueStats.AddException();
				return;
			}

			issueStats.AddIssue();

			if (element.AllowErrors)
			{
				WarningCount++;
			}
			else
			{
				ErrorCount++;
				Fulfilled = false;
			}

			OnIssueFound(element, qaError, element.AllowErrors);

			_issueWriter.WriteIssue(qaError, element);
		}

		private bool ExistsExceptionFor([NotNull] QaError qaError,
		                                [NotNull] QualitySpecificationElement element)
		{
			if (_exceptionObjectEvaluator == null)
			{
				return false;
			}

			return _exceptionObjectEvaluator.ExistsExceptionFor(qaError, element, out _);
		}

		[NotNull]
		private IssueStats GetIssueStats([NotNull] QualityCondition qualityCondition)
		{
			IssueStats issueStats;
			if (! _issueStatsByCondition.TryGetValue(qualityCondition, out issueStats))
			{
				issueStats = new IssueStats();
				_issueStatsByCondition.Add(qualityCondition, issueStats);
			}

			return issueStats;
		}

		private bool IsIssueGeometryOutsideTestPerimeter(
			[NotNull] QaError qaError,
			[NotNull] QualityCondition qualityCondition)
		{
			if (_testPerimeter == null)
			{
				return false;
			}

			IGeometry errorGeometry = qaError.Geometry;

			if (errorGeometry == null || errorGeometry.IsEmpty)
			{
				return false;
			}

			GeometryUtils.AllowIndexing(errorGeometry);

			try
			{
				return ((IRelationalOperator) _testPerimeter).Disjoint(errorGeometry);
			}
			catch (Exception e)
			{
				_msg.WarnFormat(
					"Error in disjoint check for issue geometry: {0}. " +
					"Assuming that issue is non-disjoint (see log for details)",
					e.Message);

				_msg.DebugFormat("Quality condition: {0}", qualityCondition.Name);
				_msg.DebugFormat("Issue description: {0}", qaError.Description);

				foreach (InvolvedRow row in qaError.InvolvedRows)
				{
					_msg.DebugFormat("Involved row: {0} table: {1}", row.OID, row.TableName);
				}

				_msg.DebugFormat("test perimeter: {0}", GeometryUtils.ToString(_testPerimeter));
				_msg.DebugFormat("error geometry: {0}", GeometryUtils.ToString(errorGeometry));

				return false;
			}
		}

		private void OnIssueFound(
			[NotNull] QualitySpecificationElement qSpecElement,
			[NotNull] QaError qaError,
			bool isAllowable)
		{
			if (IssueFound == null)
			{
				return;
			}

			string involvedObjectsString = null;

			IssueFound.Invoke(
				this, new IssueFoundEventArgs(qSpecElement, qaError, isAllowable,
				                              involvedObjectsString));
		}

		public int GetIssueCount([NotNull] QualityCondition qualityCondition,
		                         out int exceptionCount)
		{
			IssueStats issueStats;
			if (_issueStatsByCondition.TryGetValue(qualityCondition, out issueStats))
			{
				exceptionCount = issueStats.ExceptionCount;
				return issueStats.IssueCount;
			}

			exceptionCount = 0;
			return 0;
		}

		public int ErrorCount { get; private set; }

		public int WarningCount { get; private set; }

		public int RowsWithStopConditionsCount => _rowsWithStopConditions.Count;

		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		public bool Fulfilled { get; private set; }

		public bool HasStopCondition([NotNull] IReadOnlyRow row)
		{
			return _rowsWithStopConditions.GetStopInfo(row) != null;
		}

		[NotNull]
		public IEnumerable<RowWithStopCondition> GetRowsWithStopConditions()
		{
			// TODO ensure that any pending ID lookups are applied
			return _rowsWithStopConditions.GetRowsWithStopConditions();
		}

		private class IssueStats
		{
			public void AddIssue()
			{
				IssueCount++;
			}

			public void AddException()
			{
				ExceptionCount++;
			}

			public int ExceptionCount { get; private set; }

			public int IssueCount { get; private set; }
		}
	}
}
