namespace ProSuite.Commons.AGP.Picker;

public enum PickerPositionPreference
{
	/// <summary>
	/// The current location of the mouse pointer with special positioning in case the window
	/// would exceed the bounds of the active map view.
	/// </summary>
	MouseLocationMapOptimized,

	/// <summary>
	/// The current location of the mouse pointer with special positioning in case the window
	/// would exceed the bounds of the main application window. This is not optimal for floating
	/// map panes.
	/// </summary>
	MouseLocationMainWindowOptimized,

	/// <summary>
	/// The location of the mouse pointer as specified by the caller.
	/// </summary>
	MouseLocation
}
