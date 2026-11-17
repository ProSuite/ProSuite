using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class VerifySelectionCmdBase : ButtonCommandBase
	{
		protected VerifySelectionCmdBase()
		{
			Register();
		}

		private void Register()
		{
			VerificationPlugInController.GetInstance(SessionContext).Register(this);
		}

		protected abstract IVerificationSessionContext SessionContext { get; }

		protected abstract IWorkListOpener WorkListOpener { get; }

		protected virtual
			Func<IQualityVerificationResult, ErrorDeletionInPerimeter, bool, Task<int>>
			SaveAction => null;

		protected override Task<bool> OnClickAsyncCore()
		{
			return VerifyUtils.CreateCommandRunner(SessionContext, WorkListOpener, SaveAction)
			                  .VerifySelection(GetRelevantSelection);
		}

		private Task<IList<Row>> GetRelevantSelection(MapView mapView)
		{
			// Check if the selected feature is part of the project workspace:
			var projectWorkspace = (ProjectWorkspace) SessionContext.ProjectWorkspace;
			Datastore projectWorkspaceDatastore = projectWorkspace?.Datastore;

			if (projectWorkspaceDatastore == null)
			{
				throw new InvalidOperationException("No active project workspace");
			}

			var result = new List<Row>();
			bool anySelected = false;
			foreach (Feature selectedFeature in SelectionUtils.GetSelectedFeatures(mapView))
			{
				anySelected = true;
				Datastore featureDatastore = selectedFeature.GetTable().GetDatastore();

				if (WorkspaceUtils.IsSameDatastore(projectWorkspaceDatastore, featureDatastore))
				{
					result.Add(selectedFeature);
				}
			}

			if (result.Count == 0)
			{
				if (anySelected)
				{
					throw new InvalidOperationException(
						"The selection is not in the current project workspace");
				}

				throw new InvalidOperationException("No selection");
			}

			return Task.FromResult<IList<Row>>(result);
		}
	}
}
