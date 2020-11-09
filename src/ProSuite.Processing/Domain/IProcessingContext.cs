using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.Domain
{
	public interface IProcessingContext
	{
		Geodatabase Geodatabase { get; } // TODO really needed? cf OpenDataset()

		[CanBeNull]
		IMapContext MapContext { get; }

		ProcessSelectionType SelectionType { get; }

		ProcessExecutionType ExecutionType { get; }

		ProcessingDataset OpenDataset(ProcessDatasetName name);

		[CanBeNull]
		Polygon GetProcessingPerimeter();

		bool AllowModification(Feature feature, out string reason);

		void SetSystemFields(Row row, Table table);

		/// <remarks>Honors <see cref="SelectionType"/></remarks>
		IEnumerable<Feature> GetInputFeatures(ProcessingDataset dataset, Geometry extent = null,
		                                      bool recycling = false);

		/// <remarks>Honors <see cref="SelectionType"/></remarks>
		int CountInputFeatures(ProcessingDataset dataset, Geometry extent = null);

		/// <remarks>Ignores selection type, applies only given filter criteria</remarks>
		IEnumerable<Feature> GetOtherFeatures(ProcessingDataset dataset,
		                                 Geometry extent = null, bool recycling = false);

		/// <remarks>Ignores selection type, applies only given filter criteria</remarks>
		int CountOtherFeatures(ProcessingDataset dataset, Geometry extent = null);
	}
}
