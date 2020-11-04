namespace ProSuite.Processing.Evaluation
{
	public readonly struct Function
	{
		public readonly string Name;

		public Function(string name)
		{
			Name = name;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
