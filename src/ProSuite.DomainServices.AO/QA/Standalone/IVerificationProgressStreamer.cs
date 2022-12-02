namespace ProSuite.DomainServices.AO.QA.Standalone
{
	public interface IVerificationProgressStreamer
	{
		void Warning(string text);

		void Info(string text);
	}
}
