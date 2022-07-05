using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.DataModel.ResourceLookup;
using ProSuite.UI.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel
{
	internal class DatasetParameterFinderItem
	{
		private readonly Image _image;
		private string _modelName;

		public DatasetParameterFinderItem(Dataset dataset)
		{
			Source = new Either<Dataset, TransformerConfiguration>(dataset);

			_image = DatasetTypeImageLookup.GetImage(dataset);
			_image.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(dataset);
		}

		public DatasetParameterFinderItem(TransformerConfiguration transformerConfig)
		{
			Source = new Either<Dataset, TransformerConfiguration>(transformerConfig);

			_image = TestTypeImageLookup.GetImage(transformerConfig);
			_image.Tag = TestTypeImageLookup.GetDefaultSortIndex(transformerConfig);
		}

		[UsedImplicitly]
		public Image Image => _image;

		[UsedImplicitly]
		public string Model => _modelName ??= DatasetParameterViewModelUtils.GetModelName(Source);

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
	}
}
