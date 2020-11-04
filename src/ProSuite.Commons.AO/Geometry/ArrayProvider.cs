using System.Threading;

namespace ProSuite.Commons.AO.Geometry
{
	/// <summary>
	/// Thread-safe implementation of an array provider. This class should be used
	/// for re-using one large array rather than repeatedly re-allocating memory on
	/// the large-object-heap and hence fragmenting the process' memory.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ArrayProvider<T> : IArrayProvider<T>
	{
		private static ThreadLocal<T[]> _threadLocalArray;

		public T[] GetArray(int requiredLength)
		{
			if (_threadLocalArray == null)
			{
				const int neverUsed = 100;
				_threadLocalArray = new ThreadLocal<T[]>(() => new T[neverUsed]);
			}

			if (! _threadLocalArray.IsValueCreated ||
			    _threadLocalArray.Value.Length < requiredLength)
			{
				const double margin = 1.75;
				_threadLocalArray.Value = new T[(int) (requiredLength * margin)];
			}

			return _threadLocalArray.Value;
		}

		public bool SupportsMultithreading => true;
	}
}
