using ProSuite.Commons.Geom;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.CreateBufferedLine
{
	public class PartialCreateBufferedLineOptions : PartialOptionsBase
	{
		#region Overridable Settings

		public OverridableSetting<double> BufferWidth { get; set; }

		public OverridableSetting<bool> ShowBufferDistanceCircle { get; set; }

		public OverridableSetting<bool> Weed { get; set; }

		public OverridableSetting<double> WeedTolerance { get; set; }

		public OverridableSetting<bool> EnforceMinimumSegmentLength { get; set; }

		public OverridableSetting<double> MinimumSegmentLength { get; set; }

		public OverridableSetting<BufferSide> BufferSide { get; set; }

		#endregion

		public override PartialOptionsBase Clone()
		{
			var result = new PartialCreateBufferedLineOptions
			             {
				             BufferWidth = TryClone(BufferWidth),
				             ShowBufferDistanceCircle = TryClone(ShowBufferDistanceCircle),
				             Weed = TryClone(Weed),
				             WeedTolerance = TryClone(WeedTolerance),
				             EnforceMinimumSegmentLength = TryClone(EnforceMinimumSegmentLength),
				             MinimumSegmentLength = TryClone(MinimumSegmentLength),
				             BufferSide = TryClone(BufferSide)
			             };
			return result;
		}
	}
}
