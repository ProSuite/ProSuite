using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using ArcGIS.Desktop.Core;
using Clients.AGP.ProSuiteSolution.Commons;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.QA.ServiceManager.Types;

namespace ProSuite.AGP.Solution.ProjectItem
{

	internal class ProSuiteProjectItem : CustomProjectItemBase
	{
		public ProSuiteQASpecificationsConfiguration SpecificationConfiguration { get; set; }
		public IEnumerable<ProSuiteQAServerConfiguration> ServerConfigurations { get; set; }

		protected ProSuiteProjectItem() : base()
		{
		}

		protected ProSuiteProjectItem(ItemInfoValue iiv) : base(FlipBrowseDialogOnly(iiv))
		{
		}

		public ProSuiteProjectItem(string path, IEnumerable<ProSuiteQAServerConfiguration> serverConfigurations, ProSuiteQASpecificationsConfiguration specifationConfiguration)
		{
			Path = path;
			SpecificationConfiguration = specifationConfiguration;
			ServerConfigurations = serverConfigurations;
		}

		private static ItemInfoValue FlipBrowseDialogOnly(ItemInfoValue iiv)
		{
			iiv.browseDialogOnly = "FALSE";
			return iiv;
		}

		//TODO: Overload for use in your container create item
		public ProSuiteProjectItem(string name, string catalogPath, string typeID, string containerTypeID) :
		  base(name, catalogPath, typeID, containerTypeID)
		{

		}

		public ProSuiteProjectItem Clone() => new ProSuiteProjectItem(this.Name, this.Path, this.TypeID, this.ContainerType);

		/// <summary>
		/// DTor
		/// </summary>
		~ProSuiteProjectItem()
		{
		}

		public override ImageSource LargeImage
		{
			get
			{
				return ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset32.png");
			}
		}

		public override Task<ImageSource> SmallImage
		{
			get
			{
				return Task.FromResult((ImageSource)ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset16.png"));
			}
		}

		public override ProjectItemInfo OnGetInfo()
		{
			var projectItemInfo = new ProjectItemInfo
			{
				Name = this.Name,
				Path = this.Path,
				Type = ProSuiteProjectItemContainer.ContainerName
			};

			return projectItemInfo;
		}

		public override bool IsContainer => true;

		//TODO: Fetch is required if <b>IsContainer</b> = <b>true</b>
		public override void Fetch()
		{
			// Retrieve your child items
			// child items must also derive from CustomItemBase
		}

	}

	// TODO ProSuiteDataSubItem for different types of data?

}
