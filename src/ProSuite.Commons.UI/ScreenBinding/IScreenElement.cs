using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Drivers;
using ProSuite.Commons.UI.ScreenBinding.ScreenStates;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public interface IScreenElement
	{
		ActivationMode ActivationMode { get; set; }

		IControlDriver Label { get; set; }

		IControlDriver PostLabel { get; set; }

		string Alias { get; set; }

		string LabelText { get; }

		object Control { get; }

		void EnableControl([NotNull] IScreenState state);

		bool Matches(string labelText);

		void Focus();

		void CopyFrom([NotNull] IScreenDriver driver);

		void Hide();

		void Show();

		void Highlight(Color color);

		void RemoveHighlight();

		void UpdateDisplayState(object target);

		void BindVisibilityTo([NotNull] IPropertyAccessor accessor);

		void BindEnabledTo([NotNull] IPropertyAccessor accessor);

		void Enable();

		void Disable();

		string ToolTipText { get; set; }
	}
}
