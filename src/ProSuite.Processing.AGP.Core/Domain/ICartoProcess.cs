using System.Collections.Generic;
using ProSuite.DomainModel.Core.Processing;
using ProcessDatasetName = ProSuite.Processing.Domain.ProcessDatasetName;

namespace ProSuite.Processing.AGP.Core.Domain
{
	// TODO This should really be in ProSuite.Processing.Domain, but first have to fix dependency challenges
	public interface ICartoProcess
	{
		string Name { get; }

		string Description { get; }

		IEnumerable<CartoProcessParam> Parameters { get; }

		bool Validate(CartoProcessConfig config, out string message);

		void Initialize(CartoProcessConfig config); // TODO rename Configure()?

		IEnumerable<ProcessDatasetName> GetOriginDatasets();

		IEnumerable<ProcessDatasetName> GetDerivedDatasets();

		IEnumerable<ProcessDatasetName> GetAuxiliaryDatasets();

		bool CanExecute(IProcessingContext context);

		void Execute(IProcessingContext context, IProcessingFeedback feedback);
	}
}
