using System.Collections.Generic;
using ProSuite.Processing.Domain;

namespace ProSuite.Processing.AGP.Core.Domain
{
	// TODO This should really be in ProSuite.Processing.Domain, but first have to fix dependency challenges
	public interface ICartoProcess
	{
		string Name { get; }

		bool Validate(CartoProcessConfig config);

		void Initialize(CartoProcessConfig config);

		IEnumerable<ProcessDatasetName> GetOriginDatasets();

		IEnumerable<ProcessDatasetName> GetDerivedDatasets();

		bool CanExecute(IProcessingContext context);

		void Execute(IProcessingContext context, IProcessingFeedback feedback);
	}
}
