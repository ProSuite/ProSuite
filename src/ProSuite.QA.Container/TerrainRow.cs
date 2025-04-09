using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Container
{
	internal class TerrainRow : ISurfaceRow, IDataReference
	{
		[CanBeNull] private readonly ITestProgress _testProgress;

		private readonly double _resolution;

		[CanBeNull] private ISimpleSurface _tinSurface;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TerrainRow"/> class.
		/// </summary>
		/// <param name="box">The box.</param>
		/// <param name="terrainReference">The terrain reference.</param>
		/// <param name="resolution">The resolution.</param>
		/// <param name="testProgress">The test progress reporting instance.</param>
		internal TerrainRow([NotNull] IEnvelope box,
		                    [NotNull] TerrainReference terrainReference,
		                    double resolution,
		                    [CanBeNull] ITestProgress testProgress)
		{
			Assert.ArgumentNotNull(box, nameof(box));
			Assert.ArgumentNotNull(terrainReference, nameof(terrainReference));

			Extent = box;
			TerrainReference = terrainReference;
			_resolution = resolution;

			_testProgress = testProgress;
			DatasetName = Assert.NotNull(TerrainReference.Name);
		}

		#endregion

		public string DatasetName { get; }

		public string GetDescription()
		{
			return DatasetName;
		}

		public string GetLongDescription()
		{
			return GetDescription();
		}

		public int Execute(ContainerTest containerTest, int occurance, out bool applicable)
		{
			int index = containerTest.GetTerrainIndex(TerrainReference, occurance);
			applicable = true;
			return containerTest.Execute(this, index);
		}

		[NotNull]
		public TerrainReference TerrainReference { get; }

		public ISimpleSurface Surface => TinSurface;

		public bool HasLoadedSurface => _tinSurface != null;

		public void DisposeSurface()
		{
			if (_tinSurface == null)
			{
				return;
			}

			_msg.Debug("Disposing tin");

			_tinSurface.Dispose();

			_tinSurface = null;
		}

		public IEnvelope Extent { get; }

		[NotNull]
		public IGeometry Shape => Extent;

		private ISimpleSurface TinSurface
		{
			get
			{
				if (_tinSurface == null)
				{
					_msg.Debug("Getting tin for tile");
					using (_testProgress?.UseProgressWatch(
						       Step.TinLoading, Step.TinLoaded, 0, 1, TerrainReference))
					{
						ITin tin;
						try
						{
							tin = TerrainReference.CreateTin(Extent, _resolution);
						}
						catch (InvalidDataException e)
						{
							_msg.Debug("Error creating TIN. Throwing TestRowException.", e);
							throw new TestDataException(e.Message, this, e);
						}

						_tinSurface = new TinSurface(tin);
					}
				}

				return _tinSurface;
			}
		}
	}
}
