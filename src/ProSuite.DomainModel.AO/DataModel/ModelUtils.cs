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

			return CreateMasterDatabaseWorkspaceContextCore(model, featureWorkspace,
			                                                model.KeepDatasetLocks);
		}

		/// <summary>
		/// Tries to create a master database context owned by the caller. As opposed to
		/// <see cref="CreateDefaultMasterDatabaseWorkspaceContext"/> an inaccessible master
		/// database is not an error but results in a <c>false</c> return value, allowing the
		/// caller to degrade gracefully.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <param name="keepDatasetLocks">Whether the opened datasets shall be cached (and hence
		/// their schema locks kept) for the lifetime of the returned context.</param>
		/// <param name="workspaceContext">The created context, to be disposed by the caller,
		/// or null, if the master database is not accessible.</param>
		/// <param name="noAccessReason">The reason why the master database is not accessible,
		/// or null in case of success.</param>
		public static bool TryCreateDefaultMasterDatabaseWorkspaceContext(
			[NotNull] DdxModel model, bool keepDatasetLocks,
			[CanBeNull] out IWorkspaceContext workspaceContext,
			[CanBeNull] out string noAccessReason)
		{
			Assert.ArgumentNotNull(model, nameof(model));

			workspaceContext = null;

			if (model.UserConnectionProvider == null)
			{
				noAccessReason = "No user connection provider defined for model";
				return false;
			}

			IFeatureWorkspace featureWorkspace;

			try
			{
				_msg.Debug("Opening default master database workspace context...");

				featureWorkspace = model.UserConnectionProvider.OpenWorkspace();
			}
			catch (Exception e)
			{
				noAccessReason = e.Message;

				_msg.Warn(
					$"Error opening master database for model {model.Name}: {e.Message}", e);

				return false;
			}

			workspaceContext = CreateMasterDatabaseWorkspaceContextCore(
				model, featureWorkspace, keepDatasetLocks);

			noAccessReason = null;
			return true;
		}

		[NotNull]
		private static IWorkspaceContext CreateMasterDatabaseWorkspaceContextCore(
			[NotNull] DdxModel model, [NotNull] IFeatureWorkspace featureWorkspace,
			bool keepDatasetLocks)
		{
			var result =
				new MasterDatabaseWorkspaceContext(featureWorkspace, model, keepDatasetLocks);

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
