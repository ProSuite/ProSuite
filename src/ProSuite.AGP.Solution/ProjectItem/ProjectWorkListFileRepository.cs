using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;

namespace ProSuite.AGP.Solution.ProjectItem
{
	public class ProjectWorkListFileRepository : ProjectFileRepository
	{
		public ProjectWorkListFileRepository(Project project) : base(project)
		{
			FolderName = "WorkLists";
			FileExtension = "wklist";
			ItemName = "WorkList item";
			Type = ProjectItemType.WorkListDefinition;
		}

		// TODO algr: different icons and context menus?
		public override IEnumerable<string> GetAll()
		{
			var itemFolder = Project.GetItems<FolderConnectionProjectItem>().FirstOrDefault(p => p.Name == FolderName);
			if (itemFolder == null) return new List<string>();

			return Directory.GetFiles(itemFolder.Path, $"*.{FileExtension}");
		}

		// save and add?
		public override void Add(string path)
		{
			// serialize to tmp
			//var tmpPath = Path.GetTempFileName();
			//var helper = new XmlSerializationHelper<XmlWorkListDefinition>();
			//helper.SaveToFile(definition, tmpPath);

			//// add file to project
			//Add(tmpPath);

			base.Add(path);
		}
	}

}
