using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.TableRows;

namespace ProSuite.DdxEditor.Framework.Search
{
	public abstract class SearchProviderBase<TEntity, TRow> : ISearchProvider
		where TEntity : Entity
		where TRow : class, IEntityRow<TEntity>
	{
		[NotNull] private readonly ItemModelBuilderBase _itemModelBuilder;

		protected SearchProviderBase([NotNull] ItemModelBuilderBase itemModelBuilder,
		                             [NotNull] string text,
		                             [CanBeNull] Image overlay = null)
		{
			Assert.ArgumentNotNull(itemModelBuilder, nameof(itemModelBuilder));
			Assert.ArgumentNotNullOrEmpty(text, nameof(text));

			_itemModelBuilder = itemModelBuilder;

			Text = text;

			Image = ItemUtils.GetFindImage(overlay);
		}

		public string Text { get; }

		public Image Image { get; }

		public Entity SearchEntity(IWin32Window owner)
		{
			using (new WaitCursor())
			{
				var finder = new Finder<TRow>();
				TRow result = finder.ShowDialog(owner, GetRows().ToList());

				return result?.Entity;
			}
		}

		[NotNull]
		private IEnumerable<TRow> GetRows()
		{
			return _itemModelBuilder.ReadOnlyTransaction(GetRowsCore);
		}

		[NotNull]
		protected abstract IEnumerable<TRow> GetRowsCore();
	}
}
