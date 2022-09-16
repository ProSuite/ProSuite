namespace ProSuite.QA.Tests.Transformers
{
	public class CalculatedValue
	{
		public CalculatedValue(int targetIndex, object value)
		{
			TargetIndex = targetIndex;
			Value = value;
		}

		public object Value { get; set; }
		public int TargetIndex { get; set; }
	}
}
