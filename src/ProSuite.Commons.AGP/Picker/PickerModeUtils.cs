namespace ProSuite.Commons.AGP.Picker;

/// <summary>
/// Encapsulates the pure decision logic for the picker. This class deliberately depends on nothing
/// from ArcGIS.Desktop (only primitives and the <see cref="PickerMode"/> enum), so that it can be
/// unit tested.
/// </summary>
public static class PickerModeUtils
{
	/// <summary>
	/// Determines the <see cref="PickerMode"/> from the (already gathered) inputs.
	/// The empty-candidate case (<see cref="PickerMode.None"/>) is a collection-level
	/// concern and is handled by the caller; this method decides between
	/// <see cref="PickerMode.PickBest"/>, <see cref="PickerMode.ShowPicker"/> and
	/// <see cref="PickerMode.PickAll"/>.
	/// </summary>
	/// <param name="controlPressed">Whether a CTRL key is pressed.</param>
	/// <param name="altPressed">Whether an ALT key is pressed.</param>
	/// <param name="isPointClick">Whether the sketch is a single click (as opposed to an area select).</param>
	/// <param name="noMultiselection">Whether the tool forbids selecting more than one feature.</param>
	/// <param name="candidateCount">Total number of candidate features across all selection sets.</param>
	/// <param name="lowestDimensionCount">
	/// Number of candidate features sharing the lowest geometry dimension.
	/// </param>
	public static PickerMode DeterminePickerMode(bool controlPressed,
	                                             bool altPressed,
	                                             bool isPointClick,
	                                             bool noMultiselection,
	                                             int candidateCount,
	                                             int lowestDimensionCount)
	{
		if (controlPressed)
		{
			// always show picker if CTRL pressed
			return PickerMode.ShowPicker;
		}

		if (noMultiselection && candidateCount > 1)
		{
			if (! isPointClick)
			{
				return PickerMode.ShowPicker; // area selection: show picker
			}

			if (lowestDimensionCount > 1)
			{
				return PickerMode.ShowPicker;
			}

			return PickerMode.PickBest;
		}

		if (altPressed || ! isPointClick)
		{
			return PickerMode.PickAll;
		}

		// Multiselection allowed, point click, no modifier: still show the picker
		// if several candidates share the lowest geometry dimension.
		if (lowestDimensionCount > 1)
		{
			return PickerMode.ShowPicker;
		}

		return PickerMode.PickBest;
	}
}
