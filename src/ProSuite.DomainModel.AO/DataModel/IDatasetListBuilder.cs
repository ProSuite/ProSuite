using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IDatasetListBuilder
	{
		bool IgnoreDataset([NotNull] IDatasetName datasetName,
		                   [NotNull] out string reason);

		void UseDataset([NotNull] IDatasetName name);

		void AddDatasets([NotNull] DdxModel model);
	}
}
