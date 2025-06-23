using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AGP.QA;

public interface ITransformerQueryService
{
	IAsyncEnumerable<List<object[]>> QueryRows(
		[NotNull] TransformerConfiguration transformerConfiguration,
		[NotNull] IDictionary<string, Datastore> dataStoreByWorkspaceId,
		[CanBeNull] Geometry searchGeometry,
		[CanBeNull] string subFields,
		[CanBeNull] string whereClause);

	TransformerConfiguration CreateTransformerConfiguration(
		[NotNull] string canonicalDescriptorName,
		[NotNull] string transformerName);
}
