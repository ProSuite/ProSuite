using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;

namespace ProSuite.Commons.AGP.Gdb;

/// <summary>
/// To avoid using EditOperation.IEditContext from
/// ArcGIS.Desktop.Editing.dll in unit tests.
/// </summary>
public class EditOperationContext : IEditOperationContext
{
	private readonly EditOperation.IEditContext _context;

	public EditOperationContext(EditOperation.IEditContext context)
	{
		_context = context;
	}

	public void Invalidate(Row row)
	{
		_context.Invalidate(row);
	}

	public void Invalidate(Dataset dataset)
	{
		_context.Invalidate(dataset);
	}
}
