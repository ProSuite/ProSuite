using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	public static class QualityVerificationUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Gets all verified datasets for the verification context, including base datasets.
		/// </summary>
		/// <param name="verificationContext">The verification context.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<Dataset> GetAllVerifiedDatasets(
			[NotNull] IVerificationContext verificationContext)
		{
			ICollection<Dataset> result = verificationContext.GetVerifiedDatasets();

			return result;
		}

		/// <summary>
		/// Gets all models with datasets to be verified by the specified quality specification.
		/// </summary>
		/// <param name="qualitySpecification">The quality specification.</param>
		/// <param name="modelPredicate">An optional predicate for relevant models.</param>
		/// <returns></returns>
		[NotNull]
		public static ICollection<DdxModel> GetVerifiedModels(
			[NotNull] QualitySpecification qualitySpecification,
			[CanBeNull] Predicate<DdxModel> modelPredicate = null)
		{
			return GetVerifiedModels(qualitySpecification, out _, modelPredicate);
		}

		/// <summary>
		/// Gets all models with datasets to be verified by the specified quality specification.
		/// </summary>
		/// <param name="qualitySpecification">The quality specification.</param>
		/// <param name="involvedDatasets">The datasets involved in the quality conditions</param>
		/// <param name="modelPredicate">An optional predicate for relevant models.</param>
		/// <returns></returns>
		[NotNull]
		public static ICollection<DdxModel> GetVerifiedModels(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] out HashSet<Dataset> involvedDatasets,
			[CanBeNull] Predicate<DdxModel> modelPredicate = null)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			var result = new HashSet<DdxModel>();
			involvedDatasets = new HashSet<Dataset>();

			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				if (! element.Enabled)
				{
					continue;
				}

				foreach (Dataset dataset in element.QualityCondition.GetDatasetParameterValues())
				{
					if (dataset.Deleted)
					{
						continue;
					}

					DdxModel model = dataset.Model;
					if (model == null)
					{
						continue;
					}

					if (modelPredicate == null || modelPredicate(model))
					{
						result.Add(model);
						involvedDatasets.Add(dataset);
					}
				}
			}

			return result;
		}

		public static IList<ITest> GetTestsAndDictionaries(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IOpenDataset datasetOpener,
			[NotNull] out QualityVerification qualityVerification,
			[NotNull] out IList<QualityCondition> qualityConditions,
			[NotNull] out VerificationElements verificationDictionaries,
			[CanBeNull] Action<string, int, int> reportPreProcessing)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			reportPreProcessing?.Invoke("Loading tests...", 0, 0);

			var testList = new List<ITest>();

			HashSet<QualityCondition> orderedQualityConditions =
				new HashSet<QualityCondition>(
					QualitySpecificationUtils.GetOrderedQualityConditions(
						qualitySpecification, datasetOpener));

			Dictionary<QualityConditionVerification, QualitySpecificationElement>
				elementsByConditionVerification;

			qualityVerification = GetQualityVerification(
				qualitySpecification, orderedQualityConditions,
				out elementsByConditionVerification);

			qualityConditions = new List<QualityCondition>();
			var testsByCondition = new Dictionary<QualityCondition, IList<ITest>>();
			var testVerifications = new Dictionary<ITest, TestVerification>();

			if (orderedQualityConditions.Count == 0)
			{
				verificationDictionaries = new VerificationElements(
					testVerifications, testsByCondition, elementsByConditionVerification);
				return testList;
			}

			int index = 0;
			int count = orderedQualityConditions.Count;
			foreach (QualityCondition condition in orderedQualityConditions)
			{
				reportPreProcessing?.Invoke("Loading tests...", index++, count);

				TestFactory factory =
					Assert.NotNull(TestFactoryUtils.CreateTestFactory(condition),
					               $"Cannot create test factory for condition {condition.Name}");

				// This test can only be performed here because the DataType must be initialized:
				// It should probably be deleted once no IMosaicLayer, ITerrain is used any more
				if (QualitySpecificationUtils.HasUnsupportedDatasetParameterValues(
					    condition, datasetOpener, out string message))
				{
					_msg.WarnFormat(
						"Condition '{0}' has unsupported parameter value(s) and is ignored: {1}",
						condition.Name, message);
					continue;
				}

				IList<ITest> tests = factory.CreateTests(datasetOpener);
				if (tests.Count == 0)
				{
					// TODO: Warn, consider not adding this condition to elementsByConditionVerification
					//       in order to avoid downstream failure.
					continue;
				}

				QualityConditionVerification conditionVerification =
					qualityVerification.GetConditionVerification(condition);
				Assert.NotNull(conditionVerification,
				               "Verification not found for quality condition");

				var testIndex = 0;
				foreach (ITest test in tests)
				{
					IList<IReadOnlyTable> involvedTables = test.InvolvedTables;

					_msg.VerboseDebug(
						() =>
							$"Adding test {test}. Tables: {StringUtils.Concatenate(involvedTables, t => t.Name, ", ")}. Hashcode: {test.GetHashCode()}");

					testList.Add(test);
					testVerifications.Add(test,
					                      new TestVerification(conditionVerification, testIndex));
					testIndex++;
				}

				qualityConditions.Add(condition);
				testsByCondition.Add(condition, tests);
			}

			verificationDictionaries =
				new VerificationElements(testVerifications, testsByCondition,
				                         elementsByConditionVerification);

			return testList;
		}

		private static QualityVerification GetQualityVerification(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] HashSet<QualityCondition> conditionsToVerify,
			[NotNull] out Dictionary<QualityConditionVerification, QualitySpecificationElement>
				elementsByConditionVerification)
		{
			var result = new QualityVerification(qualitySpecification, conditionsToVerify);

			Dictionary<QualityCondition, QualityConditionVerification> verificationsByCondition
				= GetConditionVerificationsByCondition(result);

			elementsByConditionVerification = new Dictionary
				<QualityConditionVerification, QualitySpecificationElement>(
					result.ConditionVerifications.Count);

			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				QualityConditionVerification verification;
				if (verificationsByCondition.TryGetValue(element.QualityCondition,
				                                         out verification))
				{
					elementsByConditionVerification.Add(verification, element);
				}
			}

			return result;
		}

		[NotNull]
		private static Dictionary<QualityCondition, QualityConditionVerification>
			GetConditionVerificationsByCondition(
				[NotNull] QualityVerification qualityVerification)
		{
			return qualityVerification.ConditionVerifications
			                          .Where(v => v.QualityCondition != null)
			                          .ToDictionary(v => v.QualityCondition);
		}

		[NotNull]
		public static IDictionary<IFeatureClass, IVectorDataset> GetDatasetsByFeatureClass(
			[NotNull] IEnumerable<IFeature> features,
			[NotNull] IDatasetLookup datasetLookup)
		{
			var result = new Dictionary<IFeatureClass, IVectorDataset>();

			foreach (IFeature feature in features)
			{
				var featureClass = (IFeatureClass) feature.Class;

				IVectorDataset vectorDataset;
				if (! result.TryGetValue(featureClass, out vectorDataset))
				{
					vectorDataset = datasetLookup.GetDataset(featureClass);
					result.Add(featureClass, vectorDataset);
				}
			}

			return result;
		}

		internal static void LogSpecification([NotNull] QualitySpecification specification)
		{
			if (! _msg.IsDebugEnabled)
			{
				return;
			}

			var sb = new StringBuilder();

			sb.AppendFormat("Specification {0}", specification.Name);
			sb.AppendLine();

			foreach (QualitySpecificationElement element in specification.Elements)
			{
				sb.AppendFormat(
					"QualityCondition {0}: StopOnError {1}, AllowErrors {2}, TestDescriptor {3}",
					element.QualityCondition.Name, element.StopOnError,
					element.AllowErrors,
					element.QualityCondition.TestDescriptor.Name);
				sb.AppendLine();
			}

			_msg.Debug(sb.ToString());
		}

		internal static void LogQualityVerification(
			[NotNull] QualityVerification verification)
		{
			try
			{
				var sb = new StringBuilder();

				sb.AppendLine(verification.Cancelled
					              ? "The quality verification was cancelled"
					              : "Quality verified");

				int conditionCount = verification.ConditionVerifications.Count;

				sb.AppendFormat("- {0:N0} quality condition{1} verified",
				                conditionCount, conditionCount == 1
					                                ? ""
					                                : "s");
				sb.AppendLine();

				int issueCount = verification.IssueCount;
				if (issueCount == 0)
				{
					sb.AppendLine("- No issues found");
				}
				else
				{
					sb.AppendFormat(issueCount == 1
						                ? "- {0:N0} issue found"
						                : "- {0:N0} issues found",
					                issueCount);
					sb.AppendLine();

					sb.AppendFormat("  - Errors: {0:N0}", verification.ErrorCount);
					sb.AppendLine();

					sb.AppendFormat("  - Warnings: {0:N0}", verification.WarningCount);
					sb.AppendLine();

					if (verification.RowsWithStopConditions > 0)
					{
						sb.AppendFormat("  - Number of rows with a stop condition error: {0:N0}",
						                verification.RowsWithStopConditions);
						sb.AppendLine();
					}
				}

				if (! verification.Cancelled)
				{
					sb.AppendLine(verification.Fulfilled
						              ? "- The quality specification is fulfilled"
						              : "- The quality specification is not fulfilled");
				}

				if (verification.Fulfilled)
				{
					_msg.InfoFormat(sb.ToString());
				}
				else
				{
					_msg.WarnFormat(sb.ToString());
				}

				LogVerificationDetails(verification);
			}
			catch (Exception e)
			{
				_msg.Warn("Error writing report to log", e);
				// continue
			}
		}

		private static void LogVerificationDetails(
			[NotNull] QualityVerification verification)
		{
			_msg.Debug("Verified quality conditions:");
			using (_msg.IncrementIndentation())
			{
				List<QualityConditionVerification> sortedList =
					verification.ConditionVerifications.ToList();

				sortedList.Sort((v1, v2) =>
					                string.Compare(v1.QualityCondition == null
						                               ? "<null>"
						                               : v1.QualityCondition.Name,
					                               v2.QualityCondition == null
						                               ? "<null>"
						                               : v2.QualityCondition.Name,
					                               StringComparison.CurrentCultureIgnoreCase));

				foreach (QualityConditionVerification conditionVerification in sortedList)
				{
					LogConditionVerification(conditionVerification);
				}
			}

			_msg.Debug("Load times for verified datasets:");
			using (_msg.IncrementIndentation())
			{
				List<QualityVerificationDataset> sortedList =
					verification.VerificationDatasets.ToList();

				sortedList.Sort((d1, d2) =>
					                string.Compare(d1.Dataset.Name,
					                               d2.Dataset.Name,
					                               StringComparison.CurrentCultureIgnoreCase));

				foreach (QualityVerificationDataset verifiedDataset in sortedList)
				{
					LogVerificationDataset(verifiedDataset);
				}
			}
		}

		private static void LogVerificationDataset(
			[NotNull] QualityVerificationDataset verifiedDataset)
		{
			_msg.DebugFormat("{0}: {1:N2} ms",
			                 verifiedDataset.Dataset.Name,
			                 verifiedDataset.LoadTime * 1000);
		}

		private static void LogConditionVerification(
			[NotNull] QualityConditionVerification conditionVerification)
		{
			_msg.DebugFormat("{0}: {1:N0} {2} ({3:N2} ms)",
			                 conditionVerification.QualityCondition == null
				                 ? "<null>"
				                 : conditionVerification.QualityCondition.Name,
			                 conditionVerification.ErrorCount,
			                 conditionVerification.AllowErrors
				                 ? "warning(s)"
				                 : "error(s)",
			                 conditionVerification.TotalExecuteTime * 1000);
		}
	}
}
