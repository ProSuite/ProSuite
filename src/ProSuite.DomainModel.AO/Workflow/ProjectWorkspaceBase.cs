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

		[NotNull] private readonly Dictionary<Dataset, string> _gdbDatasetNamesByDataset =
			new Dictionary<Dataset, string>();

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

		public bool IsModelMasterDatabase =>
			ModelContextUtils.IsModelDefaultDatabase(Workspace, Project.ProductionModel);

		[NotNull]
		public IList<Dataset> Datasets => _datasets;

		public void Add([NotNull] Dataset dataset, [CanBeNull] string gdbDatasetName = null)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			if (! _datasets.Contains(dataset))
			{
				_datasets.Add(dataset);
			}

			// The name of the table in the (live) workspace can differ from the model
			// dataset name (e.g. feature service "L0..." prefixes, or any child-database
			// name transformation). Retain it so the client can match a live table to a
			// dataset by its gdb name. See ProjectWorkspaceMsg.gdb_dataset_names.
			if (gdbDatasetName != null)
			{
				_gdbDatasetNamesByDataset[dataset] = gdbDatasetName;
			}
		}

		/// <summary>
		/// Gets the name of the dataset's table in the (live) workspace, if it was
		/// captured. May differ from the model dataset name. Returns null when unknown.
		/// </summary>
		[CanBeNull]
		public string GetGdbDatasetName([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return _gdbDatasetNamesByDataset.TryGetValue(dataset, out string gdbDatasetName)
				       ? gdbDatasetName
				       : null;
		}
	}
}
