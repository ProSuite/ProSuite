using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.Workflow
{
	public abstract class ProjectWorkspaceBase<P, M>
		where P : Project<M>
		where M : ProductionModel
	{
		[NotNull] private readonly List<Dataset> _datasets = new List<Dataset>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectWorkspaceBase&lt;P, M&gt;"/> class.
		/// </summary>
		/// <param name="project">The project.</param>
		/// <param name="workspace">The workspace.</param>
		protected ProjectWorkspaceBase([NotNull] P project,
		                               [NotNull] IWorkspace workspace)
		{
			Assert.ArgumentNotNull(project, nameof(project));
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			Project = project;
			Workspace = workspace;
		}

		[NotNull]
		public P Project { get; }

		[NotNull]
		public IWorkspace Workspace { get; }

		[NotNull]
		public IList<Dataset> Datasets => _datasets;

		public void Add([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			if (! _datasets.Contains(dataset))
			{
				_datasets.Add(dataset);
			}
		}
	}
}
