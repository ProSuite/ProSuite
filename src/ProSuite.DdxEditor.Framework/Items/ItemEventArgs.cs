using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Items
{
	public class ItemEventArgs : EventArgs
	{
		public ItemEventArgs([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			Item = item;
		}

		[NotNull]
		public Item Item { get; }
	}
}
