using System;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public class TargetBufferOptions
	{
		[CLSCompliant(false)]
		public TargetBufferOptions(double bufferDistance,
		                           double bufferMinimumSegmentLength,
		                           [CanBeNull] IZSettingsModel zSettingsModel = null)
		{
			BufferDistance = bufferDistance;
			BufferMinimumSegmentLength = bufferMinimumSegmentLength;
			ZSettingsModel = zSettingsModel;
		}

		[CLSCompliant(false)]
		public TargetBufferOptions([NotNull] IReshapeAlongOptions reshapeAlongOptions,
		                           [CanBeNull] IZSettingsModel zSettingsModel = null)
			: this(
				reshapeAlongOptions.BufferTarget
					? reshapeAlongOptions.TargetBufferDistance
					: 0,
				reshapeAlongOptions.EnforceMinimumBufferSegmentLength
					? reshapeAlongOptions.MinimumBufferSegmentLength
					: -1,
				zSettingsModel) { }

		public bool BufferTarget => BufferDistance > 0;

		public double BufferDistance { get; }

		public bool EnforceMinimumBufferSegmentLength => BufferMinimumSegmentLength > 0;

		public double BufferMinimumSegmentLength { get; }

		[CLSCompliant(false)]
		[CanBeNull]
		public IZSettingsModel ZSettingsModel { get; }

		public int LogInfoPointThreshold { get; set; } = 10000;
	}
}
