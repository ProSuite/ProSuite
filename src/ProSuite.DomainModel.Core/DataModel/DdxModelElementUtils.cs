using System;
using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.DataModel
{
	public static class DdxModelElementUtils
	{
		/// <summary>
		/// TODO: This should probably be moved back to XmlDataQualityUtils once all the usages have been cleaned up.
		/// </summary>
		/// <param name="datasetName"></param>
		/// <param name="workspaceId"></param>
		/// <param name="testParameter"></param>
		/// <param name="instanceConfigurationName"></param>
		/// <param name="modelsByWorkspaceId"></param>
		/// <param name="getDatasetsByName"></param>
		/// <param name="ignoreUnknownDataset"></param>
		/// <returns></returns>
		[CanBeNull]
		public static Dataset GetDataset(
			[CanBeNull] string datasetName,
			[CanBeNull] string workspaceId,
			[NotNull] TestParameter testParameter,
			[NotNull] string instanceConfigurationName,
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			[NotNull] Func<string, IList<Dataset>> getDatasetsByName,
			bool ignoreUnknownDataset)
		{
			if (string.IsNullOrWhiteSpace(datasetName))
			{
				if (testParameter.IsConstructorParameter)
				{
					Assert.NotNullOrEmpty(
						datasetName,
						"Dataset is not defined for constructor-parameter '{0}' in configuration '{1}'",
						testParameter.Name, instanceConfigurationName);
				}

				return null;
			}

			if (StringUtils.IsNotEmpty(workspaceId))
			{
				Assert.True(modelsByWorkspaceId.TryGetValue(workspaceId, out DdxModel model),
				            "No matching model found for workspace id '{0}'", workspaceId);

				return GetDatasetFromStoredName(datasetName,
				                                model, ignoreUnknownDataset);
			}

			if (StringUtils.IsNullOrEmptyOrBlank(workspaceId))
			{
				const string defaultModelId = "";

				DdxModel defaultModel;
				if (modelsByWorkspaceId.TryGetValue(defaultModelId, out defaultModel))
				{
					// there is a default model
					return GetDatasetFromStoredName(datasetName,
					                                defaultModel,
					                                ignoreUnknownDataset);
				}
			}

			// no workspace id for dataset, and there is no default model

			IList<Dataset> datasets = getDatasetsByName(datasetName);

			Assert.False(datasets.Count > 1,
			             "More than one dataset found with name '{0}', for parameter '{1}' in configuration '{2}'",
			             datasetName, testParameter.Name, instanceConfigurationName);

			if (datasets.Count == 0)
			{
				if (ignoreUnknownDataset)
				{
					return null;
				}

				Assert.False(datasets.Count == 0,
				             "Dataset '{0}' for parameter '{1}' in configuration '{2}' not found",
				             datasetName, testParameter.Name, instanceConfigurationName);
			}

			return datasets[0];
		}

		[CanBeNull]
		public static Dataset GetDatasetFromStoredName([NotNull] string storedDatasetName,
		                                               [NotNull] DdxModel model,
		                                               bool ignoreUnknownDataset)
		{
			Assert.ArgumentNotNullOrEmpty(storedDatasetName, nameof(storedDatasetName));
			Assert.ArgumentNotNull(model, nameof(model));

			string searchName = GetModelElementNameFromStoredName(storedDatasetName, model);

			Dataset dataset = model.GetDatasetByModelName(searchName);

			if (dataset != null)
			{
				return dataset;
			}

			string unqualifiedName;
			if (ModelElementNameUtils.TryUnqualifyName(storedDatasetName, out unqualifiedName))
			{
				dataset = model.GetDatasetByModelName(unqualifiedName);
				if (dataset != null)
				{
					return dataset;
				}
			}

			if (ignoreUnknownDataset)
			{
				return null;
			}

			throw new ArgumentException(
				$"No dataset with name '{storedDatasetName}' exists in model '{model.Name}'");
		}

		[NotNull]
		public static string GetModelElementNameFromStoredName(
			[NotNull] string modelElementName,
			[NotNull] DdxModel model)
		{
			if (! ModelElementNameUtils.IsQualifiedName(modelElementName) &&
			    model.ElementNamesAreQualified)
			{
				model.QualifyModelElementName(modelElementName);
			}

			if (! model.ElementNamesAreQualified)
			{
				string unqualifiedName;
				if (ModelElementNameUtils.TryUnqualifyName(modelElementName, out unqualifiedName))
				{
					return unqualifiedName;
				}
			}

			return modelElementName;
		}

		[CanBeNull]
		public static Association GetAssociationFromStoredName(
			[NotNull] string storedAssociationName,
			[NotNull] DdxModel model,
			bool ignoreUnknownAssociation)
		{
			Assert.ArgumentNotNullOrEmpty(storedAssociationName, nameof(storedAssociationName));
			Assert.ArgumentNotNull(model, nameof(model));

			string searchName = GetModelElementNameFromStoredName(storedAssociationName, model);

			Association association = model.GetAssociationByModelName(searchName);

			if (association != null)
			{
				return association;
			}

			string unqualifiedName;
			if (ModelElementNameUtils.TryUnqualifyName(storedAssociationName, out unqualifiedName))
			{
				association = model.GetAssociationByModelName(unqualifiedName);
				if (association != null)
				{
					return association;
				}
			}

			if (ignoreUnknownAssociation)
			{
				return null;
			}

			throw new ArgumentException(
				$"No association with name '{storedAssociationName}' exists in model '{model.Name}'");
		}
	}
}
