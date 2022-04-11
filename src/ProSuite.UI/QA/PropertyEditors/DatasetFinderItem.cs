using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.DataModel.ResourceLookup;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class DatasetFinderItem
	{
		private readonly Dataset _dataset;
		private readonly Image _image;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetFinderItem"/> class.
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		public DatasetFinderItem([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			_dataset = dataset;

			_image = DatasetTypeImageLookup.GetImage(dataset);
			_image.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(dataset);
		}

		#endregion

		[UsedImplicitly]
		public Image Image
		{
			get { return _image; }
		}

		[UsedImplicitly]
		public string Model
		{
			get { return _dataset.Model.Name; }
		}

		[UsedImplicitly]
		public string Name
		{
			get { return _dataset.Name; }
		}

		[UsedImplicitly]
		public string Alias
		{
			get { return _dataset.AliasName; }
		}

		[UsedImplicitly]
		public string Abbreviation
		{
			get { return _dataset.Abbreviation; }
		}

		[UsedImplicitly]
		public string Category
		{
			get
			{
				return _dataset.DatasetCategory != null
					       ? _dataset.DatasetCategory.Name
					       : string.Empty;
			}
		}

		public string Type
		{
			get { return _dataset.TypeDescription; }
		}

		public string Description
		{
			get { return _dataset.Description; }
		}

		[Browsable(false)]
		public Dataset Dataset
		{
			get { return _dataset; }
		}
	}
}
