using System.Collections.Generic;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface IDataset
	{
		string Name { get; }

		IName FullName { get; }

		string BrowseName { get; set; }

		esriDatasetType Type { get; }

		string Category { get; }

		IEnumerable<IDataset> Subsets { get; }

		IWorkspace Workspace { get; }

		//IPropertySet PropertySet { get; }

		bool CanCopy();

		//IDataset Copy(string copyName, IWorkspace copyWorkspace);

		bool CanDelete();

		void Delete();

		bool CanRename();

		void Rename(string Name);

		object NativeImplementation { get; }
	}
}
