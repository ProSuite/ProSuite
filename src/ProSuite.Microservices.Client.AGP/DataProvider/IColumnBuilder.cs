using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.DataProvider;

public interface IColumnBuilder
{
	void AddValue(object value);

	void AddNull();

	void FinalizeColumn(out Column columnMsg);

	/// <summary>
	/// The estimated serialized size (in bytes) of the values accumulated so far.
	/// Used to enforce size-capped batching without materializing the column message.
	/// </summary>
	long EstimatedByteSize { get; }
}
