using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.DataProvider;

public interface IGdbDataEncoder
{
	/// <summary>
	/// Encodes the rows matching the filter into one or more <see cref="GdbData"/> batches.
	/// A new batch is started whenever <paramref name="maxRowsPerBatch"/> rows have been
	/// packed or the estimated serialized size of the current batch reaches
	/// <paramref name="maxBatchSizeBytes"/>. All but the last batch have
	/// <see cref="GdbData.HasMoreData"/> set to <c>true</c>.
	/// </summary>
	/// <param name="proTable">The table to read.</param>
	/// <param name="filter">The optional query filter.</param>
	/// <param name="classHandle">The class handle to stamp onto the resulting objects.</param>
	/// <param name="maxRowsPerBatch">The maximum number of rows per batch, or -1 for no limit.</param>
	/// <param name="maxBatchSizeBytes">The maximum estimated serialized size per batch in bytes,
	/// or a value &lt;= 0 for no limit.</param>
	[NotNull]
	IEnumerable<GdbData> Encode([NotNull] Table proTable,
	                            [CanBeNull] QueryFilter filter,
	                            long classHandle,
	                            int maxRowsPerBatch,
	                            long maxBatchSizeBytes);
}
