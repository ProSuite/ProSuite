using ArcGIS.Desktop.Core;
using ESRI.ArcGIS.ItemIndex;
using ProSuite.Commons.AGP;
using ProSuite.Commons.QA.ServiceManager.Types;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Clients.AGP.ProSuiteSolution.ProjectItem
{
    public class ProSuiteQAProjectItem : CustomProjectItemBase
    {
		public ProSuiteQAProjectItem() : base()
		{
			Path = @"c:/data/issues.wkl";
			this._pathSaveRelative = true;

			//ServerConfigurations = new List<ProSuiteQAServerConfiguration>()
			//{
			//	GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPLocal),
			//	GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPService)
			//};
			//SpecificationConfiguration = new ProSuiteQASpecificationsConfiguration();

		}
		public ProSuiteQAProjectItem(ItemInfoValue iiv) : base(FlipBrowseDialogOnly(iiv))
		{
			_pathSaveRelative = true;
		}

		/// <summary>
		/// This constructor is called if the project item was saved into the project.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="catalogPath"></param>
		/// <param name="typeID"></param>
		/// <param name="containerTypeID"></param>
		/// <remarks>Custom project items cannot <b>not</b> be saved into the project if
		/// the user clicks (or executes) save</remarks>
		public ProSuiteQAProjectItem(string name, string catalogPath, string typeID, string containerTypeID) :
		  base(name, catalogPath, typeID, containerTypeID)
		{
			_pathSaveRelative = true;
		}

		public override void Fetch()
		{
			//This is where the QA worklist is located
			string filePath = this.Path;
			// xml?
			//XDocument doc = XDocument.Load(filePath);
		}


		//public ProSuiteQAProjectItem()
		//{
		//	ServerConfigurations = new List<ProSuiteQAServerConfiguration>()
		//	{
		//		GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPLocal),
		//		GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType.GPService)
		//	};
		//	SpecificationConfiguration = new ProSuiteQASpecificationsConfiguration();
		//}

		public ProSuiteQAProjectItem(IEnumerable<ProSuiteQAServerConfiguration> serverConfigurations, ProSuiteQASpecificationsConfiguration specifationConfiguration)
		{
			SpecificationConfiguration = specifationConfiguration;
			ServerConfigurations = serverConfigurations;
		}

		public ProSuiteQASpecificationsConfiguration SpecificationConfiguration { get; set; }

		public IEnumerable<ProSuiteQAServerConfiguration> ServerConfigurations { get; set; }

		public override ProjectItemInfo OnGetInfo()
		{
			var projectItemInfo = new ProjectItemInfo
			{
				Name = "QAProjectItem",//this.Name,
				Path = this.Path,
				Type = ProSuiteProjectItemContainer.ContainerName
			};
			return projectItemInfo;
		}

		private static ItemInfoValue FlipBrowseDialogOnly(ItemInfoValue iiv)
		{
			iiv.browseDialogOnly = "FALSE";
			return iiv;
		}

		private ProSuiteQAServerConfiguration GetDefaultQAGPServiceConfiguration(ProSuiteQAServiceType serviceType)
		{
			switch (serviceType)
			{
				case ProSuiteQAServiceType.GPLocal:
					return new ProSuiteQAServerConfiguration()
					{
						ServiceType = ProSuiteQAServiceType.GPLocal,
						ServiceName = @"QAGPLocal",
						//ServiceConnection = @"c:\git\PRD_ProSuite\py_esrich_prosuite_qa_gpservice\ArcGISPro\ProSuiteToolbox.pyt"
						ServiceConnection = ""
					};

				case ProSuiteQAServiceType.GPService:
					return new ProSuiteQAServerConfiguration()
					{
						ServiceType = ProSuiteQAServiceType.GPService,
						ServiceName = @"QAGPServices\ProSuiteQAService",
						//ServiceConnection = @"C:\Users\algr\Documents\ArcGIS\Projects\test\admin on vsdev2414.esri-de.com_6443 (3).ags"
						ServiceConnection = ""
					};
				default:
					return new ProSuiteQAServerConfiguration();
			}
		}

		#region Icon override

		/// <summary>
		/// Gets whether the project item can contain child items
		/// </summary>
		public override bool IsContainer => true;

		public override ImageSource LargeImage
		{
			get
			{
				return ImageUtils.GetImageSource(@"Images/FolderWithGISData32.png");
			}
		}

		public override Task<ImageSource> SmallImage
		{
			get
			{
				return Task.FromResult((ImageSource)ImageUtils.GetImageSource(@"Images/FolderWithGISData16.png"));
			}
		}

		#endregion Icon override
	}

}
