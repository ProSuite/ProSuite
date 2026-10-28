using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA.VerificationProgress;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.AGP.QA.ProPlugins
{
	/// <summary>
	/// Standard (project-workspace based) <see cref="IVerificationCommandContext"/>. No
	/// application-specific preflight checks are required.
	/// </summary>
	public class ProjectWorkspaceVerificationCommandContext : IVerificationCommandContext
	{
		[NotNull] private readonly IVerificationSessionContext _sessionContext;
		[NotNull] private readonly IWorkListOpener _workListOpener;

		[CanBeNull] private readonly
			Func<IQualityVerificationResult, ErrorDeletionInPerimeter, bool, Task<int>> _saveAction;

		public ProjectWorkspaceVerificationCommandContext(
			[NotNull] IVerificationSessionContext sessionContext,
			[NotNull] IWorkListOpener workListOpener,
			[CanBeNull] Func<IQualityVerificationResult, ErrorDeletionInPerimeter, bool, Task<int>>
				saveAction)
		{
			Assert.ArgumentNotNull(sessionContext, nameof(sessionContext));
			Assert.ArgumentNotNull(workListOpener, nameof(workListOpener));
			_sessionContext = sessionContext;
			_workListOpener = workListOpener;
			_saveAction = saveAction;
		}

		public IQualityVerificationEnvironment Environment =>
			_sessionContext.VerificationEnvironment;

		public IApplicationBackgroundVerificationController CreateController(
			MapView mapView, Geometry perimeter)
		{
			var workspace = (ProjectWorkspace) _sessionContext.ProjectWorkspace;
			return new AgpBackgroundVerificationController(
				_workListOpener, mapView, perimeter, workspace?.ModelSpatialReference,
				Environment, _saveAction);
		}

		public Task<bool> PrepareAsync(string title)
		{
			return Task.FromResult(true);
		}
	}
}
