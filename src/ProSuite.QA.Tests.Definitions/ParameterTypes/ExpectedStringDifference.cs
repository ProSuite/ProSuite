namespace ProSuite.QA.Tests
{
	public enum ExpectedStringDifference
	{
		/// <summary>
		/// Allow all differences
		/// </summary>
		Any = 0,

		/// <summary>
		/// Require a case-sensitive difference
		/// </summary>
		CaseSensitiveDifference = 1,

		/// <summary>
		/// Require a case-insensitive difference -> case-only difference is not allowed
		/// </summary>
		CaseInsensitiveDifference = 2
	}
}
