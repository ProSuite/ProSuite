using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core
{
	/// <summary>
	/// Provides implementation details of the parameterized instance created by this
	/// implementation, mainly for display purposes.
	/// </summary>
	public interface IInstanceInfo
	{
		IList<TestParameter> Parameters { get; }

		string[] TestCategories { get; }

		TestParameter GetParameter([NotNull] string parameterName);

		string GetTestDescription();
	}
}
