namespace ProSuite.Commons.Misc
{
	public interface IContextAware
	{
		void SetContext(object context);

		object GetContext();
	}
}