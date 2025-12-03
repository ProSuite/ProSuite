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
		// - or handle the the DDX-related stuff in the caller, which should probably always be
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
			if (_domainTransactions == null)
			{
				return null;
			}

			if (_qualityVerification == null)
			{
				_domainTransactions.UseTransaction(
					() =>
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

		public string HtmlReportPath { get; set; }

		public string IssuesGdbPath { get; set; }

		private QualityVerification GetQualityVerificationTx([NotNull] QualityVerificationMsg msg)
		{
			List<QualityConditionVerification> conditionVerifications =
				GetQualityConditionVerifications(msg);

			var result = new QualityVerification(
				msg.SpecificationId, msg.SpecificationName, msg.SpecificationDescription,
				msg.UserName, conditionVerifications);

			result.Cancelled = msg.Cancelled;
			result.ContextName = msg.ContextName;
			result.ContextType = msg.ContextType;
			result.StartDate = new DateTime(msg.StartTimeTicks);
			result.EndDate = new DateTime(msg.EndTimeTicks);

			result.ProcessorTimeSeconds = msg.ProcessorTimeSeconds;
			result.RowsWithStopConditions = msg.RowsWithStopConditions;

			result.CalculateStatistics();

			return result;
		}

		private List<QualityConditionVerification> GetQualityConditionVerifications(
			[NotNull] QualityVerificationMsg msg)
		{
			List<QualityConditionVerification> conditionVerifications =
				new List<QualityConditionVerification>();

			Dictionary<int, QualityCondition> conditionsById =
				new Dictionary<int, QualityCondition>();

			foreach (var conditionVerificationMsg in msg.ConditionVerifications)
			{
				int qualityConditionId = conditionVerificationMsg.QualityConditionId;

				QualityCondition qualityCondition = GetQualityCondition(
					qualityConditionId, conditionsById);

				Assert.NotNull(qualityCondition, $"Condition {qualityConditionId} not found");

				// TODO: AllowError/StopOnError
				QualitySpecificationElement element =
					new QualitySpecificationElement(qualityCondition);

				var conditionVerification = new QualityConditionVerification(element);

				bool fullFilled = conditionVerificationMsg.Fulfilled;
				conditionVerification.Fulfilled = fullFilled;

				if (! fullFilled)
				{
					_msg.Warn($"Condition {qualityConditionId} is not fulfilled");
				}
				else
				{
					_msg.Debug($"Condition {qualityConditionId} is fulfilled");
				}

				conditionVerification.ErrorCount = conditionVerificationMsg.ErrorCount;

				conditionVerification.ExecuteTime = conditionVerificationMsg.ExecuteTime;
				conditionVerification.RowExecuteTime = conditionVerificationMsg.RowExecuteTime;
				conditionVerification.TileExecuteTime = conditionVerificationMsg.TileExecuteTime;

				if (conditionVerificationMsg.StopConditionId >= 0)
				{
					conditionVerification.StopCondition = GetQualityCondition(
						conditionVerificationMsg.StopConditionId, conditionsById);
				}

				conditionVerifications.Add(conditionVerification);
			}

			return conditionVerifications;
		}

		private static IEnumerable<int> GetVerifiedConditionIds(
			[NotNull] QualityVerificationMsg msg)
		{
			foreach (var conditionVerificationMsg in msg.ConditionVerifications)
			{
				yield return conditionVerificationMsg.QualityConditionId;
			}
		}

		private QualityCondition GetQualityCondition(int qualityConditionId,
		                                             IDictionary<int, QualityCondition>
			                                             conditionsById)
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
