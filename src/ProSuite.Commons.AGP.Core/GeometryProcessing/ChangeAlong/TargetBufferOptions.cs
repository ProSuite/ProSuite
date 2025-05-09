using ArcGIS.Core.Geometry;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong
{
	public class TargetBufferOptions
	{
		public TargetBufferOptions(double bufferDistance = 0,
		                           double bufferMinimumSegmentLength = 0)
		{
			BufferDistance = bufferDistance;
			BufferMinimumSegmentLength = bufferMinimumSegmentLength;
		}

		public bool BufferTarget => BufferDistance > 0;

		public double BufferDistance { get; }

		public bool EnforceMinimumBufferSegmentLength => BufferMinimumSegmentLength > 0;

		public double BufferMinimumSegmentLength { get; }

		public IZSettingsModel ZSettingsModel { get; set; }
	}

	public interface IZSettingsModel
	{
		public ZMode CurrentMode { get; }

		Multipart ApplyUndefinedZs(Multipart geometry);
	}

	public enum ZMode
	{
		None,
		Targets,
		Extrapolate,
		Interpolate,
		Dtm,
		ConstantZ,
		Offset
	}
}
