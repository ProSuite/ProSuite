namespace ProSuite.Commons.AO.Geometry
{
	public interface IArrayProvider<T>
	{
		T[] GetArray(int requiredLength);

		bool SupportsMultithreading { get; }
	}
}
