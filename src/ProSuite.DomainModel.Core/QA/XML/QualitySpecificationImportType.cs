namespace ProSuite.DomainModel.Core.QA.Xml
{
	public enum QualitySpecificationImportType
	{
		/// <summary>
		/// Existing specifications are updated and new specifications are added
		/// </summary>
		UpdateOrAdd,

		/// <summary>
		/// Only existing specifications are updated, if there is a specification in 
		/// the import file that is not in the selected list of target specifications,
		/// then it is not imported.
		/// </summary>
		UpdateOnly
	}
}
