namespace ProSuite.QA.Tests
{
	public enum LineFieldValuesConstraint
	{
		NoConstraint = 0,
		AllEqual = 1,
		AllEqualOrValidPointExists = 2,
		AtLeastTwoDistinctValuesIfValidPointExists = 3,
		UniqueOrValidPointExists = 4
	}
}
