using System;
using System.Collections.Generic;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.ProTrials.CartoProcess
{
	public class CreateAnnoMasks : CartoProcess
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public override string Name => nameof(CreateAnnoMasks);

		public override bool Validate(CartoProcessConfig config)
		{
			throw new NotImplementedException();
		}

		public override void Initialize(CartoProcessConfig config)
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<ProcessDatasetName> GetOriginDatasets()
		{
			throw new NotImplementedException();
		}

		public override IEnumerable<ProcessDatasetName> GetDerivedDatasets()
		{
			throw new NotImplementedException();
		}

		public override bool CanExecute(IProcessingContext context)
		{
			throw new NotImplementedException();
		}

		public override void Execute(IProcessingContext context, IProcessingFeedback feedback)
		{
			throw new NotImplementedException();
		}
	}
}
