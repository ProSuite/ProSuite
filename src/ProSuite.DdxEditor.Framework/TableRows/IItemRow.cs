using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.TableRows
{
	public interface IItemRow
	{
		[NotNull]
		Item Item { get; }
	}
}
