using System.Windows.Input;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Framework.Controls;

public interface ICommandWrapper
{
	/// <summary>
	/// The ArcGIS Pro command ID (DAML ID).
	/// </summary>
	[CanBeNull]
	string CommandID { get; }

	/// <summary>
	/// The ICommand instance of the wrapped button which is also the ArcGIS Pro
	/// command instance.
	/// </summary>
	[CanBeNull]
	ICommand Command { get; }

	/// <summary>
	/// Updates the appearance of the button based on the state of the
	/// wrapped command.
	/// </summary>
	/// <param name="force"></param>
	void UpdateAppearance(bool force = true);
}
