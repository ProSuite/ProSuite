using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class AlgorithmDescriptorsItem : GroupItem
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static AlgorithmDescriptorsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.TestDescriptorsOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(
				Resources.TestDescriptorsOverlay);
		}

		public AlgorithmDescriptorsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base("Algorithm Descriptors", "Test, transformer and filter implementations")
		{
			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override bool AllowDeleteSelectedChildren => false;

		protected override bool SortChildren => false;

		protected override IEnumerable<Item> GetChildren()
		{
			yield return RegisterChild(new TestDescriptorsItem(_modelBuilder));

			yield return RegisterChild(new TransformerDescriptorsItem(_modelBuilder));
			yield return RegisterChild(new IssueFilterDescriptorsItem(_modelBuilder));
		}
	}
}
