using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public static class ModelUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static string QualifyModelElementName(
			DdxModel model, string modelElementName)
		{
			if (model is null)
				throw new ArgumentNullException(nameof(model));

			IWorkspace workspace = model.GetMasterDatabaseWorkspace();

			if (workspace == null)
			{
				return modelElementName;
			}

			return DatasetUtils.QualifyTableName(workspace,
			                                     model.DefaultDatabaseName,
			                                     model.DefaultDatabaseSchemaOwner,
			                                     modelElementName);
		}

		[NotNull]
		public static string TranslateToModelElementName(
			[NotNull] DdxModel model, [NotNull] string masterDatabaseDatasetName)
		{
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNullOrEmpty(masterDatabaseDatasetName,
			                              nameof(masterDatabaseDatasetName));

			// the master database context does not support any prefix mappings etc.

			// translate query class name (if it is one) to table name
			string gdbDatasetName = ModelElementUtils.GetBaseTableName(
				masterDatabaseDatasetName, model.GetMasterDatabaseWorkspaceContext());

			return model.ElementNamesAreQualified
				       ? gdbDatasetName // expected to be qualified also
				       : ModelElementNameUtils.GetUnqualifiedName(gdbDatasetName);
		}

		[NotNull]
		public static IWorkspaceContext CreateDefaultMasterDatabaseWorkspaceContext(DdxModel model)
		{
			Assert.ArgumentNotNull(model, nameof(model));

			_msg.Debug("Opening default master database workspace context...");

			IFeatureWorkspace featureWorkspace = model.UserConnectionProvider.OpenWorkspace();

			var result = new MasterDatabaseWorkspaceContext(featureWorkspace, model);

			if (model.AutoEnableSchemaCache && ! model.DisableAutomaticSchemaCaching)
			{
				// The model schema cache can be turned OFF by environment variable.
				bool noModelSchemaCache =
					EnvironmentUtils.GetBooleanEnvironmentVariableValue(
						DdxModel.EnvironmentVariableNoModelSchemaCache);

				if (! noModelSchemaCache)
				{
					WorkspaceUtils.EnableSchemaCache(result.Workspace);
				}
			}

			return result;
		}
	}
}
