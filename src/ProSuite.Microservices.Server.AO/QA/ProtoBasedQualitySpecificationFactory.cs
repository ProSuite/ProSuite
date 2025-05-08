using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Core;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class ProtoBasedQualitySpecificationFactory : ProtoBasedQualitySpecificationFactoryBase
	{
		private static int _currentModelId = -100;

		private readonly ICollection<DataSource> _dataSources;

		/// <summary>
		/// QualitySpecification factory that uses a fine-grained proto buf message as input.
		/// </summary>
		/// <param name="modelFactory">The model factory</param>
		/// <param name="dataSources"></param>
		/// <param name="instanceDescriptors">All supported instance descriptors</param>
		public ProtoBasedQualitySpecificationFactory(
			[NotNull] IVerifiedModelFactory modelFactory,
			ICollection<DataSource> dataSources,
			[NotNull] ISupportedInstanceDescriptors instanceDescriptors)
			: base(instanceDescriptors)
		{
			_dataSources = dataSources;
			ModelFactory = modelFactory;
		}

		/// <summary>
		/// QualitySpecification factory that uses a fine-grained proto buf message as input.
		/// </summary>
		/// <param name="modelsByWorkspaceId">The known models by workspace Id</param>
		/// <param name="instanceDescriptors">All supported instance descriptors</param>
		public ProtoBasedQualitySpecificationFactory(
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] ISupportedInstanceDescriptors instanceDescriptors)
			: base(instanceDescriptors)
		{
			ModelsByWorkspaceId = modelsByWorkspaceId;
		}

		private IVerifiedModelFactory ModelFactory { get; }

		[NotNull]
		protected override IDictionary<string, DdxModel> GetModelsByWorkspaceId(
			ConditionListSpecificationMsg conditionListSpecificationMsg)
		{
			Assert.NotNull(_dataSources, nameof(_dataSources));

			var result = new Dictionary<string, DdxModel>(StringComparer.OrdinalIgnoreCase);

			List<QualityConditionMsg> referencedConditions =
				conditionListSpecificationMsg.Elements.Select(e => e.Condition).ToList();

			foreach (DataSource dataSource in _dataSources)
			{
				if (! int.TryParse(dataSource.ID, out int modelId))
				{
					// TODO: The following is not correct! Otherwise, no SR can be assigned to the model
					// If the model is harvested using VerifiedModelFactory it is not important
					// that the modelId matches the dataSource.ID. Just use a unique, non-persistent model id
					modelId = _currentModelId--;
				}

				result.Add(dataSource.ID,
				           CreateModel(dataSource.OpenWorkspace(),
				                       dataSource.DisplayName,
				                       modelId,
				                       dataSource.DatabaseName,
				                       dataSource.SchemaOwner,
				                       referencedConditions));
			}

			return result;
		}

		protected override void AssertValidDataset(TestParameter testParameter, Dataset dataset)
		{
			TestParameterTypeUtils.AssertValidDataset(testParameter, dataset);
		}

		protected override IInstanceInfo CreateInstanceFactory<T>(T created)
		{
			IInstanceInfo instanceFactory =
				Assert.NotNull(InstanceFactoryUtils.CreateFactory(created));
			return instanceFactory;
		}

		protected override TestParameterValue CreateEmptyTestParameterValue<T>(
			TestParameter testParameter)
		{
			// TODO: Implement and test the case of an empty list parameter

			TestParameterValue parameterValue =
				TestParameterTypeUtils.GetEmptyParameterValue(testParameter);
			return parameterValue;
		}

		[NotNull]
		private DdxModel CreateModel(
			[NotNull] IWorkspace workspace,
			[NotNull] string modelName,
			int workspaceId,
			[CanBeNull] string databaseName,
			[CanBeNull] string schemaOwner,
			[NotNull] IEnumerable<QualityConditionMsg> referencedConditions)
		{
			Model result = ModelFactory.CreateModel(workspace, modelName, workspaceId,
			                                        databaseName, schemaOwner);

			if (result.SpatialReferenceDescriptor == null)
			{
				IEnumerable<Dataset> referencedDatasets = GetReferencedDatasets(
					result, workspaceId.ToString(CultureInfo.InvariantCulture),
					referencedConditions);

				ModelFactory.AssignMostFrequentlyUsedSpatialReference(result, referencedDatasets);
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<Dataset> GetReferencedDatasets(
			[NotNull] DdxModel model,
			[NotNull] string workspaceId,
			[NotNull] IEnumerable<QualityConditionMsg> referencedConditions)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(referencedConditions, nameof(referencedConditions));
			Assert.ArgumentNotNullOrEmpty(workspaceId, nameof(workspaceId));

			var datasetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (QualityConditionMsg conditionMsg in referencedConditions)
			{
				foreach (ParameterMsg parameterMsg in conditionMsg.Parameters)
				{
					if (string.IsNullOrEmpty(parameterMsg.WorkspaceId))
					{
						// No dataset parameter
						continue;
					}

					bool equalsModelId = string.Equals(parameterMsg.WorkspaceId, workspaceId,
					                                   StringComparison.OrdinalIgnoreCase);

					bool equalsModelName = string.Equals(parameterMsg.WorkspaceId,
					                                     model.Name,
					                                     StringComparison.OrdinalIgnoreCase);

					if (! equalsModelId && ! equalsModelName)
					{
						continue;
					}

					string datasetName = parameterMsg.Value;
					if (datasetName == null || datasetNames.Contains(datasetName))
					{
						continue;
					}

					datasetNames.Add(datasetName);

					Dataset dataset =
						XmlDataQualityUtils.GetDatasetByParameterValue(model, datasetName);

					if (dataset != null)
					{
						yield return dataset;
					}
				}
			}
		}
	}
}
