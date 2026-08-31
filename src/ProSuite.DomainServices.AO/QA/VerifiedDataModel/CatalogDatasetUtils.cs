using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	/// <summary>
	/// Applies the dataset declarations transported with a standalone condition list to the
	/// harvested model. Shared by the standalone factories: the XML one and, once the
	/// declarations are carried by the proto too, the proto-based one.
	/// </summary>
	public static class CatalogDatasetUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Replaces each harvested dataset covered by <paramref name="declarations"/> with the
		/// catalog dataset its declared <see cref="SupportedDatasetType"/> calls for, carrying
		/// the transported roles, so that the dataset opener resolves its file-path field exactly
		/// as it does for a DDX dataset. A no-op for the common case of nothing declared.
		/// </summary>
		public static void ApplyDeclarations(
			[NotNull] DdxModel model,
			[NotNull] IEnumerable<DatasetDeclaration> declarations)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(declarations, nameof(declarations));

			foreach (DatasetDeclaration declaration in declarations)
			{
				string datasetName = declaration.DatasetName;

				Dataset harvested = model.GetDatasetByModelName(datasetName);

				if (harvested == null)
				{
					// The dataset is referenced but was not harvested. Leave it to the regular
					// unknown-dataset handling, which knows whether that is fatal here.
					_msg.DebugFormat(
						"No harvested dataset {0} to apply the declaration to", datasetName);
					continue;
				}

				Dataset catalogDataset = CreateCatalogDataset(declaration, harvested);

				model.RemoveDataset(harvested);
				model.AddDataset(catalogDataset);

				_msg.DebugFormat("Using dataset {0} as a {1} ({2})", datasetName,
				                 declaration.DatasetType,
				                 StringUtils.Concatenate(declaration.FieldRoles, ", "));
			}
		}

		[NotNull]
		private static Dataset CreateCatalogDataset(
			[NotNull] DatasetDeclaration declaration,
			[NotNull] Dataset harvested)
		{
			switch (declaration.DatasetType)
			{
				case SupportedDatasetType.RasterCatalog:
					return new VerifiedRasterCatalogDataset(
						       declaration.DatasetName,
						       declaration.FieldRoles)
					       {
						       GeometryType = AssertVectorDataset(declaration, harvested)
							       .GeometryType,
						       AliasName = harvested.AliasName
					       };

				case SupportedDatasetType.PointCloudCatalog:
					return new VerifiedPointCloudCatalogDataset(
						       declaration.DatasetName,
						       declaration.FieldRoles)
					       {
						       GeometryType = AssertVectorDataset(declaration, harvested)
							       .GeometryType,
						       AliasName = harvested.AliasName
					       };

				default:
					throw new InvalidConfigurationException(
						$"Dataset {declaration.DatasetName} carries field roles, but its declared " +
						$"type {declaration.DatasetType} has no catalog dataset representation.");
			}
		}

		[NotNull]
		private static VectorDataset AssertVectorDataset(
			[NotNull] DatasetDeclaration declaration,
			[NotNull] Dataset harvested)
		{
			if (harvested is VectorDataset vectorDataset)
			{
				return vectorDataset;
			}

			// Both catalog kinds are feature classes in the geodatabase, so anything else means
			// the document and the workspace disagree about what this dataset is.
			throw new InvalidConfigurationException(
				$"Dataset {declaration.DatasetName} is declared as a " +
				$"{declaration.DatasetType}, but it was harvested as " +
				$"{harvested.GetType().Name}. A file catalog must be a polygon feature class.");
		}
	}
}
