using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[CanBeNull] private readonly IClientIssueMessageCollector _resultIssueCollector;
		[NotNull] private readonly IDomainTransactionManager _domainTransactions;
		private readonly IQualityVerificationRepository _qualityVerificationRepository;
		private readonly IQualityConditionRepository _qualityConditionRepository;

		private QualityVerification _qualityVerification;

		public QualityVerificationMsg VerificationMsg { get; set; }

		public BackgroundVerificationResult(
			[CanBeNull] IClientIssueMessageCollector resultIssueCollector,
			[NotNull] IDomainTransactionManager domainTransactions,
			[NotNull] IQualityVerificationRepository qualityVerificationRepository,
			[NotNull] IQualityConditionRepository qualityConditionRepository)
		{
			_resultIssueCollector = resultIssueCollector;

			_domainTransactions = domainTransactions;
			_qualityVerificationRepository = qualityVerificationRepository;
			_qualityConditionRepository = qualityConditionRepository;
		}

		public bool HasIssues => _resultIssueCollector?.HasIssues ?? false;

		public bool CanSaveIssues => _resultIssueCollector != null;

		public int SaveIssues(ErrorDeletionInPerimeter errorDeletion)
		{
			Assert.NotNull(_resultIssueCollector).ErrorDeletionInPerimeter = errorDeletion;

			Stopwatch watch = _msg.DebugStartTiming(
				"Replacing existing errors with new issues, deleting obsolete allowed errors...");

			var conditionVerifications =
				Assert.NotNull(GetQualityVerification()).ConditionVerifications;

			IEnumerable<int> verifiedConditions =
				conditionVerifications.Where(c => c.QualityCondition != null)
				                      .Select(c => c.QualityCondition.Id);

			int result = _resultIssueCollector.SaveIssues(verifiedConditions);

			_msg.DebugStopTiming(watch, "Updated issues in verified context");

			return result;
		}

		public bool HasQualityVerification()
		{
			return VerificationMsg != null;
		}

		public QualityVerification GetQualityVerification()
		{
			if (_qualityVerification == null)
			{
				if (VerificationMsg.SavedVerificationId >= 0)
				{
					return _qualityVerificationRepository.Get(VerificationMsg.SavedVerificationId);
				}

				_domainTransactions.UseTransaction(
					() => { _qualityVerification = GetQualityVerificationTx(VerificationMsg); });
			}

			return _qualityVerification;
		}

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

			result.IssueCount = conditionVerifications.Sum(v => v.ErrorCount);

			result.ProcessorTimeSeconds = msg.ProcessorTimeSeconds;
			result.RowsWithStopConditions = msg.RowsWithStopConditions;

			result.CalculateFulfilled();

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

				conditionVerification.Fulfilled = conditionVerificationMsg.Fulfilled;
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