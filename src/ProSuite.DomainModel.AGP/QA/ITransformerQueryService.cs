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

	IEnumerable<object[]> QueryRows(
		[NotNull] TransformerConfiguration transformerConfiguration,
		[NotNull] IDictionary<int, Datastore> dataStoreByModelId,
		[CanBeNull] Geometry searchGeometry,
		[CanBeNull] string subFields,
		[CanBeNull] string whereClause);

	TransformerConfiguration CreateTransformerConfiguration(
		[NotNull] string canonicalDescriptorName,
		[NotNull] string transformerName);
}
