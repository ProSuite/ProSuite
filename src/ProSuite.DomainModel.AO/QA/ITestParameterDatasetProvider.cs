using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.QA
{
	public interface ITestParameterDatasetProvider
	{
		/// <summary>
		/// Get the datasets corresponding to the bitmap of validTypes
		/// </summary>
		/// <param name="validTypes">bitmap of valid types</param>
		/// <param name="model"></param>
		/// <returns>Filtered datasets</returns>
		[NotNull]
		IEnumerable<Dataset> GetDatasets(TestParameterType validTypes,
		                                 [CanBeNull] DdxModel model);
	}
}