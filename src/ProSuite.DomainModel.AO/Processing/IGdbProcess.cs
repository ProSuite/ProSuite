using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.Processing;

namespace ProSuite.DomainModel.AO.Processing
{
	public interface IGdbProcess
	{
		[NotNull]
		string Name { get; set; }

		[CanBeNull]
		string Description { get; set; }

		/// <summary>
		/// Indicates whether all source datasets have to be present for executing the GdbProcess.
		/// </summary>
		bool RequiresAllSourceDatasets { get; }

		/// <summary>
		/// Gets a list of the input feature datasets resp. layers.
		/// </summary>
		IEnumerable<ProcessDatasetName> GetSourceDatasets();

		/// <summary>
		/// Gets a list of the output feature datasets resp. layers.
		/// </summary>
		IEnumerable<ProcessDatasetName> GetDerivedDatasets();

		bool CanExecute([NotNull] IProcessingContext context);

		void Execute([NotNull] IProcessingContext context,
		             [NotNull] IProcessingFeedback feedback);
	}
}
