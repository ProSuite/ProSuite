namespace ProSuite.Commons.ManagedOptions
{
	public class DatasetSpecificValue<T> where T : struct
	{
		public string Dataset { get; set; }
		public T Value { get; set; }

		public DatasetSpecificValue() { }

		public DatasetSpecificValue(string dataset, T value)
		{
			Dataset = dataset;
			Value = value;
		}

		public DatasetSpecificValue<T> Clone()
		{
			return new DatasetSpecificValue<T>(Dataset, Value);
		}
	}
}