using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using Radzen;
using Radzen.Blazor;

namespace ProSuite.DdxEditor.Content.Blazor;

public static class BlazorDataGridUtils
{
	public static bool Contains([NotNull] this PagedDataBoundComponent<ViewModelBase> grid,
	                            [CanBeNull] ViewModelBase row)
	{
		return row != null && grid.Data.Contains(row);
	}

	public static async Task Edit([NotNull] List<RadzenDataGrid<ViewModelBase>> grids,
	                              [NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(grids, nameof(grids));
		Assert.ArgumentNotNull(row, nameof(row));

		foreach (RadzenDataGrid<ViewModelBase> grid in grids)
		{
			if (! Contains(grid, row))
			{
				continue;
			}
			
			await grid.EditRow(row);
		}
	}
}
