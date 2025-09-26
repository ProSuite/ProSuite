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
	public class VerifiedModel : DdxModel, IModelMasterDatabase
	{
		[NotNull] private readonly IMasterDatabaseWorkspaceContextFactory _workspaceContextFactory;

		public VerifiedModel(
			[NotNull] string name,
			[NotNull] IWorkspace workspace,
			[NotNull] IMasterDatabaseWorkspaceContextFactory workspaceContextFactory,
			[CanBeNull] string databaseName = null,
			[CanBeNull] string schemaOwner = null,
			SqlCaseSensitivity sqlCaseSensitivity = SqlCaseSensitivity.SameAsDatabase,
			int cloneId = -1)
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

			SetCloneId(cloneId);
		}

		#region Overrides of Model

		public override string QualifyModelElementName(string modelElementName)
		{
			return ModelUtils.QualifyModelElementName(this, modelElementName);
		}

		public override string TranslateToModelElementName(string masterDatabaseDatasetName)
		{
			return ModelUtils.TranslateToModelElementName(this, masterDatabaseDatasetName);
		}

		IWorkspaceContext IModelMasterDatabase.CreateMasterDatabaseWorkspaceContext()
		{
			IWorkspaceContext result = _workspaceContextFactory.Create(this);

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
