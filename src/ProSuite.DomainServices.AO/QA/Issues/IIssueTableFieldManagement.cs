using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public interface IIssueTableFieldManagement : IIssueTableFields
	{
		[NotNull]
		IEnumerable<IField> CreateFields();

		[ContractAnnotation("optional : false => notnull")]
		IField CreateField(IssueAttribute attribute, bool optional = false);

		/// <summary>
		/// Creates the domains referenced in the field definitions. Make sure to call this
		/// method before creating the fields.
		/// </summary>
		/// <param name="featureWorkspace"></param>
		void CreateReferencedDomains([NotNull] IFeatureWorkspace featureWorkspace);
	}
}
