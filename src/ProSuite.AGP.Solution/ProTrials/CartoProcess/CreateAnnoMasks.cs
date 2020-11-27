using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Processing;
using ProSuite.Processing.Domain;
using ProSuite.Processing.Evaluation;
using ProSuite.Processing.Utils;

namespace ProSuite.AGP.Solution.ProTrials.CartoProcess
{
	public class CreateAnnoMasks : CartoProcess
	{
		public ProcessDatasetName InputDataset { get; set; }
		public ProcessDatasetName OutputMaskDataset { get; set; }
		public string OutputAssociation { get; set; } // relationship class, optional
		public string MaskAttributes { get; set; } // FieldSetter
		public string MaskMargin { get; set; } // NumericExpression
		public double SimplificationTolerance { get; set; } // TODO NumericExpression
		public MaskOutlineType MaskOutlineType { get; set; } // TODO EnumExpression
		public bool FillHoles { get; set; } // TODO BooleanExpression

		public override string Name => nameof(CreateAnnoMasks);

		public override void Initialize(CartoProcessConfig config)
		{
			Assert.ArgumentNotNull(config);

			InputDataset = config.GetRequiredValue<ProcessDatasetName>(nameof(InputDataset));
			OutputMaskDataset = config.GetRequiredValue<ProcessDatasetName>(nameof(OutputMaskDataset));
			OutputAssociation = config.GetOptionalValue<string>(nameof(OutputAssociation));
			MaskAttributes = config.GetOptionalValue<string>(nameof(MaskAttributes));
			MaskMargin = config.GetOptionalValue<string>(nameof(MaskMargin));
			SimplificationTolerance = config.GetOptionalValue<double>(nameof(SimplificationTolerance));
			MaskOutlineType = config.GetOptionalValue<MaskOutlineType>(nameof(MaskOutlineType));
			FillHoles = config.GetOptionalValue<bool>(nameof(FillHoles));
		}

		public override IEnumerable<ProcessDatasetName> GetOriginDatasets()
		{
			yield return InputDataset;
		}

		public override IEnumerable<ProcessDatasetName> GetDerivedDatasets()
		{
			yield return OutputMaskDataset;
		}

		public override void Execute(IProcessingContext context, IProcessingFeedback feedback)
		{
			using (var engine = new CreateAnnoMasksEngine(this, context, feedback))
			{
				// TODO engine.TransferDerivedFeatureSelection ?

				engine.Execute();

				engine.ReportProcessComplete("{0} masks created", engine.MasksCreated);
			}
		}

		private class CreateAnnoMasksEngine : CartoProcessEngineBase
		{
			private static readonly IMsg _msg = Msg.ForCurrentClass();

			private readonly ProcessingDataset _inputDataset;
			private readonly ProcessingDataset _outputMaskDataset;
			private readonly RelationshipClass _relationshipClass; // can be null
			private readonly FieldSetter _maskAttributes;
			private readonly ImplicitValue _maskMargin;
			private readonly double _simplificationToleranceMu;
			private readonly MaskOutlineType _maskOutlineType;
			private readonly bool _fillHoles;
			private const string InputQualifier = "input";

			public int MasksCreated { get; private set; }

			public CreateAnnoMasksEngine(CreateAnnoMasks config, IProcessingContext context,
			                             IProcessingFeedback feedback)
				: base(config.Name, context, feedback)
			{
				_inputDataset = OpenRequiredDataset(config.InputDataset, nameof(config.InputDataset));
				_outputMaskDataset = OpenRequiredDataset(config.OutputMaskDataset, nameof(config.OutputMaskDataset));
				_relationshipClass = OpenAssociation(config.OutputAssociation);
				_maskAttributes = ProcessingUtils.CreateFieldSetter(
					config.MaskAttributes, _outputMaskDataset.FeatureClass, nameof(config.MaskAttributes));
				_maskMargin = ImplicitValue.Create(config.MaskMargin, nameof(config.MaskMargin));
				_maskMargin.Environment = new StandardEnvironment().RegisterConversionFunctions();
				_simplificationToleranceMu = config.SimplificationTolerance; // TODO convert mm to mu
				_maskOutlineType = config.MaskOutlineType;
				_fillHoles = config.FillHoles;
			}

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
				var margin = _maskMargin.DefineFields(feature.RowValues()).Evaluate<double>(0.0);
				var simplTol = _simplificationToleranceMu;

				var maskShape = CreateMask(feature, simplTol, margin, _maskOutlineType, _fillHoles);

				using (var buffer = _outputMaskDataset.FeatureClass.CreateRowBuffer())
				{
					buffer[_outputMaskDataset.ShapeFieldName] = maskShape;

					_maskAttributes.ForgetAll()
					                     .DefineFields(feature, InputQualifier)
					                     .Execute(buffer);

					var maskFeature = _outputMaskDataset.FeatureClass.CreateRow(buffer);

					Relate(feature, maskFeature);

					MasksCreated += 1;
				}
			}

			private Geometry CreateMask(Feature feature, double simplTol, double margin, MaskOutlineType type, bool fillHoles)
			{
				var oid = feature.GetObjectID();
				var outlineType = type == MaskOutlineType.BoundingBox
					                  ? OutlineType.BoundingBox
					                  : OutlineType.Exact;

				var result = _inputDataset.Symbology.QueryDrawingOutline(oid, outlineType);

				if (simplTol > 0)
				{
					const bool removeDegenerateParts = true;
					const bool preserveCurves = true;
					result = GeometryEngine.Instance.Generalize(
						result, simplTol, removeDegenerateParts, preserveCurves);
				}

				switch (type)
				{
					case MaskOutlineType.BoundingBox:
					case MaskOutlineType.Exact:
						// nothing to do
						break;
					case MaskOutlineType.ExactSimplified:
						var maxDeviation = Math.Max(simplTol, margin / 20.0);
						const bool removeDegenerateParts = true;
						const bool preserveCurves = true;
						result = GeometryEngine.Instance.Generalize(
							result, maxDeviation, removeDegenerateParts, preserveCurves);
						break;
					case MaskOutlineType.ConvexHull:
						result = ConvexHull(result);
						break;
				}

				if (margin > 0 || margin < 0)
				{
					if (result.HasCurves())
					{
						result = GeometryEngine.Instance.DensifyByDeviation(result, margin / 10.0);
					}

					result = GeometryEngine.Instance.Buffer(result, margin);
				}

				if (fillHoles)
				{
					throw new NotImplementedException("FillHoles is not yet implemented, sorry"); // TODO
				}

				return GeometryEngine.Instance.SimplifyAsFeature(result);
			}

			private static Geometry ConvexHull(Geometry geometry)
			{
				return GeometryEngine.Instance.ConvexHull(geometry);
			}

			private void Relate(Feature origin, Feature derived)
			{
				if (_relationshipClass != null)
				{
					_relationshipClass.CreateRelationship(origin, derived);
				}
			}
		}
	}
}
