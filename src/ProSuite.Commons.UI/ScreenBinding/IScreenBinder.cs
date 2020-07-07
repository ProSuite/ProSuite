using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding
{
	public interface IScreenBinder : IEnumerable<IScreenElement>, IScreenDriver
	{
		bool IsLatched { get; }

		void AddElement([NotNull] IScreenElement element);

		void SetDefaultValues();

		void UpdateScreen();

		void Validate([NotNull] IBoundScreenElement element);

		void ShowErrorMessages([NotNull] IBoundScreenElement element,
		                       params string[] messages);

		void BindToModel([NotNull] object target);

		bool ApplyChangesToModel();

		void ResetToOriginalValues();

		void MessageElements([NotNull] Action<IScreenElement> action);

		void InsideLatch([NotNull] Action action);

		Action OnChange { get; set; }

		void MakeReadOnly(bool readOnly);

		bool IsDirty();
	}
}
