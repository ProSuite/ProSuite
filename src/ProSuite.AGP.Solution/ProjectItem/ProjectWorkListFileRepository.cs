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
	public class ProjectWorkListFileRepository : ProSuiteProjectFileRepository
	{
		private static ProjectWorkListFileRepository _repository;
		public static ProjectWorkListFileRepository Current => _repository ?? (_repository = new ProjectWorkListFileRepository());

		public ProjectWorkListFileRepository() : base()
		{
		}

		protected override string FolderName { get; set; } = "WorkLists";
		protected override string FileExtension { get; set; } = "wklist";
		protected override string ItemName { get; set; } = "WorkList item";
		protected override string ProjectName { get; set; } = "ProSuiteItem_ProjectItem";
		protected override ProjectItemType Type { get; set; } = ProjectItemType.WorkListDefinition;

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
