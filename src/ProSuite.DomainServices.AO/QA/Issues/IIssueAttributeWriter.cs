using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	[CLSCompliant(false)]
	public interface IIssueAttributeWriter
	{
		void WriteAttributes([NotNull] Issue issue, [NotNull] IRowBuffer rowBuffer);
	}
}
