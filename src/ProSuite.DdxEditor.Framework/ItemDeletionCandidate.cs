using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework
{
	public class ItemDeletionCandidate
	{
		[NotNull] private readonly List<DependingItem> _dependingItems = new List<DependingItem>();

		public ItemDeletionCandidate([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			Item = item;
		}

		[NotNull]
		public Item Item { get; }

		public void AddDependingItem([NotNull] DependingItem dependingItem)
		{
			_dependingItems.Add(dependingItem);
		}

		public int DependingItemCount => _dependingItems.Count;

		[NotNull]
		public IEnumerable<DependingItem> DependingItems => _dependingItems;
	}
}
