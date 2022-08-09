namespace ProSuite.DomainModel.Core.QA.VerificationProgress
{
	public enum VerificationProgressType
	{
		Undefined = -1,
		PreProcess = 0,
		ProcessNonCache = 1,
		ProcessContainer = 2,
		Error = 3,
		ProcessParallel = 4
	}
}
