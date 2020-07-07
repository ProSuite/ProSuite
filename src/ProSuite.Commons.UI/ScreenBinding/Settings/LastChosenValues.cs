using System.Collections.Generic;
using ProSuite.Commons.UI.ScreenBinding.Drivers;

namespace ProSuite.Commons.UI.ScreenBinding.Settings
{
	public static class LastChosenValues
	{
		private static readonly Dictionary<string, object> _values =
			new Dictionary<string, object>();

		public static void Clear()
		{
			_values.Clear();
		}

		public static void Store(IControlDriver driver, object lastValue)
		{
			string key = driver.GetKey();
			if (_values.ContainsKey(key))
			{
				_values[key] = lastValue;
			}
			else
			{
				_values.Add(key, lastValue);
			}
		}

		public static void Store(object control, object lastValue)
		{
			Store(ControlDriverFactory.GetDriver(control), lastValue);
		}

		public static object Retrieve(IControlDriver driver)
		{
			string key = driver.GetKey();

			if (_values.ContainsKey(key))
			{
				return _values[key];
			}

			return null;
		}

		public static object Retrieve(object control)
		{
			return Retrieve(ControlDriverFactory.GetDriver(control));
		}
	}
}
