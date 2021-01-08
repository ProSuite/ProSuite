using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	[CLSCompliant(false)]
	public interface IModelContext : IDatasetEditContext,
	                                 IDatasetContext,
	                                 IWorkspaceContextLookup
	{
		bool IsPrimaryWorkspaceBeingEdited();

		[NotNull]
		IWorkspaceContext PrimaryWorkspaceContext { get; }

		[CanBeNull]
		IDdxDataset GetDataset([NotNull] IDatasetName datasetName, bool isValid);
	}
}