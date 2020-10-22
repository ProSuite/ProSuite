using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.Commons.AO.Geometry.Cracking
{
	public sealed class ChopperToolOptions : OptionsBase<PartialChopperToolOptions>,
	                                         ICrackingOptions
	{
		// TODO: Move also the ICrackingOption and other stuff to this namespace
		public ChopperToolOptions([CanBeNull] PartialChopperToolOptions centralOptions,
		                          [CanBeNull] PartialChopperToolOptions localOptions)
		{
			Initialize(centralOptions, localOptions);
		}

		#region Centralizable Properties

		public CentralizableSetting<bool> CentralizableRespectMinimumSegmentLength
		{
			get;
			private set;
		}

		public CentralizableSetting<double> CentralizableMinimumSegmentLength { get; private set; }

		public CentralizableSetting<bool> CentralizableSnapToTargetVertices { get; private set; }

		public CentralizableSetting<double> CentralizableSnapTolerance { get; private set; }

		public CentralizableSetting<TargetFeatureSelection>
			CentralizableTargetFeatureSelection { get; private set; }

		public CentralizableSetting<bool>
			CentralizableExcludeInteriorInteriorIntersections { get; private set; }

		public CentralizableSetting<bool> CentralizableUseSourceZs { get; private set; }

		#endregion

		#region Partial Options Definitions

		#endregion

		#region Current Values

		public TargetFeatureSelection TargetFeatureSelection
		{
			get { return CentralizableTargetFeatureSelection.CurrentValue; }
		}

		public bool RespectMinimumSegmentLength
		{
			get { return CentralizableRespectMinimumSegmentLength.CurrentValue; }
		}

		public double MinimumSegmentLength
		{
			get { return CentralizableMinimumSegmentLength.CurrentValue; }
		}

		public bool SnapToTargetVertices
		{
			get { return CentralizableSnapToTargetVertices.CurrentValue; }
		}

		public double SnapTolerance
		{
			get { return CentralizableSnapTolerance.CurrentValue; }
		}

		public bool UseSourceZs
		{
			get { return CentralizableUseSourceZs.CurrentValue; }
		}

		public bool ExcludeInteriorInteriorIntersections
		{
			get { return CentralizableExcludeInteriorInteriorIntersections.CurrentValue; }
		}

		#endregion

		private void Initialize(PartialChopperToolOptions centralOptions,
		                        PartialChopperToolOptions localOptions)
		{
			CentralOptions = centralOptions;

			LocalOptions = localOptions ??
			               new PartialChopperToolOptions();

			CentralizableTargetFeatureSelection =
				InitializeSetting<TargetFeatureSelection>(
					ReflectionUtils.GetProperty(() => LocalOptions.TargetFeatureSelection),
					TargetFeatureSelection.SameClass);

			CentralizableRespectMinimumSegmentLength =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.RespectMinimumSegmentLength),
					false);
			CentralizableMinimumSegmentLength =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.MinimumSegmentLength), 0.0);

			CentralizableSnapToTargetVertices =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.SnapToTargetVertices), false);
			CentralizableSnapTolerance =
				InitializeSetting<double>(
					ReflectionUtils.GetProperty(() => LocalOptions.SnapTolerance),
					0.0);

			CentralizableUseSourceZs =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(() => LocalOptions.UseSourceZs), false);

			CentralizableExcludeInteriorInteriorIntersections =
				InitializeSetting<bool>(
					ReflectionUtils.GetProperty(
						() => LocalOptions.ExcludeInteriorInteriorIntersections),
					false);
		}

		public override void RevertToDefaults()
		{
			CentralizableTargetFeatureSelection.RevertToDefault();

			CentralizableRespectMinimumSegmentLength.RevertToDefault();
			CentralizableMinimumSegmentLength.RevertToDefault();

			CentralizableSnapToTargetVertices.RevertToDefault();
			CentralizableSnapTolerance.RevertToDefault();

			CentralizableUseSourceZs.RevertToDefault();
			CentralizableExcludeInteriorInteriorIntersections.RevertToDefault();
		}

		public override bool HasLocalOverrides(NotificationCollection notifications)
		{
			var result = false;

			if (HasLocalOverride(CentralizableSnapToTargetVertices, "Snap to target vertices",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableSnapTolerance, "Snap tolerance", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableRespectMinimumSegmentLength,
			                     "Use minimum segment length", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableMinimumSegmentLength, "Minimum segment length",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableTargetFeatureSelection,
			                     "Target feature selection", notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableUseSourceZs, "Use source feature's Z",
			                     notifications))
			{
				result = true;
			}

			if (HasLocalOverride(CentralizableExcludeInteriorInteriorIntersections,
			                     "Only chop at end points intersecting selected line's interior",
			                     notifications))
			{
				result = true;
			}

			return result;
		}

		[CanBeNull]
		public override string GetLocalOverridesMessage()
		{
			const string optionsName = "Chopper Tool Options";

			return GetLocalOverridesMessage(optionsName);
		}
	}
}
