using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	[CLSCompliant(false)]
	public class VerifiedModel : Model
	{
		[NotNull]
		private readonly Func<Model, IFeatureWorkspace, IWorkspaceContext> _workspaceContextFactory;

		[CLSCompliant(false)]
		public VerifiedModel(
			[NotNull] string name,
			[NotNull] IWorkspace workspace,
			[NotNull] Func<Model, IFeatureWorkspace, IWorkspaceContext> workspaceContextFactory,
			[CanBeNull] string databaseName = null,
			[CanBeNull] string schemaOwner = null,
			SqlCaseSensitivity sqlCaseSensitivity =
				SqlCaseSensitivity.SameAsDatabase)
			: base(name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(workspaceContextFactory, nameof(workspaceContextFactory));

			_workspaceContextFactory = workspaceContextFactory;

			DefaultDatabaseName = databaseName;
			DefaultDatabaseSchemaOwner = schemaOwner;

			KeepDatasetLocks = true;

			UserConnectionProvider = new OpenWorkspaceConnectionProvider(workspace);
			ElementNamesAreQualified = WorkspaceUtils.UsesQualifiedDatasetNames(workspace);

			SqlCaseSensitivity = sqlCaseSensitivity;
			UseDefaultDatabaseOnlyForSchema = false;
		}

		#region Overrides of Model

		protected override IWorkspaceContext CreateMasterDatabaseWorkspaceContext()
		{
			IFeatureWorkspace featureWorkspace = UserConnectionProvider.OpenWorkspace();

			var result = _workspaceContextFactory(this, featureWorkspace);

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

		protected override void CheckAssignSpecialDatasetCore(Dataset dataset) { }

		#endregion
	}
}
