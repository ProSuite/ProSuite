namespace ProSuite.DomainModel.AO.Workflow
{
	/// <summary>
	/// Options for allowing quality verifications outside of an edit session
	/// </summary>
	/// <remarks>Numeric values are stored in the data dictionary, DO NOT CHANGE</remarks>
	public enum QualityVerificationOutsideEditSessionAllowed
	{
		Always = 0,
		OnlySingleUseGdb = 1,
		Never = 2
	}
}
