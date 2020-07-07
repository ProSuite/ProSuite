using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.PropertyEditors
{
	public interface IAttributeInfoProvider : IDataChanged
	{
		void SetAttributeName([NotNull] string name);
	}
}
