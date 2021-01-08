using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container
{
	[CLSCompliant(false)]
	public interface IRelatedTablesProvider
	{
		[CanBeNull]
		RelatedTables GetRelatedTables([NotNull] IRow row);
	}
}
