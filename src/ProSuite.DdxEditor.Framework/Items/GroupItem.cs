using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Properties;
using ProSuite.DdxEditor.Framework.TableRows;

namespace ProSuite.DdxEditor.Framework.Items
{
	public abstract class GroupItem : Item
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="GroupItem"/> class.
		/// </summary>
		/// <param name="text">The text.</param>
		protected GroupItem([NotNull] string text) : base(text) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="GroupItem"/> class.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="description">The description.</param>
		protected GroupItem([NotNull] string text,
		                    [CanBeNull] string description) : base(text, description) { }

		#endregion

		public override bool IsNew => false;

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			// TODO pass in sort state
			const bool hideGridLines = true;
			return CreateTableControl(GetChildRows, itemNavigation, hideGridLines);
		}

		public override Image Image => Resources.GroupItem;

		public override Image SelectedImage => Resources.GroupItemSelected;

		[NotNull]
		private IEnumerable<ItemRow> GetChildRows()
		{
			return Children.Select(child => new ItemRow(child));
		}
	}
}
