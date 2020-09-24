using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Internal.Catalog.DatabaseTools.CopyToDatabase.Views;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.ProjectItem
{
	public class ProSuiteProjectItemManager
	{
		private static ProSuiteProjectItemManager _manager;
		private readonly string _workListFolderName = "WorkLists";

		public static ProSuiteProjectItemManager Current => _manager ?? (_manager = new ProSuiteProjectItemManager());

		Msg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private string GetProjectSubFolder(Project currentProject, string subFolder)
		{
			var issuesPath = Path.Combine(currentProject.HomeFolderPath, subFolder);
			Directory.CreateDirectory(issuesPath);
			return issuesPath;
		}

		public bool SaveProjectItem(Project currentProject, string itemPath)
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

		// TODO algr: temp solution
		//var issuesPath = Path.Combine(Project.Current.HomeFolderPath, "WorkLists");
		//Directory.CreateDirectory(issuesPath);
		//_qaProjectItem = new ProSuiteProjectItem(issuesPath, QAConfiguration.Current.DefaultQAServiceConfig, QAConfiguration.Current.DefaultQASpecConfig);
		//QueuedTask.Run(() =>
		//{
		//	var added = Project.Current.AddItem(_qaProjectItem);
		//	_msg.Info($"Project item added {added}");

		//	Project.Current.SetDirty();//enable save
		//});

		//QueuedTask.Run(() =>
		//{
		//	var container = Project.Current.GetProjectItemContainer("ProSuiteContainer");
		//	foreach ( var item in container.GetItems())
		//	{
		//		var p = item.PhysicalPath;
		//	}
		//});
		//Project.Current.SaveAsync();
	}
}
