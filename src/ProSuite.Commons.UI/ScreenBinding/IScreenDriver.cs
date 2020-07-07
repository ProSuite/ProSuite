using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.ScreenStates;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public interface IScreenDriver
	{
		void EnableControls([NotNull] IScreenState state);

		[CanBeNull]
		IScreenElement FindElement(string labelOrAlias);

		[CanBeNull]
		IBoundScreenElement FindElementByField(string fieldName);

		void Focus([NotNull] object control);

		void Focus([NotNull] string label);

		[CanBeNull]
		IScreenElement FindElementForControl([NotNull] object control);
	}
}
