using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.DataModel.ResourceLookup;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.UI.Core.QA.BoundTableRows
{
	public class DatasetFinderItem
	{
		private readonly Image _image;
		private string _modelName;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetFinderItem"/> class.
		/// </summary>
		/// <param name="dataset">The dataset.</param>
		public DatasetFinderItem([NotNull] Dataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			Source = new Either<Dataset, TransformerConfiguration>(dataset);

			_image = DatasetTypeImageLookup.GetImage(dataset);
			_image.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(dataset);
		}

		/// <summary>
		/// Creates a 'virtual dataset' finder item behind which there is a transformer
		/// </summary>
		/// <param name="transformerConfig"></param>
		public DatasetFinderItem(TransformerConfiguration transformerConfig)
		{
			Source = new Either<Dataset, TransformerConfiguration>(transformerConfig);

			_image = TestTypeImageLookup.GetImage(transformerConfig);
			_image.Tag = TestTypeImageLookup.GetDefaultSortIndex(transformerConfig);
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
			get
			{
				if (_modelName == null)
				{
					_modelName =
						Source.Match(d => d?.Model?.Name,
						             InstanceConfigurationUtils.GetDatasetModelName);
				}

				return _modelName;
			}
		}

		[UsedImplicitly]
		public string Name => Source.Match(d => d.Name, t => t.Name);

		[UsedImplicitly]
		public string Alias =>
			Source.Match(d => d?.AliasName,
			             t => t?.Description);

		[UsedImplicitly]
		public string Abbreviation =>
			Source.Match(d => d?.Abbreviation,
			             t => null);

		[UsedImplicitly]
		public string Category =>
			Source.Match(d => d.DatasetCategory?.Name ?? string.Empty,
			             t => t.Category?.Name ?? string.Empty);

		[UsedImplicitly]
		public string Type =>
			Source.Match(d => d.TypeDescription,
			             t => t.TransformerDescriptor.TypeDisplayName);

		[UsedImplicitly]
		public string Description =>
			Source.Match(d => d.Description,
			             t => t.Description);

		[Browsable(false)]
		public Either<Dataset, TransformerConfiguration> Source { get; }

		[Browsable(false)]
		public Dataset Dataset => Source.Match(d => d, t => null);
	}
}
