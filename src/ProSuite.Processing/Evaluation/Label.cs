namespace ProSuite.Processing.Evaluation
{
	/// <summary>
	/// This is really just a guise for an integer, but
	/// it clarifies the code and helps prevent mistakes.
	/// </summary>
	public readonly struct Label
	{
		public readonly int Value;

		public Label(int value)
		{
			Value = value;
		}

		public override string ToString()
		{
			return string.Format("Label #{0}", Value);
		}
	}
}
