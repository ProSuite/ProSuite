using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	public class LinearNetworkDatasetTableRow
	{
		private readonly LinearNetworkDataset _linearNetworkDataset;
		private readonly Image _image;

		public LinearNetworkDatasetTableRow(LinearNetworkDataset linearNetworkDataset)
		{
			Assert.ArgumentNotNull(linearNetworkDataset, nameof(linearNetworkDataset));
			_linearNetworkDataset = linearNetworkDataset;

			IVectorDataset dataset = linearNetworkDataset.Dataset;
			_image = DatasetTypeImageLookup.GetImage(dataset);
			_image.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(dataset);
		}

		[Browsable(false)]
		public VectorDataset TargetDataset => _linearNetworkDataset.Dataset;

		[UsedImplicitly]
		public Image Image => _image;

		public string ModelName => _linearNetworkDataset.Dataset.Model.Name;

		public string DatasetAliasName => _linearNetworkDataset.Dataset.AliasName;

		public bool IsDefaultJunction
		{
			get { return _linearNetworkDataset.IsDefaultJunction; }
			set { _linearNetworkDataset.IsDefaultJunction = value; }
		}

		public bool Splitting
		{
			get { return _linearNetworkDataset.Splitting; }
			set { _linearNetworkDataset.Splitting = value; }
		}

		public string WhereClause
		{
			get { return _linearNetworkDataset.WhereClause; }
			set { _linearNetworkDataset.WhereClause = value; }
		}
	}
}
