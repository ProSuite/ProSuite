using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.Projects
{
	public abstract class ProjectItem<TP> : SimpleEntityItem<TP, TP>
		where TP : Entity
	{
		[NotNull] private static readonly Image _image;

		static ProjectItem()
		{
			_image = Resources.ProjectItem;
		}

		protected ProjectItem([NotNull] TP entity,
		                      [NotNull] IRepository<TP> repository)
			: base(entity, repository) { }

		public override Image Image => _image;
	}
}
