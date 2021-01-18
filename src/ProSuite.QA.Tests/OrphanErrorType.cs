namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Describes with orpan noce related tests are done in OrphanNodeTest
	/// </summary>
	public enum OrphanErrorType
	{
		/// <summary>
		/// Both OrphanedPoint and EndPointWithoutPoint tests are performed
		/// </summary>
		Both = 1,

		/// <summary>
		/// Tests if a point without connected lines is found
		/// </summary>
		OrphanedPoint = 2,

		/// <summary>
		/// tests if line without connected points is found
		/// </summary>
		EndPointWithoutPoint = 3
	}
}
