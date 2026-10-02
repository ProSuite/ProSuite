using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface.PointCloud;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// A <see cref="PointCloudReference"/> whose tiles come from an <see cref="ILasFileSource"/>.
	/// It provides what a test needs: the files covering an area, the spatial reference, the extent.
	/// </summary>
	/// <remarks>
	/// Abstract members: Where the tiles are catalogued (<see cref="PointCloudReference.DbContainer"/>)
	/// and equality members. Those are properties of the container, not of the file list.
	/// <para>
	/// Note that only <see cref="LasCatalogReference"/> exists today. The other
	/// <see cref="ILasFileSource"/> implementations (Esri .lasd, LasDatasetDef json, single file)
	/// could be added later, as needed.
	/// </para>
	/// </remarks>
	public abstract class LasFileSourceReference : PointCloudReference
	{
		protected LasFileSourceReference([NotNull] string name,
		                                 [NotNull] ILasFileSource fileSource)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(fileSource, nameof(fileSource));

			Name = name;
			FileSource = fileSource;
		}

		/// <summary>The source the tiles are read from.</summary>
		[NotNull]
		protected ILasFileSource FileSource { get; }

		public override string Name { get; }

		public override ISpatialReference SpatialReference => FileSource.SpatialReference;

		public override IEnvelope Extent => FileSource.Extent;

		public override IEnumerable<(string FilePath, IEnvelope TileExtent)> GetFiles(
			IEnvelope searchArea)
		{
			return searchArea == null
				       ? FileSource.GetAllFiles()
				       : FileSource.GetIntersectingFiles(searchArea);
		}
	}
}
