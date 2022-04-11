using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.TableRows
{
	public class ItemRow : IItemRow
	{
		[NotNull] private readonly Item _item;

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemRow"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		public ItemRow([NotNull] Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			_item = item;
		}

		[UsedImplicitly]
		public Image Image => _item.Image;

		[UsedImplicitly]
		public string Name => _item.Text;

		[UsedImplicitly]
		public string Description => _item.Description;

		#region IItemRow Members

		[Browsable(false)]
		public Item Item => _item;

		#endregion
	}
}
