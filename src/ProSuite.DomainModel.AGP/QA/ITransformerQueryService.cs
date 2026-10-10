using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AGP.QA;

public interface ITransformerQueryService
{
	/// <summary>
	/// When true, data is always read via the client (on the MCT), so uncommitted edits are
	/// included. Set while a work-unit edit session is active so routes reflect edits before
	/// they are saved; otherwise the faster server-side / cached path is used.
	/// </summary>
	bool AlwaysUseClientData { get; set; }

	/// <summary>
	/// Reads the transformed (joined) rows for a route query. On timeout or a denied
	/// client-data request the read is truncated; the implementation then throws
	/// <see cref="RouteQueryTimeoutException"/> so the caller cannot mistake a partial result
	/// for a complete one.
	/// </summary>
	/// <param name="queryTimeoutSeconds">Per-call timeout override. <c>null</c> uses the
	/// service default (short guard for redraw/peek queries); a non-positive value or
	/// <c>NaN</c> means no cap, used for the initial full-network build so it can finish.</param>
	IEnumerable<object[]> QueryRows(
		[NotNull] TransformerConfiguration transformerConfiguration,
		[NotNull] IDictionary<int, Datastore> dataStoreByModelId,
		[CanBeNull] Geometry searchGeometry,
		[CanBeNull] string subFields,
		[CanBeNull] string whereClause,
		double? queryTimeoutSeconds = null);

	TransformerConfiguration CreateTransformerConfiguration(
		[NotNull] string canonicalDescriptorName,
		[NotNull] string transformerName);
}
