using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	/// <summary>
	/// Indicates if datasets or rows are editable in the given context.
	/// </summary>
	public interface IDatasetEditContext
	{
		bool IsEditableInCurrentState([NotNull] IDdxDataset dataset);
	}
}
