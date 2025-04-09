using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.VerificationReports
{
	public class LoggingVerificationReportBuilder : IVerificationReportBuilder
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly List<Issue> _errors = new List<Issue>();
		private int _rowsWithStopConditions;

		#region Implementation of IVerificationReportBuilder

		void IVerificationReportBuilder.BeginVerification(AreaOfInterest areaOfInterest) { }

		void IVerificationReportBuilder.AddVerifiedDataset(
			QualityVerificationDataset verificationDataset, string workspaceDisplayText,
			ISpatialReference spatialReference) { }

		void IVerificationReportBuilder.AddVerifiedQualityCondition(
			QualitySpecificationElement qualitySpecificationElement) { }

		void IVerificationReportBuilder.AddIssue(Issue issue, IGeometry errorGeometry)
		{
			_errors.Add(issue);

			_msg.WarnFormat(issue.Allowable
				                ? "Error ({0}, soft): {1}"
				                : "Error ({0}, hard): {1}",
			                issue.QualityCondition.Name, issue);
		}

		void IVerificationReportBuilder.AddRowsWithStopConditions(
			IEnumerable<RowWithStopCondition> rowsWithStopCondition)
		{
			using (IEnumerator<RowWithStopCondition> enumerator =
			       rowsWithStopCondition.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					_rowsWithStopConditions++;
				}
			}
		}

		public void AddExceptionStatistics(IExceptionStatistics statistics) { }

		void IVerificationReportBuilder.EndVerification(bool cancelled)
		{
			if (cancelled)
			{
				_msg.Warn("The verification was cancelled");
			}

			if (_errors.Count == 0)
			{
				_msg.Info("No errors found");
			}
			else
			{
				_msg.WarnFormat(_errors.Count == 1
					                ? "{0:N0} error found:"
					                : "{0:N0} errors found:", _errors.Count);

				if (_rowsWithStopConditions > 0)
				{
					_msg.WarnFormat(
						"Number of rows with stop conditions (not completely tested): {0:N0}",
						_rowsWithStopConditions);
				}
			}

			ReportErrors(_errors);
		}

		#endregion

		/// <summary>
		/// Reports the errors by writing them to the log
		/// </summary>
		/// <param name="errors">The errors.</param>
		private static void ReportErrors([NotNull] IEnumerable<Issue> errors)
		{
			foreach (
				KeyValuePair<QualityCondition, List<Issue>> pair in
				GetErrorsByQualityCondition(errors))
			{
				QualityCondition qualityCondition = pair.Key;
				List<Issue> errorsForTest = pair.Value;

				_msg.WarnFormat(errorsForTest.Count == 1
					                ? "{0}: {1:N0} error"
					                : "{0}: {1:N0} errors", qualityCondition.Name,
				                errorsForTest.Count);
			}
		}

		/// <summary>
		/// Gets the errors by test.
		/// </summary>
		/// <param name="errors">The errors.</param>
		/// <returns></returns>
		[NotNull]
		private static Dictionary<QualityCondition, List<Issue>> GetErrorsByQualityCondition
		(
			[NotNull] IEnumerable<Issue> errors)
		{
			var result = new Dictionary<QualityCondition, List<Issue>>();

			foreach (Issue error in errors)
			{
				List<Issue> list;
				if (! result.TryGetValue(error.QualityCondition, out list))
				{
					list = new List<Issue>();
					result.Add(error.QualityCondition, list);
				}

				list.Add(error);
			}

			return result;
		}
	}
}
