using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	public class BackgroundVerificationResult : IQualityVerificationResult
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private readonly IClientIssueMessageCollector _resultIssueCollector;
		[CanBeNull] private readonly IDomainTransactionManager _domainTransactions;
		private readonly IQualityVerificationRepository _qualityVerificationRepository;
		private readonly IQualityConditionRepository _qualityConditionRepository;

		private QualityVerification _qualityVerification;

		public QualityVerificationMsg VerificationMsg { get; set; }

		// TODO: Remove DDX-specific stuff and
		// - either provide the necessary repositories etc. as method parameters where needed
		// - or handle the DDX-related stuff in the caller, which should probably always be
		//   the IApplicationBackgroundVerificationController implementation.
		// Alternatively create a separate implementation for the interface.
		public BackgroundVerificationResult(
			[CanBeNull] IClientIssueMessageCollector resultIssueCollector,
			[CanBeNull] IDomainTransactionManager domainTransactions,
			[CanBeNull] IQualityVerificationRepository qualityVerificationRepository,
			[CanBeNull] IQualityConditionRepository qualityConditionRepository)
		{
			_resultIssueCollector = resultIssueCollector;

			_domainTransactions = domainTransactions;
			_qualityVerificationRepository = qualityVerificationRepository;
			_qualityConditionRepository = qualityConditionRepository;
		}

		public bool HasIssues => _resultIssueCollector?.HasIssues ?? false;

		public bool IsFulfilled => Assert.NotNull(VerificationMsg).Fulfilled;

		public int RowCountWithStopConditions =>
			Assert.NotNull(VerificationMsg).RowsWithStopConditions;

		public int VerifiedConditionCount =>
			Assert.NotNull(VerificationMsg).ConditionVerifications?.Count ?? 0;

		public bool CanSaveIssues => _resultIssueCollector != null && VerificationMsg != null;

		public int IssuesSaved { get; private set; } = -1;

		public int SaveIssues(ErrorDeletionInPerimeter errorDeletion)
		{
			Assert.NotNull(_resultIssueCollector).ErrorDeletionInPerimeter = errorDeletion;

			Stopwatch watch = _msg.DebugStartTiming("Replacing existing errors with new issues...");

			var verifiedConditions = GetVerifiedConditionIds(VerificationMsg).ToList();
			int issueCount = _resultIssueCollector.SaveIssues(verifiedConditions);

			_msg.DebugStopTiming(watch, "Updated issues in verified context");

			return issueCount;
		}

		public async Task<int> SaveIssuesAsync(
			ErrorDeletionInPerimeter errorDeletion =
				ErrorDeletionInPerimeter.VerifiedQualityConditions)
		{
			Assert.NotNull(_resultIssueCollector).ErrorDeletionInPerimeter = errorDeletion;

			Stopwatch watch = _msg.DebugStartTiming("Replacing existing errors with new issues...");

			var verifiedConditions = GetVerifiedConditionIds(VerificationMsg).ToList();
			int issueCount = await _resultIssueCollector.SaveIssuesAsync(verifiedConditions);

			_msg.DebugStopTiming(watch, "Updated issues in verified context");

			IssuesSaved = issueCount;

			return issueCount;
		}

		public bool HasQualityVerification()
		{
			return VerificationMsg != null && _domainTransactions != null;
		}

		public QualityVerification GetQualityVerification()
		{
			// TODO: Load the conditions-dictionary up front and provide as parameter or use
			// separate implementations if no direct DDX access is available.
			if (_domainTransactions == null || VerificationMsg == null)
			{
				return null;
			}

			if (_qualityVerification == null)
			{
				_domainTransactions.UseTransaction(() =>
				{
					if (VerificationMsg.SavedVerificationId >= 0)
					{
						_msg.DebugFormat("Getting verification details from DDX (<id> {0}).",
						                 VerificationMsg.SavedVerificationId);
						_qualityVerification =
							_qualityVerificationRepository.Get(
								VerificationMsg.SavedVerificationId);

						Assert.NotNull(_qualityVerification, "Quality verification not found.");

						_domainTransactions.Initialize(
							_qualityVerification.ConditionVerifications);
						_domainTransactions.Initialize(
							_qualityVerification.VerificationDatasets);
					}
					else
					{
						_msg.DebugFormat(
							"Using verification details provided from QA service.");
						_qualityVerification = GetQualityVerificationTx(VerificationMsg);
					}
				});
			}

			return _qualityVerification;
		}

		/// <summary>
		/// Builds a <see cref="QualityVerification"/> from this result's verification message
		/// combined with the already-loaded quality specification, so the form can display full
		/// condition details (including AllowErrors / StopOnError) without going back to the server.
		/// </summary>
		public QualityVerification GetQualityVerification(
			[NotNull] QualitySpecification spec)
		{
			var msg = Assert.NotNull(VerificationMsg);

			var elementById = spec.Elements
			                      .Where(e => e.Enabled)
			                      .ToDictionary(e => e.QualityCondition.Id);

			var conditionVerifications = new List<QualityConditionVerification>();

			foreach (QualityConditionVerificationMsg cvMsg in msg.ConditionVerifications)
			{
				if (! elementById.TryGetValue(cvMsg.QualityConditionId, out var element))
				{
					continue;
				}

				var conditionVerification = new QualityConditionVerification(element);
				ApplyVerificationStats(conditionVerification, cvMsg);

				if (cvMsg.StopConditionId >= 0 &&
				    elementById.TryGetValue(cvMsg.StopConditionId, out var stopElement))
				{
					conditionVerification.StopCondition = stopElement.QualityCondition;
				}

				conditionVerifications.Add(conditionVerification);
			}

			return BuildVerificationResult(msg, conditionVerifications);
		}

		public string HtmlReportPath { get; set; }

		public string IssuesGdbPath { get; set; }

		private QualityVerification GetQualityVerificationTx([NotNull] QualityVerificationMsg msg)
		{
			var conditionVerifications = new List<QualityConditionVerification>();
			var conditionsById = new Dictionary<int, QualityCondition>();

			foreach (var cvMsg in msg.ConditionVerifications)
			{
				int qualityConditionId = cvMsg.QualityConditionId;

				QualityCondition qualityCondition = GetQualityCondition(qualityConditionId,
					conditionsById);

				Assert.NotNull(qualityCondition, $"Condition {qualityConditionId} not found");

				// TODO: AllowErrors/StopOnError not available without spec in this DDX path
				var element = new QualitySpecificationElement(qualityCondition);
				var conditionVerification = new QualityConditionVerification(element);

				ApplyVerificationStats(conditionVerification, cvMsg);

				if (! conditionVerification.Fulfilled)
				{
					_msg.Warn($"Condition {qualityConditionId} is not fulfilled");
				}
				else
				{
					_msg.Debug($"Condition {qualityConditionId} is fulfilled");
				}

				if (cvMsg.StopConditionId >= 0)
				{
					conditionVerification.StopCondition =
						GetQualityCondition(cvMsg.StopConditionId, conditionsById);
				}

				conditionVerifications.Add(conditionVerification);
			}

			return BuildVerificationResult(msg, conditionVerifications);
		}

		private static void ApplyVerificationStats(
			[NotNull] QualityConditionVerification conditionVerification,
			[NotNull] QualityConditionVerificationMsg msg)
		{
			conditionVerification.Fulfilled = msg.Fulfilled;
			conditionVerification.ErrorCount = msg.ErrorCount;
			conditionVerification.ExecuteTime = msg.ExecuteTime;
			conditionVerification.RowExecuteTime = msg.RowExecuteTime;
			conditionVerification.TileExecuteTime = msg.TileExecuteTime;
		}

		private static QualityVerification BuildVerificationResult(
			[NotNull] QualityVerificationMsg msg,
			[NotNull] List<QualityConditionVerification> conditionVerifications)
		{
			var result = new QualityVerification(
				msg.SpecificationId, msg.SpecificationName, msg.SpecificationDescription,
				msg.UserName, conditionVerifications);

			result.Cancelled = msg.Cancelled;
			result.ContextType = msg.ContextType;
			result.ContextName = msg.ContextName;
			result.StartDate = new DateTime(msg.StartTimeTicks);
			result.EndDate = new DateTime(msg.EndTimeTicks);
			result.ProcessorTimeSeconds = msg.ProcessorTimeSeconds;
			result.RowsWithStopConditions = msg.RowsWithStopConditions;

			// The constructor auto-creates QualityVerificationDatasets with LoadTime=0.
			// Patch in actual load times from the proto message.
			var vdatasetById = result.VerificationDatasets.ToDictionary(vd => vd.Dataset.Id);
			foreach (QualityVerificationDatasetMsg vdMsg in msg.VerificationDatasets)
			{
				if (vdatasetById.TryGetValue(vdMsg.DatasetId, out QualityVerificationDataset vd))
				{
					vd.LoadTime = vdMsg.LoadTime;
				}
			}

			result.CalculateStatistics();
			return result;
		}

		private static IEnumerable<int> GetVerifiedConditionIds(
			[NotNull] QualityVerificationMsg msg)
		{
			foreach (var conditionVerificationMsg in msg.ConditionVerifications)
			{
				yield return conditionVerificationMsg.QualityConditionId;
			}
		}

		private QualityCondition GetQualityCondition(
			int qualityConditionId,
			[NotNull] IDictionary<int, QualityCondition> conditionsById)
		{
			Assert.NotNull(_qualityConditionRepository);

			QualityCondition qualityCondition;
			if (! conditionsById.TryGetValue(qualityConditionId, out qualityCondition))
			{
				qualityCondition = _qualityConditionRepository.Get(qualityConditionId);
				conditionsById.Add(qualityConditionId, qualityCondition);
			}

			return qualityCondition;
		}
	}
}
