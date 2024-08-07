using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class XmlDataQualityDocumentCache
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly XmlDataQualityDocument _document;

		private Dictionary<string, XmlTransformerConfiguration> _transformersByName;
		private Dictionary<string, XmlIssueFilterConfiguration> _issueFiltersByName;

		private Dictionary<string, XmlTestDescriptor> _testDescriptors;
		private Dictionary<string, XmlTransformerDescriptor> _transformerDescriptors;
		private Dictionary<string, XmlIssueFilterDescriptor> _issueFilterDescriptors;

		[CanBeNull] private Dictionary<XmlTransformerConfiguration, TransformerConfiguration>
			_transformerInstances;

		[CanBeNull] private Dictionary<XmlIssueFilterConfiguration, IssueFilterConfiguration>
			_issueFilterInstances;

		[NotNull]
		private Dictionary<string, XmlTransformerConfiguration> TransformersByName =>
			_transformersByName ??
			(_transformersByName =
				 TransformersWithCategories.Select(x => x.Key).ToDictionary(x => x.Name));

		[NotNull]
		private Dictionary<string, XmlIssueFilterConfiguration> IssueFiltersByName =>
			_issueFiltersByName ??
			(_issueFiltersByName =
				 IssueFiltersWithCategories.Select(x => x.Key).ToDictionary(x => x.Name));

		[NotNull]
		private Dictionary<string, XmlTestDescriptor> TestDescriptorsByName =>
			_testDescriptors ??
			(_testDescriptors = _document.TestDescriptors?.ToDictionary(x => x.Name) ??
			                    new Dictionary<string, XmlTestDescriptor>());

		[NotNull]
		private Dictionary<string, XmlTransformerDescriptor> TransformerDescriptorsByName =>
			_transformerDescriptors ??
			(_transformerDescriptors =
				 _document.TransformerDescriptors?.ToDictionary(x => x.Name) ??
				 new Dictionary<string, XmlTransformerDescriptor>());

		[NotNull]
		private Dictionary<string, XmlIssueFilterDescriptor> IssueFilterDescriptorsByName =>
			_issueFilterDescriptors ??
			(_issueFilterDescriptors =
				 _document.IssueFilterDescriptors?.ToDictionary(x => x.Name) ??
				 new Dictionary<string, XmlIssueFilterDescriptor>());

		public IReadOnlyList<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>
			QualityConditionsWithCategories { get; }

		public IReadOnlyList<KeyValuePair<XmlTransformerConfiguration, XmlDataQualityCategory>>
			TransformersWithCategories { get; }

		public IReadOnlyList<KeyValuePair<XmlIssueFilterConfiguration, XmlDataQualityCategory>>
			IssueFiltersWithCategories { get; }

		public List<XmlWorkspace> Workspaces => _document.Workspaces;

		[CanBeNull]
		public IDictionary<string, DdxModel> ReferencedModels { get; set; }

		[CanBeNull]
		public IDictionary<XmlDataQualityCategory, DataQualityCategory> CategoryMap { get; set; }

		[CanBeNull]
		public ITestParameterDatasetValidator ParameterDatasetValidator { get; set; }

		public XmlDataQualityDocumentCache(XmlDataQualityDocument document,
		                                   IEnumerable<KeyValuePair<XmlQualityCondition,
			                                   XmlDataQualityCategory>> qualityConditions)
		{
			_document = document;

			QualityConditionsWithCategories =
				new List<KeyValuePair<XmlQualityCondition, XmlDataQualityCategory>>(
					qualityConditions);

			TransformersWithCategories =
				new List<KeyValuePair<XmlTransformerConfiguration, XmlDataQualityCategory>>(
					_document.GetAllTransformers());

			IssueFiltersWithCategories =
				new List<KeyValuePair<XmlIssueFilterConfiguration, XmlDataQualityCategory>>(
					_document.GetAllIssueFilters());
		}

		[CanBeNull]
		public DataQualityCategory GetDataQualityCategoryFor(
			[NotNull] XmlInstanceConfiguration xmlConfig)
		{
			var categories = Assert.NotNull(CategoryMap, $"{nameof(CategoryMap)} not set");

			XmlDataQualityCategory xmlCategory = null;

			if (xmlConfig is XmlTransformerConfiguration)
			{
				foreach (var pair in TransformersWithCategories)
				{
					if (pair.Key.Equals(xmlConfig)) xmlCategory = pair.Value;
				}
			}
			else if (xmlConfig is XmlIssueFilterConfiguration)
			{
				foreach (var pair in IssueFiltersWithCategories)
				{
					if (pair.Key.Equals(xmlConfig)) xmlCategory = pair.Value;
				}
			}
			else if (xmlConfig is XmlQualityCondition)
			{
				foreach (var pair in QualityConditionsWithCategories)
				{
					if (pair.Key.Equals(xmlConfig)) xmlCategory = pair.Value;
				}
			}
			else
			{
				throw new InvalidConfigurationException($"Unhandled type {xmlConfig.GetType()}");
			}

			return xmlCategory == null ? null : categories[xmlCategory];
		}

		public bool TryGetIssueFilter(string name,
		                              out XmlIssueFilterConfiguration issueFilterConfiguration)
		{
			return IssueFiltersByName.TryGetValue(name, out issueFilterConfiguration);
		}

		public bool TryGetTransformer(string name,
		                              out XmlTransformerConfiguration transformerConfiguration)
		{
			return TransformersByName.TryGetValue(name, out transformerConfiguration);
		}

		public IEnumerable<XmlInstanceConfiguration> EnumReferencedConfigurationInstances()
		{
			foreach (XmlQualityCondition xmlCondition in QualityConditionsWithCategories.Select(
				         x => x.Key))
			{
				foreach (XmlInstanceConfiguration config in EnumReferencedConfigurationInstances(
					         xmlCondition))
				{
					yield return config;
				}
			}
		}

		public IEnumerable<XmlInstanceConfiguration> EnumReferencedConfigurationInstances(
			XmlInstanceConfiguration xmlConfig)
		{
			yield return xmlConfig;

			if (xmlConfig is XmlQualityCondition xmlCondition)
			{
				if (xmlCondition.Filters != null)
				{
					foreach (string filterName in xmlCondition.Filters.Select(
						         f => f.IssueFilterName))
					{
						if (! TryGetIssueFilter(
							    filterName.Trim(),
							    out XmlIssueFilterConfiguration xmlIssueFilterConfiguration))
						{
							Assert.Fail($"missing issue filter named {filterName}");
						}

						foreach (var referenced in EnumReferencedConfigurationInstances(
							         xmlIssueFilterConfiguration))
						{
							yield return referenced;
						}
					}
				}
			}

			foreach (XmlTestParameterValue parameterValue in xmlConfig.ParameterValues)
			{
				if (! string.IsNullOrWhiteSpace(parameterValue.TransformerName))
				{
					if (! TransformersByName.TryGetValue(parameterValue.TransformerName,
					                                     out XmlTransformerConfiguration
						                                         transformer))
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

		[CanBeNull]
		public QualityCondition CreateQualityCondition(
			[NotNull] XmlQualityCondition xmlCondition,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
			[NotNull] FactorySettings factorySettings,
			[NotNull] out ICollection<DatasetTestParameterRecord> unknownDatasetParameters)
		{
			string testDescriptorName = xmlCondition.TestDescriptorName;
			Assert.True(StringUtils.IsNotEmpty(testDescriptorName),
			            $"Test descriptor name is missing in condition: {xmlCondition}");

			if (! TestDescriptorsByName.TryGetValue(testDescriptorName.Trim(),
			                                        out XmlTestDescriptor xmlDesc))
			{
				Assert.Fail(
					"Test descriptor '{0}' referenced in quality condition '{1}' does not exist",
					testDescriptorName, xmlCondition.Name);
			}

			var condition =
				new QualityCondition(xmlCondition.Name,
				                     XmlDataQualityUtils.CreateTestDescriptor(xmlDesc));

			// Use the UUIDs from the XML for consistency (and because of downstream equality assertion)
			if (StringUtils.IsNotEmpty(xmlCondition.Uuid))
			{
				condition.Uuid = xmlCondition.Uuid;
			}

			XmlDataQualityUtils.UpdateQualityCondition(condition, xmlCondition,
			                                           GetDataQualityCategoryFor(xmlCondition));

			DatasetSettings datasetSettings =
				new DatasetSettings(getDatasetsByName, factorySettings.IgnoreConditionsForUnknownDatasets);

			// The result could be set to null, if there are missing datasets.
			condition = CompleteConfiguration(
				condition, xmlCondition, factorySettings, datasetSettings);
			unknownDatasetParameters = datasetSettings.UnknownDatasetParameters;

			return condition;
		}

		[CanBeNull]
		private T CompleteConfiguration<T>(
			[NotNull] T created,
			[NotNull] XmlInstanceConfiguration xmlConfig,
			[NotNull] FactorySettings factorySettings,
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
				AddIssueFilters(
					qualityCondition, (XmlQualityCondition) xmlConfig,
					factorySettings, datasetSettings);
			}

			foreach (XmlTestParameterValue xmlParamValue in xmlConfig.ParameterValues)
			{
				TestParameter testParameter;
				if (! testParametersByName.TryGetValue(xmlParamValue.TestParameterName,
				                                       out testParameter))
				{
					string msg = string.Format(
						"The name '{0}' as a test parameter in quality condition '{1}' " +
						"defined in import document does not match test descriptor.",
						xmlParamValue.TestParameterName, xmlConfig.Name);

					if (factorySettings.IgnoreUnknownParameters)
					{
						_msg.Warn(msg);
						continue;
					}

					throw new InvalidConfigurationException(msg);
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
						GetTransformerConfiguration(xmlTransformer, factorySettings,
						                            datasetSettings);
					CompleteConfiguration(
						transformerConfig, xmlTransformer,
						factorySettings, datasetSettings);

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
						Assert.NotNullOrEmpty(xmlConfig.Name), datasetSettings);

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

			// TODO: Handle missing datasets in transformers and issue filters!
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
			[NotNull] FactorySettings factorySettings,
			[NotNull] DatasetSettings datasetSettings)
		{
			if (xmlCondition.Filters != null)
			{
				foreach (string filterName in xmlCondition.Filters.Select(f => f.IssueFilterName))
				{
					if (! TryGetIssueFilter(
						    filterName.Trim(),
						    out XmlIssueFilterConfiguration xmlIssueFilterConfiguration))
					{
						Assert.Fail($"missing issue filter named {filterName}");
					}

					IssueFilterConfiguration issueFilterConfiguration =
						GetIssueFilterConfiguration(xmlIssueFilterConfiguration, factorySettings,
						                            datasetSettings);
					qualityCondition.AddIssueFilterConfiguration(issueFilterConfiguration);
				}
			}

			qualityCondition.IssueFilterExpression = xmlCondition.FilterExpression?.Expression;
		}

		private IssueFilterConfiguration GetIssueFilterConfiguration(
			[NotNull] XmlIssueFilterConfiguration xmlIssueFilter,
			FactorySettings factorySettings, DatasetSettings datasetSettings)
		{
			_issueFilterInstances =
				_issueFilterInstances
				?? new Dictionary<XmlIssueFilterConfiguration, IssueFilterConfiguration>();

			if (! _issueFilterInstances.TryGetValue(xmlIssueFilter,
			                                        out IssueFilterConfiguration issueFilter))
			{
				if (! IssueFilterDescriptorsByName.TryGetValue(
					    Assert.NotNull(xmlIssueFilter.IssueFilterDescriptorName),
					    out XmlIssueFilterDescriptor xmlDesc))
				{
					Assert.Fail(
						$"IssueFilter descriptor not found for {xmlIssueFilter.IssueFilterDescriptorName}");
				}

				issueFilter = new IssueFilterConfiguration(
					xmlIssueFilter.Name,
					XmlDataQualityUtils.CreateIssueFilterDescriptor(xmlDesc));

				// Use the UUIDs from the XML for consistency (and because of downstream equality assertion)
				if (StringUtils.IsNotEmpty(xmlIssueFilter.Uuid))
				{
					issueFilter.Uuid = xmlIssueFilter.Uuid;
				}

				XmlDataQualityUtils.UpdateIssueFilterConfiguration(
					issueFilter, xmlIssueFilter, GetDataQualityCategoryFor(xmlIssueFilter));

				CompleteConfiguration(issueFilter, xmlIssueFilter, factorySettings,
				                      datasetSettings);

				Assert.NotNull(_issueFilterInstances).Add(xmlIssueFilter, issueFilter);
			}

			return issueFilter;
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
		private static TestParameterValue CreateDatasetTestParameterValue(
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

		private TransformerConfiguration GetTransformerConfiguration(
			[NotNull] XmlTransformerConfiguration xmlTransformer,
			[NotNull] FactorySettings factorySettings,
			[NotNull] DatasetSettings datasetSettings)
		{
			_transformerInstances =
				_transformerInstances
				?? new Dictionary<XmlTransformerConfiguration, TransformerConfiguration>();

			if (! _transformerInstances.TryGetValue(xmlTransformer,
			                                        out TransformerConfiguration transformer))
			{
				if (! TransformerDescriptorsByName.TryGetValue(
					    Assert.NotNull(xmlTransformer.TransformerDescriptorName),
					    out XmlTransformerDescriptor xmlDesc))
				{
					Assert.Fail(
						$"Transformer descriptor not found for {xmlTransformer.TransformerDescriptorName}");
				}

				transformer = new TransformerConfiguration(
					xmlTransformer.Name,
					XmlDataQualityUtils.CreateTransformerDescriptor(xmlDesc));

				// Use the UUIDs from the XML for consistency (and because of downstream equality assertion)
				if (StringUtils.IsNotEmpty(xmlTransformer.Uuid))
				{
					transformer.Uuid = xmlTransformer.Uuid;
				}

				XmlDataQualityUtils.UpdateTransformerConfiguration(transformer, xmlTransformer,
					GetDataQualityCategoryFor(xmlTransformer));

				CompleteConfiguration(transformer, xmlTransformer, factorySettings,
				                      datasetSettings);
				Assert.NotNull(_transformerInstances).Add(xmlTransformer, transformer);
			}

			return transformer;
		}
	}
}
