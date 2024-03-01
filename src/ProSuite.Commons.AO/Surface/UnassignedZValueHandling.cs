namespace ProSuite.Commons.AO.Surface
{
	public enum UnassignedZValueHandling
	{
		/// <summary>
		/// If for any of the vertices no valid Z value can be calculated because
		/// no raster is available a the respective location, null shall be returned
		/// for the <see cref="ISimpleSurface.Drape"/> and <see cref="ISimpleSurface.SetShapeVerticesZ"/> methods.
		/// This is consistent with the ArcObjects behaviour.
		/// </summary>
		ReturnNullGeometryIfNotCompletelyCovered,
		IgnoreVertex,
		SetDefaultValueForUnassignedZs
	}
}
