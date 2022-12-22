using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
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

		/// <summary>
		/// Gets the transformer configurations for the specified model / types.
		/// </summary>
		/// <param name="validType"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		IEnumerable<TransformerConfiguration> GetTransformers(TestParameterType validType,
		                                                      [CanBeNull] DdxModel model);

		/// <summary>
		/// Exclude a specific transformer from the transformers result (to avoid circular references).
		/// </summary>
		/// <param name="transformer"></param>
		void Exclude(TransformerConfiguration transformer);
	}
}
