using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Processing;
using ProSuite.Processing.Domain;
using ProSuite.Processing.Evaluation;
using ProSuite.Processing.Utils;
using MapPoint = ArcGIS.Core.Geometry.MapPoint;

namespace ProSuite.AGP.Solution.ProTrials.CartoProcess
{
	public class AlignMarkers : CartoProcess
	{
		public override string Name => nameof(AlignMarkers);

		public ProcessDatasetName InputDataset { get; set; }
		public IList<ProcessDatasetName> ReferenceDatasets { get; set; }
		public double SearchDistance { get; set; }
		public string MarkerAttributes { get; set; }

		public override void Initialize(CartoProcessConfig config)
		{
			Assert.ArgumentNotNull(config);

			InputDataset = config.GetRequiredValue<ProcessDatasetName>(nameof(InputDataset));

			ReferenceDatasets = new List<ProcessDatasetName>();
			foreach (var dn in config.GetValues<ProcessDatasetName>(nameof(ReferenceDatasets)))
			{
				ReferenceDatasets.Add(dn);
			}

			if (ReferenceDatasets.Count < 1)
				throw ConfigError("At least one reference dataset is required");

			SearchDistance = config.GetOptionalValue<double>(nameof(SearchDistance));
			MarkerAttributes = config.GetOptionalValue<string>(nameof(MarkerAttributes));

			// Note: would provide utilities in base class for the operations above
			// Note: parameter name for lists: Dataset (sg) or Datasets (pl)?
		}

		public override IEnumerable<ProcessDatasetName> GetOriginDatasets()
		{
			yield return InputDataset;
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

			private const string InputQualifier = "input";
			private const string ReferenceQualifier = "reference";

			public AlignMarkersEngine(AlignMarkers config, IProcessingContext context, IProcessingFeedback feedback)
				: base(config.Name, context, feedback)
			{
				_inputDataset =
					OpenRequiredDataset(config.InputDataset, nameof(config.InputDataset));

				_referenceDatasets = OpenDatasets(config.ReferenceDatasets);

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
				TotalFeatures = CountInputFeatures(_inputDataset);
				if (TotalFeatures == 0)
				{
					return;
				}

				foreach (var feature in GetInputFeatures(_inputDataset))
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
				var shape = feature.GetShape();
				var point = Assert.NotNull(shape as MapPoint, "Input shape is not MapPoint");

				IDictionary<Feature, double> distanceByFeature =
					GetNearFeatures(point, _referenceDatasets, _searchDistance);

				if (distanceByFeature.Count == 0)
				{
					_msg.DebugFormat(
						"Marker feature {0}: No reference feature found within search distance of {1}",
						ProcessingUtils.Format(feature), _searchDistance);
					return;
				}

				var nearest = distanceByFeature.OrderBy(f => f.Value).First();

				var referenceFeature = Assert.NotNull(nearest.Key, "Oops, bug");
				var distance = nearest.Value; // may be zero

				var referenceShape = referenceFeature.GetShape();
				var referenceCurve = Assert.NotNull(referenceShape as Multipart,
				                                    "Reference shape is not Multipart");

				double distanceAlongCurve = GeometryUtils.GetDistanceAlongCurve(referenceCurve, point);

				double normalLength = Math.Max(_inputDataset.XYTolerance, distance);
				var normalPoly = GeometryEngine.Instance.QueryNormal(
					referenceCurve, SegmentExtension.NoExtension, distanceAlongCurve,
					AsRatioOrLength.AsLength, normalLength);
				var normal = (LineSegment) normalPoly.Parts[0][0]; // TODO safety guards

				double tangentLength = Math.Max(_inputDataset.XYTolerance, distance);
				var tangentPoly = GeometryEngine.Instance.QueryTangent(
					referenceCurve, SegmentExtension.NoExtension, distanceAlongCurve,
					AsRatioOrLength.AsLength, tangentLength);
				var tangent = (LineSegment) tangentPoly.Parts[0][0]; // TODO safety guards

				// ILine.Angle is the angle between line and positive x axis,
				// but the Angle property of a representation marker has its
				// zero point at North: add 90° to ILine.Angle to fix:

				double normalOffset = MathUtils.ToDegrees(normal.Angle) - 90;
				double normalAngle = ProcessingUtils.ToPositiveDegrees(normalOffset);

				double tangentOffset = MathUtils.ToDegrees(tangent.Angle) - 90;
				double tangentAngle = ProcessingUtils.ToPositiveDegrees(tangentOffset);

				_markerFieldSetter.ForgetAll()
				                  .DefineFields(feature, InputQualifier)
								  .DefineFields(referenceFeature, ReferenceQualifier)
				                  .DefineValue("normalAngle", normalAngle)
				                  .DefineValue("tangentAngle", tangentAngle)
				                  .DefineValue("distance", distance)
				                  .Execute(feature);

				feature.Store();

				FeaturesAligned += 1;

				_msg.DebugFormat(
					"Marker feature {0}: aligned to {1} (normalAngle: {2}, tangentAngle: {3}, distance: {4})",
					ProcessingUtils.Format(feature), ProcessingUtils.Format(referenceFeature),
					normalAngle, tangentAngle, distance);

				// TODO need some mechanism to ensure disposal (required by Pro SDK documentation); see also OneNote
				foreach (var pair in distanceByFeature)
				{
					pair.Key.Dispose();
				}
			}

			[NotNull]
			private IDictionary<Feature, double> GetNearFeatures(
				[NotNull] Geometry geometry,
				[NotNull] IEnumerable<ProcessingDataset> datasets,
				double searchDistance)
			{
				Assert.ArgumentNotNull(geometry, nameof(geometry));
				Assert.ArgumentNotNull(datasets, nameof(datasets));

				var result = new Dictionary<Feature, double>();

				Envelope searchExtent = geometry.Extent;

				searchExtent = searchExtent.Expand(searchDistance, searchDistance, false);

				foreach (ProcessingDataset dataset in datasets.Where(dataset => dataset != null))
				{
					foreach (Feature feature in GetOtherFeatures(dataset, searchExtent))
					{
						Geometry otherShape = feature.GetShape();

						if (otherShape is Polygon)
						{
							// want the outline of the polygon:
							otherShape = GeometryEngine.Instance.Boundary(otherShape);
						}

						double distance = GeometryEngine.Instance.Distance(geometry, otherShape);
						if (distance <= searchDistance)
						{
							result.Add(feature, distance);
						}
					}
				}

				return result;
			}
		}
	}
}
