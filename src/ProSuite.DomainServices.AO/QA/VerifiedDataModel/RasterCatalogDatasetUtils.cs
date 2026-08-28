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
	/// Applies the attribute-role assignments transported with a standalone condition list to the
	/// harvested model. Shared by the standalone factories: the XML one and, once the field roles
	/// are carried by the proto too, the proto-based one.
	/// </summary>
	public static class RasterCatalogDatasetUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Replaces each harvested dataset named in <paramref name="fieldRolesByDatasetName"/>
		/// with a <see cref="VerifiedRasterCatalogDataset"/> carrying the given roles, so that the
		/// dataset opener resolves its file-path field through
		/// <see cref="IRasterCatalogDataset.FilePathFieldName"/>, exactly as it does for a DDX
		/// dataset. A no-op for the common case of no transported roles.
		/// </summary>
		public static void ApplyFieldRoles(
			[NotNull] DdxModel model,
			[NotNull] IDictionary<string, IList<DatasetFieldRole>> fieldRolesByDatasetName)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(fieldRolesByDatasetName, nameof(fieldRolesByDatasetName));

			foreach (KeyValuePair<string, IList<DatasetFieldRole>> pair in
			         fieldRolesByDatasetName)
			{
				string datasetName = pair.Key;

				Dataset harvested = model.GetDatasetByModelName(datasetName);

				if (harvested == null)
				{
					// The dataset is referenced but was not harvested. Leave it to the regular
					// unknown-dataset handling, which knows whether that is fatal here.
					_msg.DebugFormat(
						"No harvested dataset {0} to apply the transported field roles to",
						datasetName);
					continue;
				}

				if (! (harvested is VectorDataset))
				{
					throw new InvalidConfigurationException(
						$"Dataset {datasetName} is referenced as a raster catalog, but it was " +
						$"harvested as {harvested.GetType().Name}. A raster catalog must be a " +
						"polygon feature class.");
				}

				var catalogDataset =
					new VerifiedRasterCatalogDataset(datasetName, pair.Value)
					{
						GeometryType = harvested.GeometryType,
						AliasName = harvested.AliasName
					};

				model.RemoveDataset(harvested);
				model.AddDataset(catalogDataset);

				_msg.DebugFormat("Using dataset {0} as a raster catalog ({1})",
				                 datasetName, StringUtils.Concatenate(pair.Value, ", "));
			}
		}
	}
}
