using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.Domain;

namespace ProSuite.Processing.AGP.Core.Domain;

public interface IProcessingContext
{
	[CanBeNull]
	IMapContext MapContext { get; }

	ProcessSelectionType SelectionType { get; }

	ProcessExecutionType ExecutionType { get; }

	bool WantCustomDrawingOutlineCode { get; } // TODO consider move to separate ProcessingOptions object/interface (which may have other options)

	ProcessingDataset OpenDataset(ProcessDatasetName name);

	RelationshipClass OpenAssociation(string name, Table endpoint);

	[CanBeNull]
	Polygon GetProcessingPerimeter(); // TODO always in Map's SRef (by spec/document)

	T ToMapSRef<T>(T geometry) where T : Geometry;

	bool AllowModification(Feature feature, out string reason);

	void SetSystemFields(Row row, Table table);

	/// <summary>Notify the system that the given row has changed</summary>
	void Invalidate(Row row);

	/// <summary>Notify the system that the given relationship has changed</summary>
	void Invalidate(Relationship relationship);

	/// <remarks>Honors <see cref="SelectionType"/></remarks>
	IEnumerable<Feature> GetInputFeatures(
		ProcessingDataset dataset, Geometry extent = null, bool recycling = false);

	/// <remarks>Honors <see cref="SelectionType"/></remarks>
	long CountInputFeatures(ProcessingDataset dataset, Geometry extent = null);

	/// <remarks>Ignores selection type, applies only given filter criteria</remarks>
	IEnumerable<Feature> GetOtherFeatures(
		ProcessingDataset dataset, Geometry extent = null, bool recycling = false);

	/// <remarks>Ignores selection type, applies only given filter criteria</remarks>
	long CountOtherFeatures(ProcessingDataset dataset, Geometry extent = null);
}
