using System.Collections.Generic;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Workflow
{
	/// <summary>
	/// Base interface for a project workspace, i.e. the abstraction of the datasets loaded in a
	/// specific datastore / workspace in the context of a DDX project and its settings.
	/// </summary>
	public interface IProjectWorkspace
	{
		int ProjectId { get; }

		string ProjectName { get; }

		IProjectSettings ProjectSettings { get; }

		/// <summary>
		/// The currently relevant datasets of the project that are available in this
		/// project workspace.
		/// </summary>
		IList<IDdxDataset> Datasets { get; }

		/// <summary>
		/// The connection string of the datastore / workspace.
		/// </summary>
		string DatastoreConnectionString { get; }

		string DisplayName { get; }
	}
}
