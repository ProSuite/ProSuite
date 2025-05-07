using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.Core.DataModel;

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
		public static IWorkspaceContext GetMasterDatabaseWorkspaceContext(this DdxModel model)
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
		/// if <see cref="DdxModel.AutoEnableSchemaCache"/> is set to <c>true</c>.
		/// </summary>
		[CanBeNull]
		public static IWorkspace GetMasterDatabaseWorkspace(this DdxModel model)
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

		public static bool IsMasterDatabaseAccessible(this DdxModel model)
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

		public static string GetMasterDatabaseNoAccessReason(this DdxModel model)
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
			this DdxModel model, out IWorkspaceContext result, out string noAccessReason)
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

		[NotNull]
		public static IWorkspaceContext AssertMasterDatabaseWorkspaceContextAccessible(
			this DdxModel model)
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

		public static void EnableSchemaCache(this DdxModel model) // TODO move to ModelUtils?
		{
			// schema cache might have been discarded (e.g. Reconcile does this)

			// Note:
			// The model Schema Cache can be turned OFF by environment variable.
			// This is experimental and used while analysing memory consumption.
			bool noModelSchemaCache =
				EnvironmentUtils.GetBooleanEnvironmentVariableValue(
					DdxModel.EnvironmentVariableNoModelSchemaCache);

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

		public static void DisableSchemaCache(this DdxModel model) // TODO move to ModelUtils?
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

		private static bool DetermineMasterDatabaseWorkspaceAccessibility(this DdxModel model)
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

		private static IModelMasterDatabase GetMasterDatabase(DdxModel model)
		{
			return model as IModelMasterDatabase ??
			       throw new InvalidOperationException(
				       $"Model is not {nameof(IModelMasterDatabase)}");
		}
	}
}
