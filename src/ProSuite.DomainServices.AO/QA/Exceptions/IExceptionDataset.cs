using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public interface IExceptionDataset
	{
		[NotNull]
		IObjectClass ObjectClass { get; }

		int ExceptionCount { get; }

		[NotNull]
		IIssueTableFields Fields { get; }
	}
}
