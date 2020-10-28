using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.ProTrials.CartoProcess
{
	public class AlignMarkers : CartoProcess
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public override string Name => nameof(AlignMarkers);

		public ProcessDatasetName InputDataset { get; set; }
		public IList<ProcessDatasetName> ReferenceDatasets { get; set; }
		public double SearchDistance { get; set; }
		public string MarkerAttributes { get; set; }

		public override bool Validate(CartoProcessConfig config)
		{
			return true;
		}

		public override void Initialize(CartoProcessConfig config)
		{
			Assert.ArgumentNotNull(config);

			InputDataset = config.GetValue<ProcessDatasetName>(nameof(InputDataset))
			               ?? throw ConfigError("Required parameter {0} is missing", nameof(InputDataset));

			ReferenceDatasets = new List<ProcessDatasetName>();
			foreach (var dn in config.GetValues<ProcessDatasetName>(nameof(ReferenceDatasets)))
			{
				ReferenceDatasets.Add(dn);
			}

			if (ReferenceDatasets.Count < 1)
				throw ConfigError("At least one reference dataset is required");

			SearchDistance = config.GetValue<double>(nameof(SearchDistance));
			MarkerAttributes = config.GetValue<string>(nameof(MarkerAttributes));

			// Note: would provide utilities in base class for the operations above
			// Note: parameter name for lists: Dataset (sg) or Datasets (pl)?
		}

		[StringFormatMethod("format")]
		protected static Exception ConfigError(string format, params object[] args)
		{
			return new Exception(string.Format(format, args));
		}

		public override IEnumerable<ProcessDatasetName> GetOriginDatasets()
		{
			yield return InputDataset;
		}

		public override IEnumerable<ProcessDatasetName> GetDerivedDatasets()
		{
			yield break; // no derived datasets
		}

		public override bool CanExecute(IProcessingContext context)
		{
			return context.CanExecute(this);
		}

		public override void Execute(IProcessingContext context, IProcessingFeedback feedback)
		{
			using (var engine = new AlignMarkersEngine(this, context, feedback))
			{
				engine.Execute();

				// TODO engine.ReportProcessComplete("{0} features aligned", engine.FeaturesAligned);
			}
		}

		private class AlignMarkersEngine : CartoProcessEngineBase
		{
			private static readonly IMsg _msg = Msg.ForCurrentClass();

			private readonly ProcessingDataset _inputDataset;
			private readonly IList<ProcessingDataset> _referenceDatasets;
			private readonly double _searchDistance;
			private readonly object _markerFieldSetter; // TODO

			public AlignMarkersEngine(AlignMarkers config, IProcessingContext context, IProcessingFeedback feedback)
				: base(context, feedback)
			{
				_inputDataset = null;
			}

			public override void Execute()
			{
				_msg.Info($"Hello from {nameof(AlignMarkersEngine)}");
			}
		}
	}
}
