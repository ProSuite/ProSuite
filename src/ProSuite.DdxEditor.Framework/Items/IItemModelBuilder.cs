using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Items
{
	public interface IItemModelBuilder
	{
		[NotNull]
		IList<Item> GetRootItems();
	}
}
