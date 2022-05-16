using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA
{
	public static class QualityVerificationUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static void IncludeBaseDatasets([NotNull] ICollection<Dataset> datasets,
		                                       [NotNull] IModelContext datasetContext)
		{
			Assert.ArgumentNotNull(datasets, nameof(datasets));
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));

			var existingDatasets = new HashSet<Dataset>(datasets);

			foreach (Dataset baseDataset in GetBaseDatasets(datasets, datasetContext))
			{
				if (! existingDatasets.Contains(baseDataset))
				{
					existingDatasets.Add(baseDataset);
					datasets.Add(baseDataset);
				}
			}
		}

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

			IncludeBaseDatasets(result, verificationContext);

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

		[NotNull]
		private static IEnumerable<Dataset> GetBaseDatasets(
			[NotNull] IEnumerable<Dataset> datasets,
			[NotNull] IWorkspaceContextLookup workspaceContextLookup)
		{
			var result = new List<Dataset>();

			foreach (Dataset dataset in datasets)
			{
				// Currently this is only relevant for terrains. Theoretically linear networks
				// could also be datasets implementing IDatasetCollection.

				if (! (dataset is IDatasetCollection terrainDataset))
				{
					continue;
				}

				IEnumerable<IDdxDataset> baseDatasets = terrainDataset.ContainedDatasets;

				result.AddRange(baseDatasets.Cast<Dataset>());
			}

			return result;
		}
	}
}
