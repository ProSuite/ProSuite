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
using ProSuite.Processing.Utils;

namespace ProSuite.AGP.Solution.ProTrials.CartoProcess
{
	public class CalculateControlPoints : CartoProcess
	{
		public override string Name => nameof(CalculateControlPoints);

		public ProcessDatasetName InputDataset { get; set; }
		public double MaximumAngle { get; set; } // decimal degrees, 0..180
		public int ControlPointIdValue { get; set; }
		public double SimplificationTolerance { get; set; }

		public override void Initialize(CartoProcessConfig config)
		{
			Assert.ArgumentNotNull(config);

			InputDataset = config.GetRequiredValue<ProcessDatasetName>(nameof(InputDataset));
			MaximumAngle = config.GetOptionalValue<double>(nameof(MaximumAngle));
			ControlPointIdValue = config.GetOptionalValue<int>(nameof(ControlPointIdValue));
			SimplificationTolerance = config.GetOptionalValue<double>(nameof(SimplificationTolerance));
		}

		public override IEnumerable<ProcessDatasetName> GetOriginDatasets()
		{
			yield return InputDataset;
		}

		public override void Execute(IProcessingContext context, IProcessingFeedback feedback)
		{
			using (var engine = new CalculateControlPointsEngine(this, context, feedback))
			{
				engine.Execute();

				engine.ReportProcessComplete("{0}/{1} control points added/removed",
				                             engine.ControlPointsAdded,
				                             engine.ControlPointsRemoved);
			}
		}

		private class CalculateControlPointsEngine : CartoProcessEngineBase
		{
			private static readonly IMsg _msg = Msg.ForCurrentClass();

			private readonly ProcessingDataset _inputDataset;
			private readonly double _maximumAngle;
			private readonly int _controlPointIdValue;
			private readonly double _simplificationTolerance;

			public int ControlPointsAdded { get; private set; }
			public int ControlPointsRemoved { get; private set; }

			public CalculateControlPointsEngine(CalculateControlPoints config,
			                                    IProcessingContext context,
			                                    IProcessingFeedback feedback)
				: base(config.Name, context, feedback)
			{
				_inputDataset =
					OpenRequiredDataset(config.InputDataset, nameof(config.InputDataset));

				_maximumAngle = config.MaximumAngle;

				if (! (0 <= _maximumAngle && _maximumAngle <= 180))
				{
					throw ConfigError(
						$"{nameof(config.MaximumAngle)} is {config.MaximumAngle}, not between 0 and 180");
				}

				_controlPointIdValue = ProcessingUtils.Clip(
					config.ControlPointIdValue, 1, int.MaxValue,
					nameof(config.ControlPointIdValue));

				_simplificationTolerance = ProcessingUtils.Clip(
					config.SimplificationTolerance, 0, double.MaxValue,
					nameof(config.SimplificationTolerance)); // TODO convert Millimeters (Points) to MapUnits -- how?
			}

			public override void Execute()
			{
				TotalFeatures = CountInputFeatures(_inputDataset);
				if (TotalFeatures == 0)
				{
					return;
				}

				var perimeter = Context.GetProcessingPerimeter();

				foreach (Feature feature in GetInputFeatures(_inputDataset))
				{
					CheckCancel();

					try
					{
						ReportStartFeature(feature);

						if (AllowModification(feature, out string reason))
						{
							ProcessFeature(feature, perimeter);

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

			private void ProcessFeature(Feature feature, [CanBeNull] Polygon perimeter)
			{
				Polyline curve;

				var shape = feature.GetShape();

				if (shape is Polygon polygon)
				{
					var before = GetPointIDs(polygon); // TODO DEBUG DROP
					curve = GeometryUtils.Boundary(polygon);
				}
				else if (shape is Polyline polyline)
				{
					var before = GetPointIDs(polyline); // TODO DEBUG DROP
					curve = polyline;
				}
				else
				{
					curve = null;
					Assert.Fail("Input shape is neither Polyline nor Polygon");
				}

				if (_simplificationTolerance > 0)
				{
					curve = GeometryUtils.Generalize(curve, _simplificationTolerance);
					curve = GeometryUtils.Simplify(curve);
				}

				var builder = new PolylineBuilder(curve) {HasID = true};
				builder.SetEmpty();

				int controlPointsAdded = 0;
				int controlPointsRemoved = 0;

				// Things to watch out:
				// - preserve non-linear segments
				// - process each part separately
				// - remember Start/EndPoint or rings

				foreach (var part in curve.Parts)
				{
					var numSegments = part.Count;
					if (numSegments < 1) continue;
					bool startNewPart = true;

					var one = part[0];
					for (int i = 1; i < numSegments; i++)
					{
						var two = part[i];

						var centerPoint = two.StartPoint;
						if (ProcessingUtils.WithinPerimeter(centerPoint, perimeter))
						{
							DoVertex(ref one, ref two, ref controlPointsAdded, ref controlPointsRemoved);
						}

						builder.AddSegment(one, startNewPart);
						startNewPart = false;

						one = two;
					}

					// For polygons (closed rings), also look at Start/EndPoint
					if (shape is Polygon)
					{
						var two = part[0];
						var centerPoint = two.StartPoint;
						if (ProcessingUtils.WithinPerimeter(centerPoint, perimeter))
						{
							DoVertex(ref one, ref two, ref controlPointsAdded, ref controlPointsRemoved);
						}
					}

					builder.AddSegment(one, startNewPart);
				}

				if (controlPointsAdded > 0 || controlPointsRemoved > 0)
				{
					bool wantPolygon = shape is Polygon;

					if (wantPolygon)
					{
						var polyline = builder.ToGeometry();
						shape = PolygonBuilder.CreatePolygon(polyline);
					}
					else
					{
						shape = builder.ToGeometry();
					}

					var after = GetPointIDs((Multipart) shape);

					feature.SetShape(shape);
					feature.Store(); // TODO requires an edit session

					_msg.DebugFormat("Feature {0}: {1} control points added",
					                 ProcessingUtils.Format(feature), controlPointsAdded);

					ControlPointsAdded += controlPointsAdded;
					ControlPointsRemoved += controlPointsRemoved;
				}
			}

			private void DoVertex(ref Segment one, ref Segment two,
			                      ref int controlPointsAdded, ref int controlPointsRemoved)
			{
				var before = one.StartCoordinate;
				var center = one.EndCoordinate; // equals two.StartCoordinate
				var after = two.EndCoordinate;

				double degrees = AngleDegrees(before, center, after);

				if (degrees < _maximumAngle || degrees > 360 - _maximumAngle)
				{
					one = ControlPointUtils.SetPointID(one, null, _controlPointIdValue);
					two = ControlPointUtils.SetPointID(two, _controlPointIdValue, null);

					controlPointsAdded += 1;
				}
				else
				{
					one = ControlPointUtils.SetPointID(one, null, 0);
					two = ControlPointUtils.SetPointID(two, 0, null);

					controlPointsRemoved += 1;
				}
			}

			private static int[] GetPointIDs(Multipart curve)
			{
				int count = curve.PointCount;
				var points = curve.Points;
				var ids = new int[count];
				for (int i = 0; i < count; i++)
				{
					ids[i] = points[i].ID;
				}

				return ids;
			}

			private static double AngleDegrees(Coordinate2D a, Coordinate2D center, Coordinate2D b)
			{
				double centerX = center.X;
				double centerY = center.Y;

				double acX = a.X - centerX;
				double acY = a.Y - centerY;
				double bcX = b.X - centerX;
				double bcY = b.Y - centerY;

				double radians = Math.Atan2(acX * bcY - bcX * acY, acX * bcX + acY * bcY) %
				                 (2 * Math.PI);

				double degrees = radians * 180 / Math.PI;

				if (degrees < 0)
				{
					degrees += 360;
				}

				return degrees;
			}
		}
	}
}
