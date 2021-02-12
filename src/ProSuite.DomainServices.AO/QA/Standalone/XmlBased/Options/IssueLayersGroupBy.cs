namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public enum IssueLayersGroupBy
	{
		/// <summary>
		/// All quality condition issue layers are added in a flat list
		/// </summary>
		None,

		/// <summary>
		/// Quality condition issue layers are grouped by issue type (Warning or Error) 
		/// </summary>
		IssueType,

		/// <summary>
		/// Issue layers for quality conditions assigned to a category are included in a hierarchy of 
		/// group layers reflecting the category structure. Issue layers within a category, as well as 
		/// issue layers not assigned to a category, are grouped by issue type (Warning and Error)
		/// </summary>
		CategoryAndIssueType,

		/// <summary>
		/// Same as CategoryAndIssueType, except that issue layers within a category are *not* grouped by issue type.
		/// Instead, they are listed directly in the group layer for the category.
		/// </summary>
		CategoryOrIssueType
	}
}
