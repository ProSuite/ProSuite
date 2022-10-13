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
		string TestDescription { get; }

		string[] TestCategories { get; }

		IList<TestParameter> Parameters { get; }

		TestParameter GetParameter([NotNull] string parameterName);

		string GetParameterDescription([NotNull] string parameterName);

		//Notification GetValidation();
	}
}
