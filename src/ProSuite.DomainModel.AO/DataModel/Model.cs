using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class Model : DdxModel
	{
		protected const string EnvironmentVariableNoModelSchemaCache =
			"PROSUITE_NO_MODEL_SCHEMA_CACHE";

		#region Fields

		private bool _keepDatasetLocks;

		[CanBeNull] private IWorkspaceContext _masterDatabaseWorkspaceContext;

		// private IWorkspaceProxy _workspaceProxy;
		private bool? _isMasterDatabaseAccessible;

		private string _lastMasterDatabaseAccessError;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Model"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected Model() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Model"/> class.
		/// </summary>
		/// <param name="name">The name of the model.</param>
		protected Model(string name) : base(name) { }

		#endregion

		[CanBeNull]
		public IWorkspaceContext MasterDatabaseWorkspaceContext
		{
			get
			{
				if (! IsMasterDatabaseAccessible)
				{
					_masterDatabaseWorkspaceContext = null;
				}
				else if (_masterDatabaseWorkspaceContext == null)
				{
					_masterDatabaseWorkspaceContext = CreateMasterDatabaseWorkspaceContext();
				}

				return _masterDatabaseWorkspaceContext;
			}
		}

		public bool IsMasterDatabaseAccessible
		{
			get
			{
				if (! _isMasterDatabaseAccessible.HasValue)
				{
					_isMasterDatabaseAccessible = DetermineMasterDatabaseWorkspaceAccessibility();
				}

				return _isMasterDatabaseAccessible.Value;
			}
		}

		public string MasterDatabaseNoAccessReason
		{
			get
			{
				if (UserConnectionProvider == null)
				{
					return "No user connection provider defined for model";
				}

				return _lastMasterDatabaseAccessError;
			}
		}

		public bool KeepDatasetLocks
		{
			get { return _keepDatasetLocks; }
			set
			{
				if (value == _keepDatasetLocks)
				{
					return;
				}

				// Value changed. Discard current workspace proxy
				_keepDatasetLocks = value;

				if (_masterDatabaseWorkspaceContext == null)
				{
					return;
				}

				_masterDatabaseWorkspaceContext = null;

				if (_keepDatasetLocks)
				{
					return;
				}

				// make sure any locks are released immediately
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		public bool TryGetMasterDatabaseWorkspaceContext(out IWorkspaceContext result,
		                                                 out string noAccessReason)
		{
			result = MasterDatabaseWorkspaceContext;

			if (result == null)
			{
				noAccessReason = MasterDatabaseNoAccessReason;
				return false;
			}

			noAccessReason = null;
			return true;
		}

		[NotNull]
		public IWorkspaceContext AssertMasterDatabaseWorkspaceContextAccessible()
		{
			if (! TryGetMasterDatabaseWorkspaceContext(out IWorkspaceContext workspaceContext,
			                                           out string noAccessReason))
			{
				throw new AssertionException(
					$"The master database of model {Name} is not accessible: {noAccessReason}");
			}

			return workspaceContext;
		}

		/// <summary>
		/// Opens the workspace, without building the schema cache even if <see cref="AutoEnableSchemaCache"/>
		/// is set to <c>true</c>.
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		public IWorkspace GetMasterDatabaseWorkspace()
		{
			if (_masterDatabaseWorkspaceContext != null)
			{
				return _masterDatabaseWorkspaceContext.Workspace;
			}

			if (UserConnectionProvider == null)
			{
				return null;
			}

			if (! IsMasterDatabaseAccessible)
			{
				return null;
			}

			return (IWorkspace) UserConnectionProvider.OpenWorkspace();
		}

		[NotNull]
		public string TranslateToModelElementName(
			[NotNull] string masterDatabaseDatasetName)
		{
			Assert.ArgumentNotNullOrEmpty(masterDatabaseDatasetName,
			                              nameof(masterDatabaseDatasetName));

			// the master database context does not support any prefix mappings etc.

			// translate query class name (if it is one) to table name
			string gdbDatasetName = ModelElementUtils.GetBaseTableName(
				masterDatabaseDatasetName, MasterDatabaseWorkspaceContext);

			return ElementNamesAreQualified
				       ? gdbDatasetName // expected to be qualified also
				       : ModelElementNameUtils.GetUnqualifiedName(gdbDatasetName);
		}

		public override string ToString()
		{
			return Name ?? "<no name>";
		}

		public override string QualifyModelElementName(string modelElementName)
		{
			IWorkspace workspace = GetMasterDatabaseWorkspace();

			if (workspace == null)
			{
				return modelElementName;
			}

			return DatasetUtils.QualifyTableName(workspace,
			                                     DefaultDatabaseName,
			                                     DefaultDatabaseSchemaOwner,
			                                     modelElementName);
		}

		#region Schema Cache Control

		[PublicAPI]
		public void EnableSchemaCache()
		{
			// schema cache might have been discarded (e.g. Reconcile does this)

			// Note:
			// The model Schema Cache can be turned OFF by environment variable.
			// This is experimental and used while analysing memory consumption.
			bool noModelSchemaCache =
				EnvironmentUtils.GetBooleanEnvironmentVariableValue(
					EnvironmentVariableNoModelSchemaCache);

			if (! noModelSchemaCache && MasterDatabaseWorkspaceContext != null)
			{
				WorkspaceUtils.EnableSchemaCache(MasterDatabaseWorkspaceContext.Workspace);
			}
		}

		[PublicAPI]
		public void DisableSchemaCache()
		{
			// schema cache might have been discarded
			if (MasterDatabaseWorkspaceContext != null)
			{
				// workspace already cached, make sure it has schema cache disabled
				WorkspaceUtils.DisableSchemaCache(MasterDatabaseWorkspaceContext.Workspace);
			}
		}

		#endregion

		#region Non-public members

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual SpatialReferenceDescriptor CreateDefaultSpatialReferenceDescriptor() // TODO Drop? No usages in Topgis
		{
			return null;
		}

		[PublicAPI]
		public bool DisableAutomaticSchemaCaching { get; set; }

		protected virtual bool AutoEnableSchemaCache => false;

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual IEnumerable<ConnectionProvider> GetConnectionProvidersCore()
		{
			return new List<ConnectionProvider>();
		}

		[NotNull]
		protected abstract IWorkspaceContext CreateMasterDatabaseWorkspaceContext();

		[NotNull]
		protected IWorkspaceContext CreateDefaultMasterDatabaseWorkspaceContext()
		{
			_msg.Debug("Opening default master database workspace context...");

			IFeatureWorkspace featureWorkspace = UserConnectionProvider.OpenWorkspace();

			var result = new MasterDatabaseWorkspaceContext(featureWorkspace, this);

			if (AutoEnableSchemaCache && ! DisableAutomaticSchemaCaching)
			{
				// The model schema cache can be turned OFF by environment variable.
				bool noModelSchemaCache =
					EnvironmentUtils.GetBooleanEnvironmentVariableValue(
						EnvironmentVariableNoModelSchemaCache);

				if (! noModelSchemaCache)
				{
					WorkspaceUtils.EnableSchemaCache(result.Workspace);
				}
			}

			return result;
		}

		private bool DetermineMasterDatabaseWorkspaceAccessibility()
		{
			if (UserConnectionProvider == null)
			{
				return false;
			}

			try
			{
				// try to open the workspace
				UserConnectionProvider.OpenWorkspace();

				return true;
			}
			catch (Exception e)
			{
				_msg.DebugFormat("Error opening master database for model {0}: {1}",
				                 Name, e.Message);

				_lastMasterDatabaseAccessError = e.Message;

				return false;
			}
		}

		#endregion
	}
}
