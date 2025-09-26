using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Projects
{
	public class ProjectsItem<TP, TM> : EntityTypeItem<TP> where TP : Project<TM>
	                                                       where TM : ProductionModel
	{
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static ProjectsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.ProjectsOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.ProjectsOverlay);
		}

		protected ProjectsItem([NotNull] string text,
		                       [CanBeNull] string description)
			: base(text, description) { }

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;
	}
}
