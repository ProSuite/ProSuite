namespace ProSuite.DomainModel.Core.DataModel
{
	public enum ErrorType
	{
		// must correspond to Coded Domain TLM_CORRECTION_ERRORTYPE_CD
		Hard = 100,
		Soft = 200,
		Allowed = 300
	}
}