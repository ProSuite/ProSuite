using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlDataQualityDocumentCache
	{
		private readonly XmlDataQualityDocument _document;

		private readonly List<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>
			_qualityConditions;

		private Dictionary<string, XmlIssueFilterConfiguration> _issueFilters;
		private Dictionary<string, XmlTransformerConfiguration> _transformers;

		private Dictionary<string, XmlTestDescriptor> _testDescriptors;
		private Dictionary<string, XmlIssueFilterDescriptor> _issueFilterDescriptors;
		private Dictionary<string, XmlTransformerDescriptor> _transformerDescriptors;

		[CanBeNull] private Dictionary<XmlIssueFilterConfiguration, IssueFilterConfiguration>
			_issueFilterInstances;

		[CanBeNull] private Dictionary<XmlTransformerConfiguration, TransformerConfiguration>
			_transformerInstances;

		[CanBeNull] private Dictionary<XmlTestDescriptor, TestDescriptor> _testDescriptorInstances;

		[NotNull]
		private Dictionary<string, XmlIssueFilterConfiguration> IssueFilters => _issueFilters ??
			(_issueFilters = _document.IssueFilters?.ToDictionary(x => x.Name) ??
			                 new Dictionary<string, XmlIssueFilterConfiguration>());

		[NotNull]
		private Dictionary<string, XmlTransformerConfiguration> Transformers => _transformers ??
			(_transformers = _document.Transformers?.ToDictionary(x => x.Name) ??
			                 new Dictionary<string, XmlTransformerConfiguration>());

		[NotNull]
		private Dictionary<string, XmlTestDescriptor> TestDescriptors => _testDescriptors ??
			(_testDescriptors = _document.TestDescriptors?.ToDictionary(x => x.Name) ??
			                    new Dictionary<string, XmlTestDescriptor>());

		[NotNull]
		private Dictionary<string, XmlIssueFilterDescriptor> IssueFilterDescriptors =>
			_issueFilterDescriptors ??
			(_issueFilterDescriptors =
				 _document.IssueFilterDescriptors?.ToDictionary(x => x.Name) ??
				 new Dictionary<string, XmlIssueFilterDescriptor>());

		[NotNull]
		private Dictionary<string, XmlTransformerDescriptor> TransformerDescriptors =>
			_transformerDescriptors ??
			(_transformerDescriptors =
				 _document.TransformerDescriptors?.ToDictionary(x => x.Name) ??
				 new Dictionary<string, XmlTransformerDescriptor>());

		public List<XmlWorkspace> Workspaces => _document.Workspaces;

		public IEnumerable<XmlQualityCondition> QualityConditions =>
			_qualityConditions.Select(x => x.Key);

		public IReadOnlyList<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>
			QualityConditionsWithCategories => _qualityConditions;

		[CanBeNull]
		public IDictionary<string, DdxModel> ReferencedModels { get; set; }

		[CanBeNull]
		public IIssueFilterExpressionParser IssueFilterExpressionParser { get; set; }

		[CanBeNull]
		public ITestParameterDatasetValidator ParameterDatasetValidator { get; set; }

		public XmlDataQualityDocumentCache(XmlDataQualityDocument document,
		                                   IEnumerable<KeyValuePair<XmlQualityCondition,
			                                   XmlDataQualityCategory>> qualityConditions)
		{
			_document = document;
			_qualityConditions =
				new List<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>(
					qualityConditions);
		}

		public bool TryGetIssueFilter(string name,
		                              out XmlIssueFilterConfiguration issueFilterConfiguration)
		{
			return IssueFilters.TryGetValue(name, out issueFilterConfiguration);
		}

		public bool TryGetTransformer(string name,
		                              out XmlTransformerConfiguration transformerConfiguration)
		{
			return Transformers.TryGetValue(name, out transformerConfiguration);
		}

		private IssueFilterConfiguration GetIssueFilterConfiguration(
			[NotNull] XmlIssueFilterConfiguration xmlIssueFilter, DatasetSettings datasetSettings)
		{
			_issueFilterInstances =
				_issueFilterInstances
				?? new Dictionary<XmlIssueFilterConfiguration, IssueFilterConfiguration>();

			if (! _issueFilterInstances.TryGetValue(xmlIssueFilter,
			                                        out IssueFilterConfiguration issueFilter))
			{
				if (! IssueFilterDescriptors.TryGetValue(
					    Assert.NotNull(xmlIssueFilter.IssueFilterDescriptorName),
					    out XmlIssueFilterDescriptor xmlDesc))
				{
					Assert.Fail(
						$"IssueFilter descriptor not found for {xmlIssueFilter.IssueFilterDescriptorName}");
				}

				issueFilter = new IssueFilterConfiguration(
					xmlIssueFilter.Name,
					XmlDataQualityUtils.CreateIssueFilterDescriptor(xmlDesc));
				CompleteConfiguration(issueFilter, xmlIssueFilter, datasetSettings);

				Assert.NotNull(_issueFilterInstances).Add(xmlIssueFilter, issueFilter);
			}

			return issueFilter;
		}

		public IEnumerable<XmlInstanceConfiguration> EnumReferencedConfigurationInstances()
		{
			foreach (XmlQualityCondition xmlCondition in _qualityConditions.Select(x => x.Key))
			{
				foreach (XmlInstanceConfiguration config in EnumReferencedConfigurationInstances(
					         xmlCondition))
				{
					yield return config;
				}
			}
		}

		public QualityCondition CreateQualityCondition(
			[NotNull] XmlQualityCondition xmlCondition,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
			bool ignoreForUnknownDatasets,
			[NotNull] out ICollection<DatasetTestParameterRecord> unknownDatasetParameters)
		{
			string testDescriptorName = xmlCondition.TestDescriptorName;
			Assert.True(StringUtils.IsNotEmpty(testDescriptorName),
			            $"Test descriptor name is missing in condition: {xmlCondition}");

			if (! TestDescriptors.TryGetValue(testDescriptorName.Trim(),
			                                  out XmlTestDescriptor xmlTestDescriptor))
			{
				Assert.Fail(
					"Test descriptor '{0}' referenced in quality condition '{1}' does not exist", // TODO '... quality condition ...' correct?
					testDescriptorName, xmlCondition.Name);
			}

			var result =
				new QualityCondition(xmlCondition.Name, GetTestDescriptor(xmlTestDescriptor));

			DatasetSettings datasetSettings =
				new DatasetSettings(getDatasetsByName, ignoreForUnknownDatasets);

			// The result could be set to null, if there are missing datasets.
			result = CompleteConfiguration(result, xmlCondition, datasetSettings);
			unknownDatasetParameters = datasetSettings.UnknownDatasetParameters;

			return result;
		}

		[CanBeNull]
		private T CompleteConfiguration<T>(
			[NotNull] T created,
			[NotNull] XmlInstanceConfiguration xmlInstanceConfiguration,
			[NotNull] DatasetSettings datasetSettings)
			where T : InstanceConfiguration
		{
			if (created.ParameterValues.Count > 0)
			{
				return created;
			}

			IInstanceInfo instanceInfo =
				Assert.NotNull(InstanceDescriptorUtils.GetInstanceInfo(created.InstanceDescriptor));

			Dictionary<string, TestParameter> testParametersByName =
				instanceInfo.Parameters.ToDictionary(
					parameter => parameter.Name, StringComparer.OrdinalIgnoreCase);

			if (created is QualityCondition qualityCondition)
			{
				AddIssueFilters(qualityCondition, (XmlQualityCondition) xmlInstanceConfiguration,
				                datasetSettings);
			}

			foreach (XmlTestParameterValue xmlParamValue in
			         xmlInstanceConfiguration.ParameterValues)
			{
				TestParameter testParameter;
				if (! testParametersByName.TryGetValue(xmlParamValue.TestParameterName,
				                                       out testParameter))
				{
					throw new InvalidConfigurationException(
						string.Format(
							"The name '{0}' as a test parameter in quality condition '{1}' " +
							"defined in import document does not match test descriptor.",
							xmlParamValue.TestParameterName,
							xmlInstanceConfiguration.Name));
				}

				TestParameterValue parameterValue;

				if (! string.IsNullOrWhiteSpace(xmlParamValue.TransformerName))
				{
					if (! TryGetTransformer(
						    xmlParamValue.TransformerName,
						    out XmlTransformerConfiguration xmlTransformer))
					{
						Assert.Fail(
							$"missing transformer {xmlParamValue.TransformerName} for parameter value {xmlParamValue}");
					}

					TransformerConfiguration transformerConfig =
						GetTransformerConfiguration(xmlTransformer, datasetSettings);
					CompleteConfiguration(
						transformerConfig, xmlTransformer,
						datasetSettings);

					if (xmlParamValue is XmlDatasetTestParameterValue datasetValue)
					{
						parameterValue = CreateDatasetTestParameterValue(
							testParameter, datasetValue, (Dataset) null, datasetSettings);
					}
					else if (xmlParamValue is XmlScalarTestParameterValue scalarValue)
					{
						parameterValue = CreateScalarTestParameterValue(testParameter, scalarValue);
					}
					else
					{
						throw new InvalidProgramException("Unhandled TestParameterValue " +
						                                  xmlParamValue.TestParameterName);
					}

					parameterValue.ValueSource = transformerConfig;
				}
				else if (xmlParamValue is XmlDatasetTestParameterValue datasetValue)
				{
					parameterValue = CreateDatasetTestParameterValue(
						testParameter, datasetValue,
						Assert.NotNullOrEmpty(xmlInstanceConfiguration.Name),
						datasetSettings);

					if (parameterValue == null)
					{
						datasetSettings.UnknownDatasetParameters.Add(
							new DatasetTestParameterRecord(datasetValue.Value,
							                               datasetValue.WorkspaceId));
					}
				}
				else if (xmlParamValue is XmlScalarTestParameterValue scalarValue)
				{
					parameterValue = CreateScalarTestParameterValue(testParameter, scalarValue);
				}
				else
				{
					throw new InvalidProgramException("Unhandled TestParameterValue " +
					                                  xmlParamValue.TestParameterName);
				}

				if (parameterValue != null)
				{
					created.AddParameterValue(parameterValue);
				}
			}

			// TODO: Handle missing datasets in transformersf and issue filters!
			if (datasetSettings.UnknownDatasetParameters.Count > 0)
			{
				Assert.True(datasetSettings.IgnoreUnknownDatasets,
				            nameof(datasetSettings.IgnoreUnknownDatasets));

				return null;
			}

			return created;
		}

		private void AddIssueFilters(
			[NotNull] QualityCondition qualityCondition,
			[NotNull] XmlQualityCondition xmlCondition,
			[NotNull] DatasetSettings datasetSettings)
		{
			string issueFilterExpression = xmlCondition.IssueFilterExpression?.Expression;

			foreach (string issueFilterName in GetIssueFilterNames(issueFilterExpression))
			{
				if (! TryGetIssueFilter(
					    issueFilterName.Trim(),
					    out XmlIssueFilterConfiguration xmlIssueFilterConfiguration))
				{
					Assert.Fail($"missing issue filter named {issueFilterName}");
				}

				IssueFilterConfiguration issueFilterConfiguration =
					GetIssueFilterConfiguration(xmlIssueFilterConfiguration, datasetSettings);
				qualityCondition.AddIssueFilterConfiguration(issueFilterConfiguration);
			}

			qualityCondition.IssueFilterExpression = issueFilterExpression;
		}

		[NotNull]
		private static TestParameterValue CreateScalarTestParameterValue(
			[NotNull] TestParameter testParameter,
			[NotNull] XmlScalarTestParameterValue xmlScalarTestParameterValue)
		{
			return new ScalarTestParameterValue(testParameter, xmlScalarTestParameterValue.Value);
		}

		[CanBeNull]
		private TestParameterValue CreateDatasetTestParameterValue(
			[NotNull] TestParameter testParameter,
			[NotNull] XmlDatasetTestParameterValue xmlDatasetTestParameterValue,
			[NotNull] string qualityConditionName,
			[NotNull] DatasetSettings datasetSettings)
		{
			var referenceModels =
				Assert.NotNull(ReferencedModels, $"{nameof(ReferencedModels)} not set");

			return XmlDataQualityUtils.CreateDatasetTestParameterValue(
				testParameter, xmlDatasetTestParameterValue, qualityConditionName, referenceModels,
				datasetSettings.GetDatasetsByName, ParameterDatasetValidator,
				datasetSettings.IgnoreUnknownDatasets);
		}

		[NotNull]
		private TestParameterValue CreateDatasetTestParameterValue(
			[NotNull] TestParameter testParameter,
			[NotNull] XmlDatasetTestParameterValue xmlDatasetTestParameterValue,
			[CanBeNull] Dataset dataset,
			[NotNull] DatasetSettings datasetSettings)
		{
			var paramValue = new DatasetTestParameterValue(
				testParameter, dataset,
				xmlDatasetTestParameterValue.WhereClause,
				xmlDatasetTestParameterValue.UsedAsReferenceData);

			return paramValue;
		}

		public IEnumerable<XmlInstanceConfiguration> EnumReferencedConfigurationInstances(
			XmlInstanceConfiguration config)
		{
			yield return config;

			if (config is XmlQualityCondition xmlCondition)
			{
				string issueFilterExpression = xmlCondition.IssueFilterExpression?.Expression;

				foreach (string filterName in GetIssueFilterNames(issueFilterExpression))
				{
					if (! IssueFilters.TryGetValue(filterName,
					                               out XmlIssueFilterConfiguration filter))
					{
						Assert.Fail($"missing issue filter {filterName}");
					}

					foreach (var referenced in EnumReferencedConfigurationInstances(filter))
					{
						yield return referenced;
					}
				}
			}

			foreach (XmlTestParameterValue parameterValue in config.ParameterValues)
			{
				if (! string.IsNullOrWhiteSpace(parameterValue.TransformerName))
				{
					if (! Transformers.TryGetValue(parameterValue.TransformerName,
					                               out XmlTransformerConfiguration transformer))
					{
						Assert.Fail($"missing transformer {parameterValue.TransformerName}");
					}

					foreach (var referenced in EnumReferencedConfigurationInstances(transformer))
					{
						yield return referenced;
					}
				}
			}
		}

		private TransformerConfiguration GetTransformerConfiguration(
			[NotNull] XmlTransformerConfiguration xmlTransformer,
			[NotNull] DatasetSettings datasetSettings)
		{
			_transformerInstances =
				_transformerInstances
				?? new Dictionary<XmlTransformerConfiguration, TransformerConfiguration>();

			if (! _transformerInstances.TryGetValue(xmlTransformer,
			                                        out TransformerConfiguration transformer))
			{
				if (! TransformerDescriptors.TryGetValue(
					    Assert.NotNull(xmlTransformer.TransformerDescriptorName),
					    out XmlTransformerDescriptor xmlDesc))
				{
					Assert.Fail(
						$"Transformer descriptor not found for {xmlTransformer.TransformerDescriptorName}");
				}

				transformer = new TransformerConfiguration(
					xmlTransformer.Name,
					XmlDataQualityUtils.CreateTransformerDescriptor(xmlDesc));
				CompleteConfiguration(transformer, xmlTransformer, datasetSettings);
				Assert.NotNull(_transformerInstances).Add(xmlTransformer, transformer);
			}

			return transformer;
		}

		public TestDescriptor GetTestDescriptor(
			[NotNull] XmlTestDescriptor xmlDescriptor)
		{
			_testDescriptorInstances =
				_testDescriptorInstances
				?? new Dictionary<XmlTestDescriptor, TestDescriptor>();

			if (! _testDescriptorInstances.TryGetValue(xmlDescriptor,
			                                           out TestDescriptor descriptor))
			{
				descriptor =
					XmlDataQualityUtils.CreateTestDescriptor(xmlDescriptor);
				_testDescriptorInstances.Add(xmlDescriptor, descriptor);
			}

			return descriptor;
		}

		public IList<string> GetIssueFilterNames([CanBeNull] string issueFilterExpression)
		{
			if (string.IsNullOrWhiteSpace(issueFilterExpression))
			{
				return new List<string>();
			}

			var result = Assert.NotNull(IssueFilterExpressionParser,
			                            "Filter Expression Parser not initialized")
			                   .GetFilterNames(issueFilterExpression);

			return result;
		}
	}
}