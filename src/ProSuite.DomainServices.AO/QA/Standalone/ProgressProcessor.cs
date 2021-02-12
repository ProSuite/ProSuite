using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.DomainServices.AO.QA.Standalone
{
	public class ProgressProcessor
	{
		[NotNull] private readonly IDictionary<ITest, QualitySpecificationElement>
			_elementsByTest;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private bool _firstNonContainerTestReported;
		private bool _firstTileProcessingReported;
		[NotNull] private readonly TestContainer _testContainer;
		[CanBeNull] private readonly ITrackCancel _trackCancel;

		public ProgressProcessor(
			[NotNull] TestContainer testContainer,
			[NotNull] IDictionary<ITest, QualitySpecificationElement> elementsByTest,
			[CanBeNull] ITrackCancel trackCancel)
		{
			Assert.ArgumentNotNull(testContainer, nameof(testContainer));
			Assert.ArgumentNotNull(elementsByTest, nameof(elementsByTest));

			_testContainer = testContainer;
			_elementsByTest = elementsByTest;
			_trackCancel = trackCancel;
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
					if (CheckCancelled(_trackCancel, _testContainer))
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
					_msg.Debug("    Loading data...");
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
		                                   [NotNull] TestContainer testContainer)
		{
			if (trackCancel != null && ! trackCancel.Continue())
			{
				testContainer.StopExecute();
				return true;
			}

			return false;
		}
	}
}
