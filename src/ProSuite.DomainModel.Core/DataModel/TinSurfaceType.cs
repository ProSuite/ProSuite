namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// A basic TIN surface type that corresponds with esriTinSurfaceType.
	/// The additional surface types not modelled here could in principle be
	/// supported also.
	/// </summary>
	public enum TinSurfaceType
	{
		HardLine = 1,
		HardClip = 2,
		HardErase = 3,
		HardReplace = 4,

		SoftLine = 9,
		SoftClip = 10,
		SoftErase = 11,
		SoftReplace = 12,

		MassPoint = 18,

		//
		// Custom types:
		/// <summary>
		/// The point cloud archive is a catalog for LAS files which shall be used as mass points.
		/// </summary>
		LasPointCloud = 20
	}
}
