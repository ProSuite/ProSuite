using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class RadioButtonGroup<ENUM>
	{
		private readonly List<RadioElement<ENUM>> _list = new List<RadioElement<ENUM>>();
		private bool _latched;

		public void Add([NotNull] RadioElement<ENUM> button)
		{
			Assert.ArgumentNotNull(button, nameof(button));

			_list.Add(button);
		}

		public void ApplyChanges(object target)
		{
			if (_latched)
			{
				return;
			}

			// TODO use Latch class
			_latched = true;

			try
			{
				foreach (RadioElement<ENUM> element in _list)
				{
					element.ApplyChanges();
				}
			}
			finally
			{
				_latched = false;
			}
		}

		public void LatchPeers([NotNull] RadioElement<ENUM> element)
		{
			foreach (RadioElement<ENUM> radioElement in _list)
			{
				if (! ReferenceEquals(radioElement, element))
				{
					radioElement.Latched = true;
				}
			}
		}

		public void UnLatchPeers([NotNull] RadioElement<ENUM> element)
		{
			foreach (RadioElement<ENUM> radioElement in _list)
			{
				if (! ReferenceEquals(radioElement, element))
				{
					radioElement.Latched = false;
				}
			}
		}
	}
}
