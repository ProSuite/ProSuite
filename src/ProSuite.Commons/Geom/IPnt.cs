namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// More specific interface for a point in space than <see cref="ICoordinates"/>. Extends both <see cref="ICoordinates"/> and <see cref="IGmtry"/>.
	/// </summary>
	/// <remarks>
	/// Grown over time.
	/// <para>
	/// <strong>Future Considerations:</strong><br></br>
	/// We may want to reconsider the design of this interface and its position in the hierarchy.
	/// Probably we don't want to expose the indexer here, since it is an implementation detail.
	/// Moreover, <c>Clone()</c> might need to be pushed to <see cref="IGmtry"/> or a more general interface.
	/// </para>
	/// </remarks>
	public interface IPnt : ICoordinates, IGmtry
	{
		double this[int index] { get; set; }

		IPnt Clone();
	}
}
