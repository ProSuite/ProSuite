namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// Policy for allocating points to a tile that lie exactly on the tile boundary
	/// Example: BorderPointTileAllocationPolicy.TopLeft: Points that are on the top
	/// or left boundary of a specific tile are assigned to that tile. Points on the
	/// bottom or right tile boundary fall outside this tile.
	/// </summary>
	/// <remarks>Used in db mapping, don't alter values</remarks>
	public enum BorderPointTileAllocationPolicy
	{
		TopLeft = 0,
		TopRight = 1,
		BottomLeft = 2,
		BottomRight = 3
	}
}
