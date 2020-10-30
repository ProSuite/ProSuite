using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Processing.Evaluation;
using ProSuite.Processing.Utils;

namespace ProSuite.AGP.Solution.ProTrials.CartoProcess
{
	public class AlignMarkers : CartoProcess
	{
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

				engine.ReportProcessComplete("{0} features aligned", engine.FeaturesAligned);
			}
		}

		private class AlignMarkersEngine : CartoProcessEngineBase
		{
			private static readonly IMsg _msg = Msg.ForCurrentClass();

			private readonly ProcessingDataset _inputDataset;
			private readonly IList<ProcessingDataset> _referenceDatasets;
			private readonly double _searchDistance;
			private readonly FieldSetter _markerFieldSetter;

			public AlignMarkersEngine(AlignMarkers config, IProcessingContext context, IProcessingFeedback feedback)
				: base(config.Name, context, feedback)
			{
				_inputDataset =
					OpenRequiredDataset(config.InputDataset, nameof(config.InputDataset));

				_referenceDatasets = OpenDatasetList();

				_searchDistance = ProcessingUtils.Clip(
					config.SearchDistance, 0, double.MaxValue,
					nameof(config.SearchDistance));

				_markerFieldSetter = ProcessingUtils.CreateFieldSetter(
					config.MarkerAttributes, _inputDataset.FeatureClass,
					nameof(config.MarkerAttributes));
			}

			public int FeaturesAligned { get; private set; }

			public override void Execute()
			{
				TotalFeatures = CountFeatures(_inputDataset);
				if (TotalFeatures == 0)
				{
					return;
				}

				foreach (var feature in GetFeatures(_inputDataset))
				{
					CheckCancel();

					try
					{
						ReportStartFeature(feature);

						if (AllowModification(feature, out string reason))
						{
							ProcessFeature(feature);

							ReportFeatureProcessed(feature);
						}
						else
						{
							ReportFeatureSkipped(feature, reason);
						}
					}
					catch (Exception ex)
					{
						ReportFeatureFailed(feature, ex);
					}
				}
			}

			private void ProcessFeature([NotNull] Feature feature)
			{

			}
		}
	}
}
