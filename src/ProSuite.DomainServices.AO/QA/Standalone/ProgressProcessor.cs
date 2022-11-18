using System.Collections.Generic;
using System.Threading;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.DomainServices.AO.QA.Standalone
{
	public class ProgressProcessor
	{
		[NotNull] private readonly IDictionary<ITest, QualitySpecificationElement>
			_elementsByTest;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private bool _firstNonContainerTestReported;
		private bool _firstTileProcessingReported;
		[NotNull] private readonly CancellationTokenSource _cancellationTokenSource;
		[CanBeNull] private readonly ITrackCancel _trackCancel;

		public ProgressProcessor(
			[NotNull] CancellationTokenSource cancellationTokenSource,
			[NotNull] IDictionary<ITest, QualitySpecificationElement> elementsByTest,
			[CanBeNull] ITrackCancel trackCancel)
		{
			Assert.ArgumentNotNull(cancellationTokenSource, nameof(cancellationTokenSource));
			Assert.ArgumentNotNull(elementsByTest, nameof(elementsByTest));

			_cancellationTokenSource = cancellationTokenSource;
			_elementsByTest = elementsByTest;
			_trackCancel = trackCancel;
		}

		public void Process([NotNull] VerificationProgressEventArgs progressArgs)
		{
			Assert.ArgumentNotNull(progressArgs, nameof(progressArgs));

			// check for cancelled
			switch (progressArgs.ProgressStep)
			{
				// case Step.TestRowCreated:  // once for each row to be tested
				case Step.ITestProcessing:
				case Step.TileProcessing:
					if (CheckCancelled(_trackCancel, _cancellationTokenSource))
					{
						Cancelled = true;
						return;
					}

					break;
			}

			// log messages
			switch (progressArgs.ProgressStep)
			{
				case Step.ITestProcessing:
					if (! _firstNonContainerTestReported)
					{
						_msg.Info(
							"Verifying quality conditions based on non-container tests");
						_firstNonContainerTestReported = true;
					}

					QualityCondition qualityCondition = progressArgs.Tag as QualityCondition;
					if (qualityCondition == null)
					{
						var test = (ITest) progressArgs.Tag;
						qualityCondition = _elementsByTest[test].QualityCondition;
					}

					_msg.InfoFormat("  {0}", qualityCondition.Name);
					break;

				case Step.DataLoading:
					string tableName = (progressArgs.Tag as IReadOnlyTable)?.Name;
					_msg.DebugFormat("    Loading data{0}...",
					                 tableName == null ? string.Empty : $" ({tableName})");
					break;

				case Step.DataLoaded:
					_msg.Debug("    Data loaded");
					break;

				case Step.TileProcessing:
					if (! _firstTileProcessingReported)
					{
						_msg.Info(
							"Verifying quality conditions per cached tiles (container tests)");
						_firstTileProcessingReported = true;
					}

					_msg.InfoFormat("  Processing tile {0} of {1}: {2}",
					                progressArgs.Current, progressArgs.Total,
					                GeometryUtils.Format(progressArgs.CurrentBox));
					break;

				case Step.TileProcessed:
					if (progressArgs.Current > 0)
					{
						_msg.Debug("  Tile processed");
					}

					break;
			}
		}

		public void Process([NotNull] ProgressArgs progressArgs)
		{
			Assert.ArgumentNotNull(progressArgs, nameof(progressArgs));

			// check for cancelled
			switch (progressArgs.CurrentStep)
			{
				// case Step.TestRowCreated:  // once for each row to be tested
				case Step.ITestProcessing:
				case Step.TileProcessing:
					if (CheckCancelled(_trackCancel, _cancellationTokenSource))
					{
						Cancelled = true;
						return;
					}

					break;
			}

			// log messages
			switch (progressArgs.CurrentStep)
			{
				case Step.ITestProcessing:
					if (! _firstNonContainerTestReported)
					{
						_msg.Info(
							"Verifying quality conditions based on non-container tests");
						_firstNonContainerTestReported = true;
					}

					var test = (ITest) progressArgs.Tag;
					QualityCondition qualityCondition =
						_elementsByTest[test].QualityCondition;
					_msg.InfoFormat("  {0}", qualityCondition.Name);
					break;

				case Step.DataLoading:
					string tableName = (progressArgs.Tag as IReadOnlyTable)?.Name;
					_msg.DebugFormat("    Loading data{0}...",
					                 tableName == null ? string.Empty : $" ({tableName})");
					break;

				case Step.DataLoaded:
					_msg.Debug("    Data loaded");
					break;

				case Step.TileProcessing:
					if (! _firstTileProcessingReported)
					{
						_msg.Info(
							"Verifying quality conditions per cached tiles (container tests)");
						_firstTileProcessingReported = true;
					}

					_msg.InfoFormat("  Processing tile {0} of {1}: {2}",
					                progressArgs.Current, progressArgs.Total,
					                GeometryUtils.Format(progressArgs.CurrentEnvelope));
					break;

				case Step.TileProcessed:
					if (progressArgs.Current > 0)
					{
						_msg.Debug("  Tile processed");
					}

					break;
			}
		}

		public bool Cancelled { get; private set; }

		private static bool CheckCancelled([CanBeNull] ITrackCancel trackCancel,
		                                   [NotNull]
		                                   CancellationTokenSource cancellationTokenSource)
		{
			if (trackCancel != null && ! trackCancel.Continue())
			{
				cancellationTokenSource.Cancel();
				return true;
			}

			return false;
		}
	}
}
