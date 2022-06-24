using System.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Blazor.ViewModel;
using Radzen.Blazor;

namespace ProSuite.DdxEditor.Content.Blazor;

public static class DataGridUtils
{
	public static async Task EditRow([NotNull] RadzenDataGrid<ViewModelBase> grid,
	                                 [NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(grid, nameof(grid));
		Assert.ArgumentNotNull(row, nameof(row));

		row.StartEditing();
		await grid.EditRow(row);
	}

	public static async Task UpdateRow([NotNull] RadzenDataGrid<ViewModelBase> grid,
	                                   [NotNull] ViewModelBase row)
	{
		Assert.ArgumentNotNull(grid, nameof(grid));
		Assert.ArgumentNotNull(row, nameof(row));

		row.StopEditing();
		await grid.UpdateRow(row);
	}

	public static async Task UpdateRowIfNotNull([CanBeNull] RadzenDataGrid<ViewModelBase> grid,
	                                            [CanBeNull] ViewModelBase row)
	{
		if (grid == null)
		{
			return;
		}

		if (row == null)
		{
			return;
		}

		row.StopEditing();
		await grid.UpdateRow(row);
	}
}
