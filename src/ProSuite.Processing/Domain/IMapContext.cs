namespace ProSuite.Processing.Domain
{
	/// <summary>
	/// Essential map properties in a framework-neutral way
	/// </summary>
	public interface IMapContext
	{
		double ReferenceScale { get; }
	}
}