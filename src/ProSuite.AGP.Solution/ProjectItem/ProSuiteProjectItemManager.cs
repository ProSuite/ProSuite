using System;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Logging;
using System.IO;
using System.Linq;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;

namespace ProSuite.AGP.Solution.ProjectItem
{
	public enum ProjectItemType
	{
		WorkListDefinition,
		Configuration
	}
	public class ProSuiteProjectItemManager
	{
		private readonly string _containerName = "ProSuiteContainer";

		private readonly string _workListFolderName = "WorkLists";
		private readonly string _configurationFolderName = "";

		private static ProSuiteProjectItemManager _manager;
		public static ProSuiteProjectItemManager Current => _manager ?? (_manager = new ProSuiteProjectItemManager());

		Msg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// TODO algr: IRepository<T> interface for Project? Save, Add, ...
		public bool SaveWorkListDefinitionInProject(Project currentProject, XmlWorkListDefinition definition)
		{
			var result = false;

			// serialize to tmp

			// add file to project

			return result;
		}

		public bool AddFileToProject(string filePath, Project project, ProjectItemType fileType)
		{
			var itemFolder = GetProjectItemFolderPath(project, fileType);
			if (String.IsNullOrEmpty(itemFolder))
				return false; 

			File.Copy(filePath, Path.Combine(itemFolder, Path.GetFileName(filePath)));
			return true;
		}

		#region private functions
		private string GetProjectItemFolderName(ProjectItemType fileType)
		{
			string folderName = null;
			switch (fileType)
			{
				case ProjectItemType.WorkListDefinition:
					folderName = _workListFolderName;
					break;

				case ProjectItemType.Configuration:
					folderName = _configurationFolderName;
					break;

				default:
					return null;
			}
			return folderName;
		}

		private string GetProjectItemFolderPath(Project project, ProjectItemType projectType)
		{
			var itemFolder = GetProjectItemFolderName(projectType);
			if (String.IsNullOrEmpty(itemFolder)) return null; // wrong type

			var itemFullPath = Path.Combine(project.HomeFolderPath, itemFolder);
			bool folderItemPresent = false;
			QueuedTask.Run(() =>
			{
				var container = project.GetProjectItemContainer(_containerName);

				// TODO algr: refactor
				foreach (var containerItem in container.GetItems())
				{
					if (containerItem is FolderConnectionProjectItem)
					{
						if (containerItem.Name == itemFolder)
						{
							folderItemPresent = true;
							break;
						}
					}
				}

				if (!folderItemPresent)
				{
					// TODO algr: other project item types
					var folderItem = new ProSuiteProjectItemFile(
						"WorkList item",
						itemFolder,
						"ProSuiteItem_ProjectItemWorkListFile",
						container.TypeID);

					folderItemPresent = project.AddItem(folderItem);
					_msg.Info($"Project item folder added {folderItemPresent}");
					if (folderItemPresent)
					{
						Directory.CreateDirectory(itemFullPath);
						project.SetDirty(); //enable save
					}
				}
			});
			return folderItemPresent ? itemFullPath : null;
		}

		#endregion
	}

}
