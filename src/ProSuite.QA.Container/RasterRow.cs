using System.Reflection;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Container
{
	internal class RasterRow : ISurfaceRow, IDataReference
	{
		[NotNull] private readonly ITestProgress _testProgress;

		[CanBeNull] private ISimpleSurface _rasterSurface;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TerrainRow"/> class.
		/// </summary>
		/// <param name="box">The box.</param>
		/// <param name="rasterReference">The dynamic surface.</param>
		/// <param name="testProgress">The test progress reporting instance.</param>
		internal RasterRow([NotNull] IEnvelope box,
		                   [NotNull] RasterReference rasterReference,
		                   [NotNull] ITestProgress testProgress)
		{
			Assert.ArgumentNotNull(box, nameof(box));
			Assert.ArgumentNotNull(rasterReference, nameof(rasterReference));
			Assert.ArgumentNotNull(testProgress, nameof(testProgress));

			Extent = box;
			RasterReference = rasterReference;

			_testProgress = testProgress;
			DatasetName = Assert.NotNull(rasterReference.Dataset.Name);
		}

		#endregion

		[NotNull]
		public RasterReference RasterReference { get; }

		public IEnvelope Extent { get; }

		public string DatasetName { get; }

		public string GetDescription()
		{
			return DatasetName;
		}

		public string GetLongDescription()
		{
			return GetDescription();
		}

		public ISimpleSurface Surface => RasterSurface;

		public bool HasLoadedSurface => _rasterSurface != null;

		public int Execute(ContainerTest containerTest, int occurance, out bool applicable)
		{
			int rasterIndex = containerTest.GetRasterIndex(RasterReference, occurance);
			applicable = true;
			return containerTest.Execute(this, rasterIndex);
		}

		public void DisposeSurface()
		{
			if (_rasterSurface == null)
			{
				return;
			}

			_msg.Debug("Disposing raster");

			_rasterSurface.Dispose();
			_rasterSurface = null;
		}

		[NotNull]
		private ISimpleSurface RasterSurface
		{
			get
			{
				if (_rasterSurface == null)
				{
					_msg.Debug("Getting rastersurface for tile");
					using (_testProgress.UseProgressWatch(Step.RasterLoading,
					                                      Step.RasterLoaded,
					                                      0, 1, RasterReference))
					{
						_rasterSurface = RasterReference.CreateSurface(Extent);
					}
				}

				return _rasterSurface;
			}
		}
	}
}
