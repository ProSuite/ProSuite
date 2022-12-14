using System.Collections.Generic;
using ProSuite.DomainModel.Core.Processing;
using ProSuite.Processing.AGP.Core.Domain;
using ProcessDatasetName = ProSuite.Processing.Domain.ProcessDatasetName;

namespace ProSuite.Processing.AGP.Core
{
	/// <summary>
	/// This Carto Process does nothing and always succeeds.
	/// May be used as a placeholder or as a sentinel.
	/// </summary>
	public class DummyCartoProcess : ICartoProcess
	{
		public string Name { get; private set; }
		public string Description { get; private set; }

		public IEnumerable<CartoProcessParam> Parameters
		{
			get { yield break; }
		}

		public void Initialize(CartoProcessConfig config)
		{
			Name = config.Name ?? nameof(DummyCartoProcess);
			Description = config.Description;
		}

		public IEnumerable<ProcessDatasetName> GetOriginDatasets()
		{
			yield break;
		}

		public IEnumerable<ProcessDatasetName> GetDerivedDatasets()
		{
			yield break;
		}

		public IEnumerable<ProcessDatasetName> GetAuxiliaryDatasets()
		{
			yield break;
		}

		public bool CanExecute(IProcessingContext context)
		{
			return true;
		}

		public void Execute(IProcessingContext context, IProcessingFeedback feedback)
		{
			// do nothing
		}
	}
}
