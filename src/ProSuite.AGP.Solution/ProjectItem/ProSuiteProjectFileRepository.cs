using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Desktop.Internal.Core;

namespace ProSuite.AGP.Solution.ProjectItem
{
	public enum ProjectItemType
	{
		WorkListDefinition,
		Configuration,
		None
	}

	public abstract class ProSuiteProjectFileRepository 
	{
		Msg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly string _containerName = "ProSuiteContainer";

		protected abstract string FolderName { get; set; }

		protected abstract string FileExtension { get; set; }

		protected abstract string ItemName { get; set; }

		protected abstract string ProjectName { get; set; }

		protected abstract ProjectItemType Type { get; set; }

		public Project Project { get; set; }

		protected ProSuiteProjectFileRepository()
		{
		}

		public virtual IEnumerable<string> GetAll()
		{
			return new List<string>();
		}

		public virtual void Add(string path)
		{
			AddProjectItemToProject(path);
		}

		public virtual bool AddProjectItemToProject(string path)
		{
			QueuedTask.Run(() =>
			{
				var itemFolder = GetProjectItemFolderPath();
				if (!String.IsNullOrEmpty(itemFolder))
				{
					_msg.Info($"Copy file {path}");
					File.Copy(path,
							  Path.Combine(itemFolder, Path.GetFileName(path)),true);
				}
			});
			return true;
		}

		#region private functions

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

