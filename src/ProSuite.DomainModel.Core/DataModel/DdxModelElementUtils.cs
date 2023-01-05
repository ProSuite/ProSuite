using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public static class DdxModelElementUtils
	{
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
