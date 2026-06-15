using ArcGIS.Core.Data;

namespace ProSuite.Commons.AGP.Gdb;

/// <summary>
/// To avoid using EditOperation.IEditContext from
/// ArcGIS.Desktop.Editing.dll in unit tests.
/// </summary>
public interface IEditOperationContext
{
	void Invalidate(Row row);

	void Invalidate(Dataset dataset);
}
