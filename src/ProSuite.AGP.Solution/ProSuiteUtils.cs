using System.Threading.Tasks;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution
{
	public static class ProSuiteUtils
	{
		private static IProSuiteFacade _facade;
		public static IProSuiteFacade Facade => _facade ?? (_facade = new ProSuiteImpl());

		public static Task OpenIssueWorkListAsync(string issuesGdbPath = null)
		{
			return Facade.OpenIssueWorkListAsync(issuesGdbPath);
		}

		public static Task OpenSelectionWorkListAsync()
		{
			return Facade.OpenSelectionWorkListAsync();
		}

		public static Task CreateWorkListAsync([NotNull] WorkEnvironmentBase environment)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));

			return ProSuiteImpl.CreateWorkList(environment);
		}

		public static Task OpenWorkListAsync([NotNull] WorkEnvironmentBase environment,
		                                     [NotNull] string path)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			return ProSuiteImpl.OpenWorkList(environment, path);
		}
	}
}
