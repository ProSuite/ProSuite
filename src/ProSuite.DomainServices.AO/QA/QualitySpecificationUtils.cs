using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA
{
	public static class QualitySpecificationUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes all persistent entities that are part of the specified quality
		/// specification are loaded and initialized. The entities are quality conditions,
		/// their issue filters, transformers, row filters and their respective
		/// TestParameterValues and finally all the referenced datasets.
		/// </summary>
		/// <param name="specification"></param>
		/// <param name="domainTransactions"></param>
		/// <returns>All datasets that are involved in any associated entity of the
		/// conditions in the specification.</returns>
		public static ICollection<Dataset> InitializeAssociatedEntitiesTx(
			[NotNull] QualitySpecification specification,
			[NotNull] IDomainTransactionManager domainTransactions)
		{
			ICollection<Dataset> datasets =
				GetQualityConditionDatasets(
					specification, out ICollection<QualityCondition> persistentConditions);

			domainTransactions.Reattach(persistentConditions);

			// Re-attach conditions because otherwise a LazyLoadException occurs when getting
			// the issue filters
			foreach (QualityCondition condition in persistentConditions)
			{
				InitializeIssueFilters(condition, datasets);
			}

			domainTransactions.Reattach(datasets);

			return datasets;
		}

		/// <summary>
		/// Gets all datasets that are involved in any of the conditions of the provided specification,
		/// including datasets that are part of a transformer or filter used in a condition.
		/// </summary>
		/// <param name="qualitySpecification"></param>
		/// <param name="conditions"></param>
		/// <returns></returns>
		public static ICollection<Dataset> GetQualityConditionDatasets(
			QualitySpecification qualitySpecification,
			out ICollection<QualityCondition> conditions)
		{
			conditions = new List<QualityCondition>();
			var datasets = new HashSet<Dataset>();
			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				if (! element.Enabled)
				{
					continue;
				}

				QualityCondition condition = element.QualityCondition;

				const bool includeRecursively = true;
				foreach (Dataset dataset in condition.GetDatasetParameterValues(
					         includeRecursively, includeRecursively))
				{
					datasets.Add(dataset);
				}

				if (condition.IsPersistent)
				{
					// Do not re-attach un-persisted (e.g. customized) conditions.
					conditions.Add(condition);
				}
			}

			return datasets;
		}

		private static void InitializeIssueFilters([NotNull] QualityCondition condition,
		                                           [NotNull] ICollection<Dataset> allDatasets)
		{
			// Initialize Issue filters:
			foreach (var issueFilterConfig in condition.IssueFilterConfigurations)
			{
				foreach (Dataset dataset in issueFilterConfig.GetDatasetParameterValues(true))
				{
					allDatasets.Add(dataset);
				}
			}
		}

		[NotNull]
		internal static IEnumerable<QualityCondition> GetOrderedQualityConditions(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IOpenDataset datasetOpener)
		{
			var list = new List<OrderedQualitySpecificationElement>();

			var knownMissingDatasets = new HashSet<Dataset>();
			var knownExistingDatasets = new HashSet<Dataset>();

			var listOrder = 0;
			foreach (QualitySpecificationElement element in qualitySpecification.Elements)
			{
				if (! element.Enabled)
				{
					continue;
				}

				QualityCondition condition = element.QualityCondition;
				IList<TestParameterValue> deleted = condition.GetDeletedParameterValues();
				if (deleted.Count > 0)
				{
					ReportInvalidConditionWarning(condition, deleted);
					continue;
				}

				// try to open the datasets
				ICollection<Dataset> missingDatasets = GetMissingDatasets(
					condition, datasetOpener,
					knownMissingDatasets, knownExistingDatasets);

				if (missingDatasets.Count > 0)
				{
					ReportConditionWithMissingDatasetsWarning(condition, missingDatasets);
					continue;
				}

				list.Add(new OrderedQualitySpecificationElement(element, listOrder));
				listOrder++;
			}

			list.Sort();

			return list.Select(ordered =>
				                   ordered.QualitySpecificationElement.QualityCondition);
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

		[NotNull]
		private static ICollection<Dataset> GetMissingDatasets(
			[NotNull] QualityCondition condition,
			[NotNull] IOpenDataset datasetOpener,
			[NotNull] ICollection<Dataset> knownMissingDatasets,
			[NotNull] ICollection<Dataset> knownExistingDatasets)
		{
			var result = new List<Dataset>();

			foreach (var dataset in condition.GetDatasetParameterValues())
			{
				if (knownExistingDatasets.Contains(dataset))
				{
					continue;
				}

				if (knownMissingDatasets.Contains(dataset))
				{
					result.Add(dataset);
					continue;
				}

				if (ExistsDataset(dataset, datasetOpener))
				{
					knownExistingDatasets.Add(dataset);
				}
				else
				{
					knownMissingDatasets.Add(dataset);
					result.Add(dataset);
				}
			}

			return result;
		}

		private static bool ExistsDataset([NotNull] IDdxDataset dataset,
		                                  [NotNull] IOpenDataset datasetOpener)
		{
			// -> allow work context to load dataset from other database ('master')
			try
			{
				var aoDataset = datasetOpener.OpenDataset(dataset);

				return aoDataset != null;
			}
			catch (Exception e)
			{
				_msg.VerboseDebug(() => $"Error opening dataset {dataset.Name}", e);
				return false;
			}
		}

		private static void ReportConditionWithMissingDatasetsWarning(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IEnumerable<Dataset> missingDatasets)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("Quality condition '{0}' has missing datasets and is ignored.",
			                qualityCondition.Name);
			sb.AppendLine();
			foreach (Dataset value in missingDatasets)
			{
				sb.AppendFormat("- {0}", value.Name);
			}

			_msg.Warn(sb.ToString());
		}

		public static bool HasUnsupportedDatasetParameterValues(
			[NotNull] QualityCondition condition,
			IOpenDataset datasetOpener,
			out string message)
		{
			bool result = false;
			var sb = new StringBuilder();

			foreach (TestParameterValue parameterValue in condition.ParameterValues)
			{
				if (parameterValue is DatasetTestParameterValue datasetParameterValue)
				{
					Type dataType = datasetParameterValue.DataType;

					if (dataType == null)
					{
						continue;
					}

					// TODO: ValueSource? Alternative TestParameterValue!

					if (! datasetOpener.IsSupportedType(dataType))
					{
						sb.AppendFormat("Dataset type '{0}' is not supported.",
						                dataType);

						result = true;
					}
				}
			}

			message = result ? sb.ToString() : null;

			return result;
		}

		private static void ReportInvalidConditionWarning(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IEnumerable<TestParameterValue> deletedTestParameterValues)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("Quality condition '{0}' has deleted values and is ignored.",
			                qualityCondition.Name);
			sb.AppendLine();
			foreach (TestParameterValue value in deletedTestParameterValues)
			{
				sb.AppendFormat("- {0}: {1}", value.TestParameterName, value.StringValue);
			}

			_msg.Warn(sb.ToString());
		}
	}
}
