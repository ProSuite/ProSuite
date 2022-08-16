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
	public static async Task Edit([NotNull] this RadzenDataGrid<ViewModelBase> grid,
	                              [NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(grid, nameof(grid));
		Assert.ArgumentNotNull(row, nameof(row));

		if (! Contains(grid, row))
		{
			return;
		}

		row.StartEditing();
		await grid.EditRow(row);
	}

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

			row.StartEditing();
			await grid.EditRow(row);
		}
	}

	public static async Task Update([NotNull] RadzenDataGrid<ViewModelBase> grid,
	                                [NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(grid, nameof(grid));
		Assert.ArgumentNotNull(row, nameof(row));

		if (! Contains(grid, row))
		{
			return;
		}

		row.StopEditing();
		await grid.UpdateRow(row);
	}

	public static async Task Update([NotNull] List<RadzenDataGrid<ViewModelBase>> grids,
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

			row.StopEditing();

			await grid.UpdateRow(row);
		}
	}

	public static async Task UpdateIfNotNull([NotNull] this RadzenDataGrid<ViewModelBase> grid,
	                                         [CanBeNull] ViewModelBase row)
	{
		if (row == null)
		{
			return;
		}

		if (! Contains(grid, row))
		{
			return;
		}

		row.StopEditing();
		await grid.UpdateRow(row);
	}

	public static async Task UpdateIfNotNull([NotNull] List<RadzenDataGrid<ViewModelBase>> grids,
	                                         [CanBeNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(grids, nameof(grids));

		foreach (RadzenDataGrid<ViewModelBase> grid in grids)
		{
			if (grid == null)
			{
				continue;
			}

			if (row == null)
			{
				continue;
			}

			if (! Contains(grid, row))
			{
				continue;
			}

			row.StopEditing();
			await grid.UpdateRow(row);
		}
	}

	public static async void Insert([NotNull] List<RadzenDataGrid<ViewModelBase>> grids,
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

			await grid.Reload();
		}
	}
}
