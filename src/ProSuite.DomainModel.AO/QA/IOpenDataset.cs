using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public interface IOpenDataset
	{
		bool CanOpen([NotNull] IDdxDataset dataset);

		[CanBeNull]
		object OpenDataset([NotNull] IDdxDataset dataset,
		                   [NotNull] Type dataType);

		[CanBeNull]
		IRelationshipClass OpenRelationshipClass([NotNull] Association association);
	}
}
