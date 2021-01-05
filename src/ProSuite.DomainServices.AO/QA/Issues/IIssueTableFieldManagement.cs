using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	[CLSCompliant(false)]
	public interface IIssueTableFieldManagement : IIssueTableFields
	{
		[NotNull]
		IEnumerable<IField> CreateFields();

		[ContractAnnotation("optional : false => notnull")]
		IField CreateField(IssueAttribute attribute, bool optional = false);
	}
}
