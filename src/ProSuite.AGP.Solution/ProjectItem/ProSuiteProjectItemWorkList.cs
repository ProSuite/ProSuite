using System.Threading.Tasks;
using System.Windows.Media;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.Solution.Commons;

namespace ProSuite.AGP.Solution.ProjectItem
{
	// TODO ProSuiteDataSubItem for different types of data?
	internal class ProSuiteProjectItemWorkList : CustomItemBase
	{
		private const string DefaultDisplayType = "WorkList";
		private string _displayType;

		public ProSuiteProjectItemWorkList(string name, string path, string type,
		                                   string lastModifiedTime) : base(
			name, path, type, lastModifiedTime)
		{
			_displayType = DefaultDisplayType;
		}

		protected override string DisplayType
		{
			get => _displayType ?? DefaultDisplayType;
			set => _displayType = value;
		}

		public override ImageSource LargeImage =>
			ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset32.png");

		public override Task<ImageSource> SmallImage =>
			Task.FromResult(ImageUtils.GetImageSource(@"GeodatabaseFeatureDataset16.png"));

		public override bool IsContainer => false;
	}
}
