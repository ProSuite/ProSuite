using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel
{
	[CLSCompliant(false)]
	public interface IDatasetListBuilder
	{
		bool IgnoreDataset([NotNull] IDatasetName datasetName,
		                   [NotNull] out string reason);

		void UseDataset([NotNull] IDatasetName name);

		void AddDatasets([NotNull] Model model);
	}
}
