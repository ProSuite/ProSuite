using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.QA.WorkList;
using ProSuite.Application.Configuration;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class IssueWorkListEnvironment : IssueWorkListEnvironmentBase
	{
		private readonly string _templateLayer = "Selection Work List.lyrx";

		public IssueWorkListEnvironment([CanBeNull] string path) : base(path) { }

		protected override LayerDocument GetLayerDocumentCore()
		{
			string path = ConfigurationUtils.GetConfigFilePath(_templateLayer);

			return LayerUtils.CreateLayerDocument(path);
		}
	}
}
