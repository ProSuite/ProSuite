using System;
using System.Text.RegularExpressions;

namespace ProSuite.Processing.Domain
{
	[Flags]
	public enum ProcessSelectionType
	{
		SelectedFeatures = 1,
		VisibleExtent = 2,
		AllFeatures = 3,

		WithinPerimeter = 0x100,

		SelectedFeaturesWithinPerimeter = SelectedFeatures + WithinPerimeter,
		VisibleExtentWithinPerimeter = VisibleExtent + WithinPerimeter,
		AllFeaturesWithinPerimeter = AllFeatures + WithinPerimeter
	}

	public static class ProcessSelectionTypeExtensions
	{
		private const ProcessSelectionType BaseModeMask = (ProcessSelectionType) 0xFF;

		/// <summary>
		/// Returns true iff the given <paramref name="selectionType"/> is
		/// "selected features", ignoring any additional flags.
		/// </summary>
		public static bool IsSelectedFeatures(this ProcessSelectionType selectionType)
		{
			return (selectionType & BaseModeMask) == ProcessSelectionType.SelectedFeatures;
		}

		public static bool IsVisibleExtent(this ProcessSelectionType selectionType)
		{
			return (selectionType & BaseModeMask) == ProcessSelectionType.VisibleExtent;
		}

		public static bool IsAllFeatures(this ProcessSelectionType selectionType)
		{
			return (selectionType & BaseModeMask) == ProcessSelectionType.AllFeatures;
		}

		public static bool IsWithinEditPerimeter(this ProcessSelectionType selectionType)
		{
			return (selectionType & ProcessSelectionType.WithinPerimeter) ==
			       ProcessSelectionType.WithinPerimeter;
		}

		public static ProcessSelectionType WithinEditPerimeter(
			this ProcessSelectionType selectionType)
		{
			return selectionType | ProcessSelectionType.WithinPerimeter;
		}

		/// <summary>
		/// Return selection type "Visible Features" with the same extra
		/// flags as the given <paramref name="selectionType"/>
		/// </summary>
		public static ProcessSelectionType ToVisibleExtent(this ProcessSelectionType selectionType)
		{
			return ProcessSelectionType.VisibleExtent | (selectionType & ~BaseModeMask);
		}

		/// <summary>
		/// Return true iff the given <paramref name="selectionType"/>
		/// is valid. Use this to check if a selection type is a valid
		/// combination of a base type (selected/visible/all features)
		/// and some extra flags (presently only WithinPerimeter).
		/// </summary>
		public static bool IsValid(this ProcessSelectionType selectionType)
		{
			switch (selectionType)
			{
				case ProcessSelectionType.SelectedFeatures:
				case ProcessSelectionType.SelectedFeaturesWithinPerimeter:
				case ProcessSelectionType.VisibleExtent:
				case ProcessSelectionType.VisibleExtentWithinPerimeter:
				case ProcessSelectionType.AllFeatures:
				case ProcessSelectionType.AllFeaturesWithinPerimeter:
					return true;

				default:
					return false;
			}
		}

		/// <summary>
		/// Produces a "nice" string for display purposes.
		/// </summary>
		public static string ToDisplayName(this ProcessSelectionType selectionType)
		{
			var baseMode = selectionType & BaseModeMask;

			var baseName = Enum.GetName(typeof(ProcessSelectionType), baseMode) ??
			               baseMode.ToString();

			var niceName = Regex.Replace(baseName, @"([0-9\p{Ll}])(\p{Lu})", "$1 $2");

			if (IsWithinEditPerimeter(selectionType))
			{
				niceName += " within Perimeter";
			}

			return niceName;
		}
	}
}
