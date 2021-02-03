using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	[CLSCompliant(false)]
	public interface IVerifiedModelFactory
	{
		[NotNull]
		Model CreateModel([NotNull] IWorkspace workspace,
		                  [NotNull] string name,
		                  [CanBeNull] ISpatialReference spatialReference,
		                  [CanBeNull] string databaseName,
		                  [CanBeNull] string schemaOwner);
	}
}
