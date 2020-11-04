namespace ProSuite.Commons.AO.Geodatabase
{
	public enum GeometricNetworkConnectOption
	{
		/// <summary>
		/// The feature will be disconnected from it's connected neighbours and reconnected after the store.
		/// The feature will be connected to features where the edge endpoint is coincident with a junction 
		/// or another edge end point.
		/// Use this value in the following situations
		/// - end points are not changed (e.g. merging lines)
		/// - in a later operation the 'correct' junction will also be updated and moved to the line end's 
		///   location. This avoids creating extra non-orphan junctions.
		/// </summary>
		DisconnectAndReconnect,

		/// <summary>
		/// Works the same as DisconnectAndReconnect with the addition that if there is no previous junction
		/// at the location of the new edge's end point a default junction will be created rather than an
		/// orphan junction. If no default junction type other than the orphan junction class is set in the 
		/// geometric network connectivity this is the same as DisconnectAndReconnect because ArcGIS 
		/// always ensures that there is an orphan junction.
		/// </summary>
		DisconnectAndReconnectEnsuringDefaultJunction,

		/// <summary>
		/// Destroy and Rebuild and Open-Jaw Reshape behaviour: Disconnects those endpoints with
		/// - different end point position in the new geometry
		/// - a different edge or junction at the new end point's position, except if the original 
		///   simple junction has no other connected edges when the new end point is on an edge
		/// </summary>
		DisconnectRelocatedEndPoints,

		/// <summary>
		/// The feature will maintain its connectivity with adjacent features. If necessary the adjacent 
		/// segment of connected features will be stretched to make sure that the geometric connectivity 
		/// is also maintained.
		/// </summary>
		MaintainConnectivityStretchLastSegments,

		/// <summary>
		/// The feature will maintain its connectivity. If necessary the adjacent polylines of connected features
		/// will be stretched to make sure that the geometric connectivity is also maintained. NOTE: The entire
		/// adjacent geometry will be distorted. If in doubt use the option MaintainConnectivityStretchLastSegments.
		/// </summary>
		MaintainConnectivityStretchEntireGeometries,

		/// <summary>
		/// If the feature has been newly created calling Disconnect would result in an exception. This option
		/// connects the feature after the shape was set  and is equivalent to 
		/// <cref name="MaintainConnectivityStretchLastSegments"/>).
		/// </summary>
		NewFeature
	}
}
