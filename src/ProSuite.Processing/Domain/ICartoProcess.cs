using System.Collections.Generic;

namespace ProSuite.Processing.Domain
{
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