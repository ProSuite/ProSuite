using ProSuite.AGP.QA.WorkList;
using ProSuite.Application.Configuration;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class IssueWorkListEnvironment : IssueWorkListEnvironmentBase
	{
		private readonly string _templateLayer = "Selection Work List.lyrx";

		public IssueWorkListEnvironment([CanBeNull] string path) : base(path) { }

		protected override string GetWorkListSymbologyTemplateLayerPath()
		{
			return ConfigurationUtils.GetConfigFilePath(_templateLayer);
		}
	}
}
