using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public interface IBoundScreenElement : IScreenElement, IBoundPart
	{
		void RegisterChangeHandler([NotNull] Action handler);

		void RememberLastChoice();

		void RebindAllOnChange();

		void RegisterLostFocusHandler([NotNull] Action action);

		[NotNull]
		NotificationMessage[] Validate();

		//bool IsDirty();

		object GetValue();
	}
}
