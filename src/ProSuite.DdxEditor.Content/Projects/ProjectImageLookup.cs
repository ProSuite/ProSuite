using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Projects
{
	public static class ProjectImageLookup
	{
		[NotNull] private static readonly Image _image = Resources.ProjectItem;

		[NotNull]
		public static Image GetImage<T>([NotNull] Project<T> project)
			where T : ProductionModel
		{
			return _image;
		}
	}
}
