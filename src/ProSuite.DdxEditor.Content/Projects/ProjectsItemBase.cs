using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.Projects
{
	public abstract class ProjectsItemBase<TP> : EntityTypeItem<TP> where TP : Entity
	{
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static ProjectsItemBase()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.ProjectsOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.ProjectsOverlay);
		}

		protected ProjectsItemBase([NotNull] string text,
		                           [CanBeNull] string description)
			: base(text, description) { }

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;
	}
}
