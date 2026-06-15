using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	public class TerrainSourceDatasetTableRow
	{
		private readonly TerrainSourceDataset _sourceDataset;
		private readonly Image _image;

		public TerrainSourceDatasetTableRow(TerrainSourceDataset sourceDataset)
		{
			Assert.ArgumentNotNull(sourceDataset, nameof(sourceDataset));
			_sourceDataset = sourceDataset;

			IVectorDataset dataset = sourceDataset.Dataset;
			_image = DatasetTypeImageLookup.GetImage(dataset);
			_image.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(dataset);
		}

		[Browsable(false)]
		public IVectorDataset TargetDataset => _sourceDataset.Dataset;

		[UsedImplicitly]
		public Image Image => _image;

		public string ModelName => _sourceDataset.Dataset.Model.Name;

		public string DatasetAliasName => _sourceDataset.Dataset.AliasName;

		public string WhereClause
		{
			get { return _sourceDataset.WhereClause; }
			set { _sourceDataset.WhereClause = value; }
		}

		public TinSurfaceType SurfaceType
		{
			get { return _sourceDataset.SurfaceFeatureType; }
			set { _sourceDataset.SurfaceFeatureType = value; }
		}
	}
}
