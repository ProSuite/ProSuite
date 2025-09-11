using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	public class DummyTest : ContainerTest
	{
		public DummyTest()
			: base(new List<IReadOnlyTable>()) { }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return 0;
		}
	}

	public static class QualitySpecificationUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static QualitySpecification CreateQualitySpecification(
			[NotNull] string dataQualityXml,
			[NotNull] string specificationName,
			[NotNull] IList<DataSource> dataSourceReplacements,
			[CanBeNull] FactorySettings factorySettings = null,
			[CanBeNull] ICollection<int> excludededConditionIds = null)
		{
			IList<XmlQualitySpecification> qualitySpecifications;
			XmlDataQualityDocument document;

			using (Stream baseStream = new MemoryStream(Encoding.UTF8.GetBytes(dataQualityXml)))
			using (StreamReader xmlReader = new StreamReader(baseStream))
			{
				document = XmlDataQualityUtils.ReadXmlDocument(xmlReader,
				                                               out qualitySpecifications);
			}

			_msg.DebugFormat("Available specifications: {0}",
			                 StringUtils.Concatenate(qualitySpecifications.Select(s => s.Name),
			                                         ", "));

			// Why is it not enough to set the respective specification.Elements Enabled property to false?
			if (excludededConditionIds?.Count > 0 &&
			    qualitySpecifications.FirstOrDefault(s => s.Name == specificationName)
				    is XmlQualitySpecification selectedSpec)
			{
				DummifyExcludedConditions(document, selectedSpec,
				                          excludededConditionIds);
			}

			factorySettings = factorySettings ??
			                  new FactorySettings { IgnoreConditionsForUnknownDatasets = true };
			return CreateQualitySpecification(document, specificationName, dataSourceReplacements,
			                                  factorySettings);
		}

		public static void DisableExcludedConditions(QualitySpecification fromSpecification,
		                                             IEnumerable<int> excludedConditionIds)
		{
			HashSet<int> disabledConditionIds = new HashSet<int>(excludedConditionIds);

			foreach (var element in fromSpecification.Elements)
			{
				if (disabledConditionIds.Contains(element.QualityCondition.Id))
				{
					element.Enabled = false;
				}
			}
		}

		private static void DummifyExcludedConditions(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] XmlQualitySpecification qualitySpecification,
			[NotNull] ICollection<int> excludededConditionIds)
		{
			int iQc = 0;
			Dictionary<string, int> qcIdDict = new Dictionary<string, int>();
			foreach (var element in qualitySpecification.Elements)
			{
				if (! qcIdDict.ContainsKey(element.QualityConditionName))
				{
					qcIdDict.Add(element.QualityConditionName, iQc);
					iQc++;
				}
			}

			string dummyDescriptorName = "_dummyTestDescriptor_";
			while (document.TestDescriptors?.Exists(x => x.Name == dummyDescriptorName) == true)
			{
				dummyDescriptorName += "_";
			}

			document.TestDescriptors?.Add(
				new XmlTestDescriptor
				{
					Name = dummyDescriptorName,
					TestClass = new XmlClassDescriptor(typeof(DummyTest), 0)
				});
			if (document.QualityConditions != null)
			{
				foreach (var qc in document.QualityConditions)
				{
					if (qc.Name == null || ! qcIdDict.TryGetValue(qc.Name, out int idQc) ||
					    excludededConditionIds.Contains(idQc))
					{
						qc.ParameterValues.Clear();
						qc.TestDescriptorName = dummyDescriptorName;
					}
				}
			}
		}

		[NotNull]
		public static IList<DataSource> GetDataSources([NotNull] string dataQualityXml)
		{
			XmlDataQualityDocument document;
			using (Stream baseStream = new MemoryStream(Encoding.UTF8.GetBytes(dataQualityXml)))
			using (StreamReader xmlReader = new StreamReader(baseStream))
			{
				document = XmlDataQualityUtils.ReadXmlDocument(xmlReader, out _);
			}

			List<DataSource> dataSources = new List<DataSource>();
			if (document.Workspaces != null)
			{
				foreach (XmlWorkspace xmlWorkspace in document.Workspaces)
				{
					if (! string.IsNullOrWhiteSpace(xmlWorkspace.CatalogPath) ||
					    ! string.IsNullOrWhiteSpace(xmlWorkspace.ConnectionString))
					{
						DataSource ds = new DataSource(xmlWorkspace);
						dataSources.Add(ds);
					}
				}
			}

			return dataSources;
		}

		/// <summary>
		/// Initializes all persistent entities that are part of the specified quality
		/// specification are loaded and initialized. The entities are quality conditions,
		/// their issue filters, transformers, row filters and their respective
		/// TestParameterValues and finally all the referenced datasets.
		/// </summary>
		/// <param name="specification"></param>
		/// <param name="domainTransactions"></param>
		/// <param name="instanceConfigurations"></param>
		/// <returns>All datasets that are involved in any associated entity of the
		/// conditions in the specification.</returns>
		public static ICollection<Dataset> InitializeAssociatedEntitiesTx(
			[NotNull] QualitySpecification specification,
			[NotNull] IDomainTransactionManager domainTransactions,
			[CanBeNull] IInstanceConfigurationRepository instanceConfigurations = null)
		{
			var enabledConditions =
				specification.Elements.Where(e => e.Enabled)
				             .Select(e => e.QualityCondition)
				             .ToList();

			return InstanceConfigurationUtils.InitializeAssociatedConfigurationsTx(
				enabledConditions, domainTransactions, instanceConfigurations);
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
				IList<string> deleted = condition.GetDeletedParameterValues();
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
					if (EnvironmentUtils.GetBooleanEnvironmentVariableValue("PROSUITE_EXPECT_ALL_DATASETS_EXIST"))
					{
						throw new InvalidOperationException("Missing datasets or unable to open: " +
							StringUtils.Concatenate(missingDatasets, d => d.Name, ", "));
					}

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

			int elementsCount = specification.Elements.Count;

			int disabledCount = elementsCount -
			                    specification.Elements.Count(e => e.Enabled);

			if (disabledCount > 0)
			{
				sb.AppendFormat("{0} conditions of {1} have been disabled.", disabledCount,
				                elementsCount);

				sb.AppendLine();
			}

			sb.AppendLine("Enabled conditions:");

			foreach (QualitySpecificationElement element in specification.Elements)
			{
				if (! element.Enabled)
				{
					continue;
				}

				sb.AppendFormat(
					"QualityCondition {0}: StopOnError {1}, AllowErrors {2}, TestDescriptor {3}",
					element.QualityCondition.Name, element.StopOnError,
					element.AllowErrors,
					element.QualityCondition.TestDescriptor.Name);
				sb.AppendLine();
			}

			_msg.Debug(sb.ToString());
		}

		public static void LogQualityVerification(
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

			foreach (var dataset in condition.GetDatasetParameterValues(true))
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
				if (EnvironmentUtils.GetBooleanEnvironmentVariableValue("PROSUITE_EXPECT_ALL_DATASETS_EXIST"))
				{
					throw;
				}

				_msg.Debug($"Error opening (probably missing) dataset {dataset.Name}", e);
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
			[NotNull] IEnumerable<string> deletedTestParameterValueMessages)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("Quality condition '{0}' has deleted values and is ignored.",
			                qualityCondition.Name);
			sb.AppendLine();
			foreach (string message in deletedTestParameterValueMessages)
			{
				sb.AppendFormat("- {0}", message);
			}

			_msg.Warn(sb.ToString());
		}

		private static QualitySpecification CreateQualitySpecification(
			[NotNull] XmlDataQualityDocument document,
			[NotNull] string specificationName,
			[NotNull] IEnumerable<DataSource> dataSources,
			[NotNull] FactorySettings factorySettings)
		{
			QualitySpecification qualitySpecification;
			using (_msg.IncrementIndentation("Setting up quality specification"))
			{
				XmlBasedQualitySpecificationFactory factory = CreateSpecificationFactory();

				qualitySpecification = factory.CreateQualitySpecification(
					document, specificationName, dataSources, factorySettings);
			}

			return qualitySpecification;
		}

		private static XmlBasedQualitySpecificationFactory CreateSpecificationFactory()
		{
			var modelFactory = new VerifiedModelFactory(
				new MasterDatabaseWorkspaceContextFactory(), new SimpleVerifiedDatasetHarvester());

			var factory =
				new XmlBasedQualitySpecificationFactory(modelFactory);
			return factory;
		}
	}
}
