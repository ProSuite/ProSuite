namespace ProSuite.AGP.Editing.DestroyAndRebuild;

/// <summary>
/// Defines how the ad-hoc linear network is derived when no linear network is configured in
/// the data dictionary. Used by the Destroy and Rebuild tool's
/// <see cref="DestroyAndRebuildToolOptions.MoveOpenJawEndJunction"/> option.
/// </summary>
public enum DestroyAndRebuildLinearNetworkDefinition
{
	/// <summary>
	/// The line and point feature classes that currently have a selection form the network.
	/// </summary>
	SelectionFeatureClasses,

	/// <summary>
	/// The line and point feature layers that participate in the selected (map or GDB)
	/// topology form the network.
	/// </summary>
	Topology,

	/// <summary>
	/// All visible point and line features form the network.
	/// </summary>
	AllVisible
}
