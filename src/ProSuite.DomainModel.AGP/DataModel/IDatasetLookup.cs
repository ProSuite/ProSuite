using System;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel;

[Obsolete("Use ProjectWorkspace.GetDataset or different implementation")]
public interface IDatasetLookup
{
	IDdxDataset GetDataset([NotNull] Table table);

	T GetDataset<T>(Table table) where T : IDdxDataset;
}
