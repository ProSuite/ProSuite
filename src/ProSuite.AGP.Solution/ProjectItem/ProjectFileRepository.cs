using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace ProSuite.AGP.Solution.ProjectItem
{
	public enum ProjectItemType
	{
		WorkListDefinition,
		Configuration,
		None
	}

	public class ProjectFileRepository 
	{
		Msg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly string _containerName = "ProSuiteContainer";

		protected string FolderName { get; set; } = "";

		protected string FileExtension { get; set; } = "xml";

		protected string ItemName { get; set; } = "Project item";

		protected string ProjectName = "ProSuiteItem_ProjectItem";

		protected ProjectItemType Type { get; set; } = ProjectItemType.None;

		protected Project Project { get; }

		public ProjectFileRepository([NotNull] Project project)
		{
			Project = project;
		}

		public virtual IEnumerable<string> GetAll()
		{
			return new List<string>();
		}

		public virtual void Add([NotNull] string path)
		{
			AddProjectItemToProject(path);
		}

		public virtual void Delete([NotNull] string path)
		{
			File.Delete(path);
		}

		#region private functions

		private void AddProjectItemToProject([NotNull] string path)
		{
			QueuedTask.Run(() =>
			{
				var itemFolder = GetProjectItemFolderPath();
				if (!String.IsNullOrEmpty(itemFolder))
				{
					_msg.Info($"Add file {path}");
					//File.Copy(path,
					//		  Path.Combine(itemFolder, Path.GetFileName(path)),true);
				}
			});
		}

		private string GetProjectItemFolderPath()
		{
			var itemFullPath = Path.Combine(Project.HomeFolderPath, FolderName);

			bool folderItemPresent = false;
			var container = Project.GetProjectItemContainer(_containerName);
			foreach (var containerItem in container.GetItems())
			{
				if (containerItem is FolderConnectionProjectItem)
				{
					if (containerItem.Name == FolderName)
					{
						folderItemPresent = true;
						break;
					}
				}
			}

			if (!folderItemPresent)
			{
				var folderItem = new ProSuiteProjectItem(
					ItemName,
					itemFullPath,
					ProjectName,
					container.TypeID);

				folderItemPresent = Project.AddItem(folderItem);
				_msg.Info($"Project item folder added {folderItemPresent}");
				if (folderItemPresent)
				{
					Directory.CreateDirectory(itemFullPath);
					Project.SetDirty(); //enable save
				}
			}
			return folderItemPresent ? itemFullPath : null;
		}

		#endregion

	}

}

