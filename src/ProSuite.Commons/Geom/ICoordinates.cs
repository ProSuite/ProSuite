namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Represents a set of three-dimensional coordinates in space.
	/// 2D coordinates are represented by returning double.NaN for Z.
	/// </summary>
	/// <remarks>
	///	The interface provides a common, most simple contract for all objects that represent or operate on a point in space.
	/// The idea is to implement primitive geometric operations against this interface, to get maximum flexibility.
	/// Currently, it also implements <see cref="IBoundedXY"/> to allow for bounding box operations.
	/// <para>
	/// <strong>Future Considerations:</strong><br></br>
	/// In the future, we may want to reconsider where this interface should lie in the hierarchy. And which other interfaces it should extend/implement.
	///
	/// Any other dimensions (e.g., m, t, or other) are knowingly not supported by this interface, since their type is not
	/// necessarily a double. In contrast to IPnt, this interface does also not allow for access via index, which is an implementation detail that
	/// should not be exposed on this level.  
	/// </para>
	/// </remarks>
	public interface ICoordinates : IBoundedXY
	{
		double X { get; set; }
		double Y { get; set; }
		double Z { get; set; }
	}
}
