namespace ProSuite.QA.Tests.ParameterTypes
{
	/// <summary>
	/// Describes which orphan node related tests are done in <see cref="QaOrphanNode"/>
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
		/// Tests if a line without connected points is found
		/// </summary>
		EndPointWithoutPoint = 3
	}
}
