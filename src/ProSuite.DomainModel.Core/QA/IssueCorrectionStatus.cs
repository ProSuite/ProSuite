namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// Correction status, should correspond to the issue correction domain in the issue.gdb
	/// This could potentially go to a future DomainServices.Core project with all the other issue
	/// persistence basics.
	/// </summary>
	public enum IssueCorrectionStatus
	{
		NotCorrected = 100,
		Corrected = 200
	}
}
