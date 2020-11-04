using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core
{
	public interface ITestImplementationInfo
	{
		IList<TestParameter> Parameters { get; }

		string[] TestCategories { get; }

		TestParameter GetParameter([NotNull] string parameterName);

		string GetTestDescription();
	}
}
