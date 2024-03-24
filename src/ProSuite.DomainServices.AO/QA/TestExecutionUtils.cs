using System;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.DomainServices.AO.QA
{
	public static class TestExecutionUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static int Execute([NotNull] TestContainer container,
		                          [CanBeNull] AreaOfInterest areaOfInterest)
		{
			Assert.ArgumentNotNull(container, nameof(container));

			IGeometry testPerimeter = areaOfInterest?.Geometry;

			// TODO move enlarging by search distance WITHIN the container?
			IGeometry enlargedTestPerimeter = GetEnlargedTestPerimeter(container,
				testPerimeter);

			var box = enlargedTestPerimeter as IEnvelope;
			if (box != null)
			{
				return container.Execute(box);
			}

			var polygon = enlargedTestPerimeter as IPolygon;
			if (polygon != null)
			{
				return container.Execute(polygon);
			}

			if (enlargedTestPerimeter == null)
			{
				return container.Execute();
			}

			throw new ArgumentException("Invalid geometry type " +
			                            enlargedTestPerimeter.GeometryType);
		}

		internal static void LogProgress([NotNull] VerificationProgressEventArgs args)
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				LogProgressVerbose(args);
			}
			else if (_msg.IsDebugEnabled)
			{
				LogProgressDebug(args);
			}
		}

		public static void ReportRowWithStopCondition(
			[NotNull] QaError qaError,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] RowsWithStopConditions allRowsWithStopCondition)
		{
			Assert.ArgumentNotNull(qaError);
			Assert.ArgumentNotNull(qualityCondition);
			Assert.ArgumentNotNull(allRowsWithStopCondition);

			StopInfo stopInfo = null;
			if (qualityCondition.StopOnError)
			{
				stopInfo = new StopInfo(qualityCondition, qaError.Description);

				foreach (InvolvedRow involvedRow in qaError.InvolvedRows)
				{
					allRowsWithStopCondition.Add(involvedRow.TableName,
					                             involvedRow.OID, stopInfo);
				}
			}

			if (! qualityCondition.AllowErrors)
			{
				if (stopInfo != null)
				{
					// it's a stop condition, and it is a 'hard' condition, and the error is 
					// relevant --> consider the stop situation as sufficiently reported 
					// (no reporting in case of stopped tests required)
					stopInfo.Reported = true;
				}
			}
		}

		internal static string GetStopInfoErrorDescription([NotNull] StopInfo stopInfo)
		{
			var sb = new StringBuilder();

			sb.AppendFormat("Not tested due to violation of stop condition {0}:",
			                stopInfo.QualityCondition.Name);
			sb.AppendLine();
			sb.Append(stopInfo.ErrorDescription);

			return sb.ToString();
		}

		internal static void AssignExecutionTimes(
			[NotNull] QualityVerification qualityVerification,
			[NotNull] IEnumerable<KeyValuePair<ITest, TestVerification>> testVerifications,
			[NotNull] VerificationTimeStats verificationTimeStats,
			[NotNull] IDatasetLookup datasetLookup)
		{
			// Assign execute time
			foreach (KeyValuePair<ITest, TestVerification> pair in testVerifications)
			{
				AssignExecutionTime(pair.Key,
				                    pair.Value.QualityConditionVerification,
				                    verificationTimeStats);
			}

			// Assign load time
			foreach (
				KeyValuePair<IReadOnlyDataset, double> pair in verificationTimeStats
					.DatasetLoadTimes)
			{
				IReadOnlyDataset gdbDataset = pair.Key;
				Dataset dataset;

				try
				{
					dataset = datasetLookup.GetDataset((IDatasetName) gdbDataset.FullName);
				}
				catch (Exception e)
				{
					_msg.Warn($"Error getting dataset for {gdbDataset.Name}. " +
					          "No load times will be assigned.", e);
					continue;
				}

				if (dataset == null)
				{
					continue;
				}

				QualityVerificationDataset verificationDataset =
					qualityVerification.GetVerificationDataset(dataset);

				if (verificationDataset != null)
				{
					verificationDataset.LoadTime = pair.Value / 1000.0;
				}
			}
		}

		private static void AssignExecutionTime(
			[NotNull] ITest test,
			[NotNull] QualityConditionVerification conditionVerification,
			[NotNull] VerificationTimeStats verificationTimes)
		{
			double milliseconds;
			if (verificationTimes.TryGetTestTime(test, out milliseconds))
			{
				conditionVerification.ExecuteTime = milliseconds / 1000.0;
				return;
			}

			var containerTest = test as ContainerTest;
			if (containerTest == null)
			{
				return;
			}

			double rowMilliseconds;
			double tileCompletionMilliseconds;
			if (verificationTimes.TryGetContainerTestTimes(containerTest,
			                                               out rowMilliseconds,
			                                               out tileCompletionMilliseconds))
			{
				conditionVerification.RowExecuteTime = rowMilliseconds / 1000.0;
				conditionVerification.TileExecuteTime = tileCompletionMilliseconds / 1000.0;
			}
		}

		private static void LogProgressDebug(VerificationProgressEventArgs args)
		{
			switch (args.ProgressType)
			{
				case VerificationProgressType.PreProcess:
					_msg.DebugFormat("Preprocessing: {0}", args.Tag);
					break;

				case VerificationProgressType.ProcessNonCache:
					switch (args.ProgressStep)
					{
						case Step.ITestProcessing:
							var qualityCondition = args.Tag as QualityCondition;
							_msg.DebugFormat("Processing non-container test: {0}",
							                 qualityCondition == null
								                 ? args.Tag
								                 : qualityCondition.Name);
							break;

						case Step.DataLoading:
							var dataset = args.Tag as IReadOnlyDataset;
							if (dataset != null)
							{
								_msg.DebugFormat("Loading data: {0}", dataset.Name);
							}

							break;
					}

					break;

				case VerificationProgressType.ProcessContainer:
					switch (args.ProgressStep)
					{
						case Step.ITestProcessing:
							var qualityCondition = args.Tag as QualityCondition;
							_msg.DebugFormat("Processing standalone test: {0}",
							                 qualityCondition == null
								                 ? args.Tag
								                 : qualityCondition.Name);
							break;

						case Step.DataLoading:
							var dataset = args.Tag as IReadOnlyDataset;
							_msg.DebugFormat("Loading data: {0}",
							                 dataset == null
								                 ? args.Tag
								                 : dataset.Name);
							break;

						case Step.TileProcessing:
							if (args.CurrentBox != null)
							{
								double xMin;
								double yMin;
								double xMax;
								double yMax;
								args.CurrentBox.QueryCoords(out xMin, out yMin, out xMax, out yMax);

								string format = xMax < 400
									                ? "Verifying tile {0} of {1}, (extent: {2:N6}, {3:N6}, {4:N6}, {5:N6})"
									                : "Verifying tile {0} of {1}, (extent: {2:N2}, {3:N2}, {4:N2}, {5:N2})";

								_msg.DebugFormat(format, args.Current + 1, args.Total,
								                 xMin, yMin, xMax, yMax);
							}

							break;

						case Step.TileProcessed:
							if (args.Current > 0)
							{
								_msg.DebugFormat("Processed tile {0} of {1}", args.Current,
								                 args.Total);
							}

							break;
					}

					break;

				case VerificationProgressType.Error:
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static void LogProgressVerbose(
			[NotNull] VerificationProgressEventArgs args)
		{
			string message = args.ProgressType != VerificationProgressType.Error
				                 ? string.Format("{0} {1}, Step {2} of {3}: {4}",
				                                 args.ProgressType, args.ProgressStep,
				                                 args.Current,
				                                 args.Total,
				                                 TranslateTag(args))
				                 : string.Format("{0} #{1}", args.ProgressType, args.Current);

			_msg.Debug(message);
		}

		private static object TranslateTag([NotNull] VerificationProgressEventArgs args)
		{
			object tag = args.Tag;

			var dataset = tag as IReadOnlyDataset;
			if (dataset != null)
			{
				return dataset.Name;
			}

			var row = tag as TestRow;
			return row?.DataReference.GetDescription() ?? tag;
		}

		[CanBeNull]
		private static IGeometry GetEnlargedTestPerimeter(
			[NotNull] TestContainer container,
			[CanBeNull] IGeometry testPerimeter)
		{
			Assert.ArgumentNotNull(container, nameof(container));

			if (testPerimeter == null)
			{
				return null;
			}

			if (testPerimeter.IsEmpty)
			{
				return testPerimeter;
			}

			double maximumSearchDistance = TestUtils.GetMaximumSearchDistance(container.Tests);

			if (Math.Abs(maximumSearchDistance) < double.Epsilon)
			{
				return testPerimeter;
			}

			// returns the expanded *ENVELOPE* of the input perimeter
			_msg.DebugFormat("Creating test envelope expanded by maximum search distance: {0}",
			                 maximumSearchDistance);

			IEnvelope enlarged = GeometryFactory.Clone(testPerimeter.Envelope);

			const bool asRatio = false;
			enlarged.Expand(maximumSearchDistance, maximumSearchDistance, asRatio);

			// filtering by polygon seems to be more robust than filtering by envelope
			// (if the other geometry is very large)
			return GeometryFactory.CreatePolygon(enlarged);
		}
	}
}
