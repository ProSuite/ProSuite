using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.CreateBufferedLine
{
	[UsedImplicitly]
	public class CreateBufferedLineToolOptions
		: OptionsBase<PartialCreateBufferedLineOptions>
	{
		public const double DefaultBufferWidth = 2.5;
		public const double DefaultWeedTolerance = 0.1;
		public const double DefaultMinimumSegmentLength = 0.1;

		public CreateBufferedLineToolOptions(
			[CanBeNull] PartialCreateBufferedLineOptions centralOptions,
			[CanBeNull] PartialCreateBufferedLineOptions localOptions)
		{
			CentralOptions = centralOptions;
			LocalOptions = localOptions ?? new PartialCreateBufferedLineOptions();

			CentralizableBufferWidth = InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.BufferWidth),
				DefaultBufferWidth);

			CentralizableShowBufferDistanceCircle = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.ShowBufferDistanceCircle),
				false);

			CentralizableWeed = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.Weed),
				false);

			CentralizableWeedTolerance = InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.WeedTolerance),
				DefaultWeedTolerance);

			CentralizableEnforceMinimumSegmentLength = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.EnforceMinimumSegmentLength),
				false);

			CentralizableMinimumSegmentLength = InitializeSetting<double>(
				ReflectionUtils.GetProperty(() => LocalOptions.MinimumSegmentLength),
				DefaultMinimumSegmentLength);

			CentralizableBufferSide = InitializeSetting<BufferSide>(
				ReflectionUtils.GetProperty(() => LocalOptions.BufferSide),
				BufferSide.Both);
		}

		#region Centralizable Properties

		public CentralizableSetting<double> CentralizableBufferWidth { get; }

		public CentralizableSetting<bool> CentralizableShowBufferDistanceCircle { get; }

		public CentralizableSetting<bool> CentralizableWeed { get; }

		public CentralizableSetting<double> CentralizableWeedTolerance { get; }

		public CentralizableSetting<bool> CentralizableEnforceMinimumSegmentLength { get; }

		public CentralizableSetting<double> CentralizableMinimumSegmentLength { get; }

		public CentralizableSetting<BufferSide> CentralizableBufferSide { get; }

		#endregion

		#region Current Values

		public double BufferWidth
		{
			get => CentralizableBufferWidth.CurrentValue;
			set => CentralizableBufferWidth.CurrentValue = value;
		}

		public bool ShowBufferDistanceCircle => CentralizableShowBufferDistanceCircle.CurrentValue;

		public bool Weed => CentralizableWeed.CurrentValue;

		public double WeedTolerance => CentralizableWeedTolerance.CurrentValue;

		public bool EnforceMinimumSegmentLength =>
			CentralizableEnforceMinimumSegmentLength.CurrentValue;

		public double MinimumSegmentLength => CentralizableMinimumSegmentLength.CurrentValue;

		public BufferSide BufferSide => CentralizableBufferSide.CurrentValue;

		#endregion

		public override void RevertToDefaults()
		{
			// The buffer width is a very volatile setting and is deliberately not reverted.
			CentralizableShowBufferDistanceCircle.RevertToDefault();
			CentralizableWeed.RevertToDefault();
			CentralizableWeedTolerance.RevertToDefault();
			CentralizableEnforceMinimumSegmentLength.RevertToDefault();
			CentralizableMinimumSegmentLength.RevertToDefault();
			CentralizableBufferSide.RevertToDefault();
		}

		public override bool HasLocalOverrides(NotificationCollection notifications)
		{
			bool result = false;

			if (HasLocalOverride(CentralizableShowBufferDistanceCircle,
			                     "Show circle to indicate current buffer width", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableWeed,
			                     "Generalize segments", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableWeedTolerance,
			                     "Generalization tolerance", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableEnforceMinimumSegmentLength,
			                     "Enforce minimum segment length", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableMinimumSegmentLength,
			                     "Minimum segment length", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableBufferSide,
			                     "Buffer side", notifications))
			{
				result = true;
			}

			return result;
		}

		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Create Buffered Line Options";
			return GetLocalOverridesMessage(optionsName);
		}
	}
}
