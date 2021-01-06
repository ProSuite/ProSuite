using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface ITestDescriptorRepository : IRepository<TestDescriptor>
	{
		TestDescriptor Get([NotNull] string name);

		TestDescriptor GetWithSameImplementation([NotNull] TestDescriptor testDescriptor);

		/// <summary>
		/// Gets the count of quality conditions that are based on a test descriptor,
		/// as a map TestDescriptor.Id -> number of referencing quality conditions, for all test 
		/// descriptors.
		/// Unreferenced test descriptors are not contained in the map -> implied count is 0.
		/// </summary>
		/// <returns>dictionary [test descriptor id] -> [number of referencing quality conditions]</returns>
		IDictionary<int, int> GetReferencingQualityConditionCount();
	}
}
