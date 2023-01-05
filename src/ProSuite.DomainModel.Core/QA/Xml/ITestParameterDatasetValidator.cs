using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	/// <summary>
	/// Dataset validator abstraction that allows for platform-specific validation.
	/// </summary>
	public interface ITestParameterDatasetValidator
	{
		bool IsValidForParameter([NotNull] Dataset dataset,
		                         [NotNull] TestParameter testParameter,
		                         [CanBeNull] out string message);
	}
}
