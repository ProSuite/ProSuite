using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	[CLSCompliant(false)]
	public interface ICurrentModelContext
	{
		[CanBeNull]
		Dataset GetDataset([NotNull] string gdbDatasetName,
		                   [NotNull] IWorkspaceName workspaceName);

		[CanBeNull]
		Association GetAssociation([NotNull] string relationshipClass,
		                           [NotNull] IWorkspaceName workspaceName);
	}
}
