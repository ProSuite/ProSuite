using System;
using System.Collections;
using System.Collections.Generic;
using ProSuite.Commons.UI.ScreenBinding.ScreenStates;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class NulloBinder : IScreenBinder
	{
		#region IScreenBinder Members

		public void EnableControls(IScreenState state)
		{
			// no-op;
		}

		public void AddElement(IScreenElement element)
		{
			// no-op;
		}

		public bool IsLatched
		{
			get { return false; }
			set { }
		}

		public IScreenElement FindElement(string labelOrAlias)
		{
			return null;
		}

		public IBoundScreenElement FindElementByField(string fieldName)
		{
			throw new NotImplementedException();
		}

		public void SetDefaultValues() { }

		public void UpdateScreen() { }

		public void Validate(IBoundScreenElement element) { }

		public void Focus(object control)
		{
			throw new NotImplementedException();
		}

		public void Focus(string label)
		{
			throw new NotImplementedException();
		}

		public IScreenElement FindElementForControl(object control)
		{
			throw new NotImplementedException();
		}

		public void BindToModel(object target)
		{
			throw new NotImplementedException();
		}

		public bool ApplyChangesToModel()
		{
			throw new NotImplementedException();
		}

		public void ResetToOriginalValues()
		{
			throw new NotImplementedException();
		}

		public void MessageElements(Action<IScreenElement> action) { }

		public void InsideLatch(Action action)
		{
			action();
		}

		public Action OnChange
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public void MakeReadOnly(bool readOnly)
		{
			throw new NotImplementedException();
		}

		public bool IsDirty()
		{
			return false;
		}

		public void ShowErrorMessages(IBoundScreenElement element,
		                              params string[] messages) { }

		IEnumerator<IScreenElement> IEnumerable<IScreenElement>.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		public IEnumerator GetEnumerator()
		{
			return ((IEnumerable<IScreenElement>) this).GetEnumerator();
		}

		#endregion
	}
}
