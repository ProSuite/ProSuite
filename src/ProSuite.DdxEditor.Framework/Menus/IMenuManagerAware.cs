using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Menus
{
	public interface IMenuManagerAware
	{
		[CanBeNull]
		IMenuManager MenuManager { get; set; }
	}
}
