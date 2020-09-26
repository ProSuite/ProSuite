using System;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Logging;
using System.IO;
using System.Linq;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.Xml;

namespace ProSuite.AGP.Solution.ProjectItem
{
	public enum ProjectItemType
	{
		WorkListDefinition,
		Configuration
	}
	public class ProSuiteProjectItemManager
	{
		// TODO algr: IRepository<T> interface for Project? Save, Add, ...

		private readonly string _containerName = "ProSuiteContainer";

		private readonly string _workListFolderName = "WorkLists";
		private readonly string _configurationFolderName = "";

		private static ProSuiteProjectItemManager _manager;
		public static ProSuiteProjectItemManager Current => _manager ?? (_manager = new ProSuiteProjectItemManager());

		Msg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// TODO tmp

		private string GetProjectSubFolder(Project currentProject, string subFolder)
		{
			var issuesPath = Path.Combine(currentProject.HomeFolderPath, subFolder);
			Directory.CreateDirectory(issuesPath);
			return issuesPath;
		}

		public bool AddProjectItemFileToProject(Project currentProject, string itemPath)
		{
			var result = false;

			var subFolder = GetProjectSubFolder(currentProject, _workListFolderName);

			// do we have container?
			QueuedTask.Run(() =>
			{
				var container = currentProject.GetProjectItemContainer("ProSuiteContainer");
				_msg.Info($"ProjectItems are {container.GetItems().Count()}");

				bool folderItemPresent = false;
				foreach (var containerItem in container.GetItems())
				{
					if (containerItem is FolderConnectionProjectItem)
					{
						folderItemPresent = true;
						break;
					}
				}

				if (!folderItemPresent)
				{
					var folderItem = new ProSuiteProjectItem(
						"WorkList item",
						subFolder,
						"ProSuiteItem_ProjectItem", //item.TypeID,
						"ProSuiteItem_FolderContainer"); //container.TypeID);

					var added = currentProject.AddItem(folderItem);
					_msg.Info($"Project item folder added {added}");
				}

				File.Copy(itemPath, Path.Combine(subFolder, Path.GetFileName(itemPath)));
				currentProject.SetDirty(); //enable save
			});
			return result;
		}

		public bool SaveWorkListDefinitionInProject(Project currentProject, XmlWorkListDefinition definition)
		{
			// serialize to tmp
			var tmpPath = Path.GetTempFileName();
			var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
			helper.SaveToFile(definition, tmpPath);

			// add file to project
			AddProjectItemFileToProject(currentProject, tmpPath);
			return true;
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
			Directory.CreateDirectory(itemFullPath);

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
					var folderItem = new ProSuiteProjectItem(
						"WorkList item",
						itemFolder,
						"ProSuiteItem_ProjectItem",
						container.TypeID);

					folderItemPresent = project.AddItem(folderItem);
					_msg.Info($"Project item folder added {folderItemPresent}");
					if (folderItemPresent)
						project.SetDirty(); //enable save
				}
			});
			return folderItemPresent ? itemFullPath : null;
		}

		#endregion
	}

}
