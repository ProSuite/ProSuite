using System;
using System.Windows.Forms;
using ProSuite.Commons.Keyboard;

namespace ProSuite.Commons.UI.Keyboard
{
	public class KeyTranslator : IKeyTranslator
	{
		#region IKeyTranslator Members

		public int GetKey(string keyString)
		{
			return (int) Enum.Parse(typeof(Keys), keyString);
		}

		public string GetKeyString(int key)
		{
			return ((Keys) key).ToString();
		}

		#endregion
	}
}
