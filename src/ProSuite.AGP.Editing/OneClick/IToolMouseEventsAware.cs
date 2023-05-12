using System.Windows.Input;

namespace ProSuite.AGP.Editing.OneClick;

public interface IToolMouseEventsAware
{
	event MouseButtonEventHandler MouseDown;
}
