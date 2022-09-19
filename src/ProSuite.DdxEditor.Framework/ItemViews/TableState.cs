using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public class TableState
	{
		[NotNull] private readonly HashSet<int> _entitySelection = new HashSet<int>();

		public bool FilterRows { get; set; }

		public string FindText { get; set; }

		public DataGridViewSortState TableSortState { get; set; }

		public bool MatchCase { get; set; }

		public int FirstDisplayedScrollingRowIndex { get; set; }

		public int FirstDisplayedScrollingColumnIndex { get; set; }

		public void ClearEntitySelection()
		{
			_entitySelection.Clear();
		}

		public int SelectedEntityCount => _entitySelection.Count;

		public void AddSelectedEntity([CanBeNull] Entity entity)
		{
			if (entity == null)
			{
				return;
			}

			_entitySelection.Add(entity.Id);
		}

		public bool IsSelected([CanBeNull] Entity entity)
		{
			return entity != null && _entitySelection.Contains(entity.Id);
		}
	}
}
