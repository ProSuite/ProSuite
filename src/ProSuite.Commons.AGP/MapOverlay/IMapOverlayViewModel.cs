using System.Windows.Input;

namespace ProSuite.Commons.AGP.MapOverlay;

public interface IMapOverlayViewModel
{
	ICommand EscapeCommand { get; }
	ICommand OkCommand { get; }
	string StatusMessage { get; set; }
	bool ShowStatusMessage { get; set; }
}
