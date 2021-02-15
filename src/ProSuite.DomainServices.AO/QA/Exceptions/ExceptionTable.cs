using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionTable : ExceptionDataset, IExceptionTable
	{
		public ExceptionTable([NotNull] IObjectClass objectClass,
		                      [NotNull] IIssueTableFields fields,
		                      int exceptionCount)
			: base(objectClass, fields, exceptionCount) { }
	}
}
