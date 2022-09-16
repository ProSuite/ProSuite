using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.Data
{
	public abstract class DataItemBase : GroupItem
	{
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static DataItemBase()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.DataItemOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.DataItemOverlay);
		}

		protected DataItemBase(string text, string description) : base(text, description) { }

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;
	}
}
