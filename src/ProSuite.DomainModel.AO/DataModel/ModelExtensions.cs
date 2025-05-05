using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.Geodatabase;

namespace ProSuite.DomainModel.AO.DataModel
{
	public static class ModelExtensions
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Get the workspace context for the model's master database.
		/// This is either a cached workspace context instance, or it
		/// will be created (that is, a workspace will be opened).
		/// </summary>
		[CanBeNull]
		public static IWorkspaceContext GetMasterDatabaseWorkspaceContext(this Model model)
		{
			var masterDatabase = GetMasterDatabase(model);

			if (! model.IsMasterDatabaseAccessible())
			{
				model.CachedMasterDatabaseWorkspaceContext = null;
				return null;
			}

			if (! (model.CachedMasterDatabaseWorkspaceContext is IWorkspaceContext context))
			{
				context = masterDatabase.CreateMasterDatabaseWorkspaceContext();
				model.CachedMasterDatabaseWorkspaceContext = context;
			}

			return context;

			// ------- originally on Model class: ------
			//if (!model.IsMasterDatabaseAccessible())
			//{
			//	_masterDatabaseWorkspaceContext = null;
			//}
			//else if (_masterDatabaseWorkspaceContext == null)
			//{
			//	_masterDatabaseWorkspaceContext = model.CreateMasterDatabaseWorkspaceContext();
			//}
			//return _masterDatabaseWorkspaceContext;
		}

		/// <summary>
		/// Opens the workspace, without building the schema cache even
		/// if <see cref="Model.AutoEnableSchemaCache"/> is set to <c>true</c>.
		/// </summary>
		[CanBeNull]
		public static IWorkspace GetMasterDatabaseWorkspace(this Model model)
		{
			if (model.CachedMasterDatabaseWorkspaceContext is IWorkspaceContext workspaceContext)
			{
				return workspaceContext.Workspace;
			}
			//if (_masterDatabaseWorkspaceContext != null)
			//{
			//	return _masterDatabaseWorkspaceContext.Workspace;
			//}

			if (model.UserConnectionProvider == null)
			{
				return null;
			}

			if (!model.IsMasterDatabaseAccessible())
			{
				return null;
			}

			return (IWorkspace) model.UserConnectionProvider.OpenWorkspace();
			//return (IWorkspace) UserConnectionProvider.OpenWorkspace();
		}

		public static bool IsMasterDatabaseAccessible(this Model model)
		{
			if (model is IModelMasterDatabase)
			{
				if (! model.CachedIsMasterDatabaseAccessible.HasValue)
				{
					model.CachedIsMasterDatabaseAccessible =
						DetermineMasterDatabaseWorkspaceAccessibility(model);
				}

				return model.CachedIsMasterDatabaseAccessible.Value;
			}

			return false;
		}

		public static string GetMasterDatabaseNoAccessReason(this Model model)
		{
			if (model is null)
				throw new ArgumentNullException(nameof(model));

			if (model.UserConnectionProvider == null)
			{
				return "No user connection provider defined for model";
			}

			return model.CachedMasterDatabaseNoAccessReason; // _lastMasterDatabaseAccessError (Model)
		}

		public static bool TryGetMasterDatabaseWorkspaceContext(
			this Model model, out IWorkspaceContext result, out string noAccessReason)
		{
			Assert.ArgumentNotNull(model, nameof(model));

			result = model.GetMasterDatabaseWorkspaceContext();

			if (result == null)
			{
				noAccessReason = model.GetMasterDatabaseNoAccessReason();
				return false;
			}

			noAccessReason = null;
			return true;
		}

		private static bool DetermineMasterDatabaseWorkspaceAccessibility(this Model model)
		{
			if (model.UserConnectionProvider == null)
			{
				return false;
			}

			try
			{
				// try to open the workspace
				model.UserConnectionProvider.OpenWorkspace();

				return true;
			}
			catch (Exception ex)
			{
				_msg.DebugFormat("Error opening master database for model {0}: {1}",
				                 model.Name, ex.Message);

				//_lastMasterDatabaseAccessError = ex.Message;
				model.CachedMasterDatabaseNoAccessReason = ex.Message;

				return false;
			}
		}

		[NotNull]
		public static IWorkspaceContext AssertMasterDatabaseWorkspaceContextAccessible(
			this Model model)
		{
			Assert.ArgumentNotNull(model, nameof(model));

			if (! model.TryGetMasterDatabaseWorkspaceContext(out IWorkspaceContext workspaceContext,
			                                                 out string noAccessReason))
			{
				throw new AssertionException(
					$"The master database of model {model.Name} is not accessible: {noAccessReason}");
			}

			return workspaceContext;
		}

		#region Schema Cache Control

		public static void EnableSchemaCache(this Model model) // TODO move to ModelUtils?
		{
			// schema cache might have been discarded (e.g. Reconcile does this)

			// Note:
			// The model Schema Cache can be turned OFF by environment variable.
			// This is experimental and used while analysing memory consumption.
			bool noModelSchemaCache =
				EnvironmentUtils.GetBooleanEnvironmentVariableValue(
					Model.EnvironmentVariableNoModelSchemaCache);

			var masterDatabaseWorkspaceContext = GetMasterDatabaseWorkspaceContext(model);

			if (! noModelSchemaCache && masterDatabaseWorkspaceContext != null)
			{
				WorkspaceUtils.EnableSchemaCache(masterDatabaseWorkspaceContext.Workspace);
			}

			//if (!noModelSchemaCache && MasterDatabaseWorkspaceContext != null)
			//{
			//	WorkspaceUtils.EnableSchemaCache(MasterDatabaseWorkspaceContext.Workspace);
			//}
		}

		public static void DisableSchemaCache(this Model model) // TODO move to ModelUtils?
		{
			var masterDatabaseWorkspaceContext = GetMasterDatabaseWorkspaceContext(model);

			// schema cache might have been discarded
			if (masterDatabaseWorkspaceContext != null)
			{
				// workspace already cached, make sure it has schema cache disabled
				WorkspaceUtils.DisableSchemaCache(masterDatabaseWorkspaceContext.Workspace);
			}

			//// schema cache might have been discarded
			//if (MasterDatabaseWorkspaceContext != null)
			//{
			//	// workspace already cached, make sure it has schema cache disabled
			//	WorkspaceUtils.DisableSchemaCache(MasterDatabaseWorkspaceContext.Workspace);
			//}
		}

		#endregion

		private static IModelMasterDatabase GetMasterDatabase(Model model)
		{
			return model as IModelMasterDatabase ??
			       throw new InvalidOperationException(
				       $"Model is not {nameof(IModelMasterDatabase)}");
		}
	}

	public static class ModelUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static string QualifyModelElementName(
			Model model, string modelElementName)
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
			[NotNull] Model model, [NotNull] string masterDatabaseDatasetName)
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

		public static IWorkspaceContext CreateDefaultMasterDatabaseWorkspaceContext(Model model)
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
						Model.EnvironmentVariableNoModelSchemaCache);

				if (! noModelSchemaCache)
				{
					WorkspaceUtils.EnableSchemaCache(result.Workspace);
				}
			}

			return result;
		}
	}
}
