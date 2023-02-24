using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;
using ProSuite.DdxEditor.Framework.TableRows;

namespace ProSuite.DdxEditor.Framework.Commands
{
	public abstract class FindEntityItemCommandBase<T, E> : ItemCommandBase<Item>
		where T : class, IEntityRow
		where E : Entity
	{
		[NotNull] private readonly ItemModelBuilderBase _modelBuilder;

		protected FindEntityItemCommandBase(
			[NotNull] Item item,
			[NotNull] IApplicationController applicationController,
			[NotNull] ItemModelBuilderBase modelBuilder, [NotNull] string text)
			: base(item, applicationController)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNullOrEmpty(text, nameof(text));

			_modelBuilder = modelBuilder;
			Text = text;
		}

		public sealed override Image Image => Resources.Find;

		[NotNull]
		public override string Text { get; }

		protected override bool EnabledCore => ! ApplicationController.HasPendingChanges;

		protected sealed override void ExecuteCore()
		{
			E entity = FindEntity(ApplicationController.Window);

			if (entity == null)
			{
				return;
			}

			ApplicationController.GoToItem(entity);
		}

		[CanBeNull]
		private E FindEntity([NotNull] IWin32Window window)
		{
			List<T> tableRows = _modelBuilder.ReadOnlyTransaction(
				GetTableRows).ToList();

			var finder = new Finder<T>();
			T result = finder.ShowDialog(window, tableRows);

			return (E) result?.Entity;
		}

		protected abstract IEnumerable<T> GetTableRows();
	}
}
