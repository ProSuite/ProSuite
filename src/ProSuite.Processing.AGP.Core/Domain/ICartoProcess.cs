using System.Collections.Generic;
using ProSuite.Processing.Domain;

namespace ProSuite.Processing.AGP.Core.Domain
{
	// TODO This should really be in ProSuite.Processing.Domain, but first have to fix dependency challenges
	public interface ICartoProcess
	{
		string Name { get; }

		string Description { get; }

		void Initialize(CartoProcessConfig config); // TODO rename Configure()?

		IEnumerable<ProcessDatasetName> GetOriginDatasets();

		IEnumerable<ProcessDatasetName> GetDerivedDatasets();

		IEnumerable<ProcessDatasetName> GetAuxiliaryDatasets();

		bool CanExecute(IProcessingContext context);

		void Execute(IProcessingContext context, IProcessingFeedback feedback);
	}

	public interface IGroupCartoProcess : ICartoProcess
	{
		// Just for tagging, no additional members
	}
}
