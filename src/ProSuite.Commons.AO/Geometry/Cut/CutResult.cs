using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.Cut
{
	public class CutResult
	{
		public long? TargetOid { get; }
		public long? SourceOid { get; }
		public bool Success { get; }
		public string Msg { get; }
		public double? MinSizeAbs { get; }
		public double? MinSizePercent { get; }
		public int ResultGeometryCount { get; }

		public CutResult(
			[CanBeNull] long? targetOid,
			[CanBeNull] long? sourceOid,
			bool success,
			string msg,
			int resultGeometryCount = 0,
			double? minSizeAbs = null,
			double? minSizePercent = null)
		{
			TargetOid = targetOid;
			SourceOid = sourceOid;
			Success = success;
			Msg = msg;
			MinSizeAbs = minSizeAbs;
			MinSizePercent = minSizePercent;
			ResultGeometryCount = resultGeometryCount;
		}
	}
}
