namespace ProSuite.Commons.Keyboard
{
	/// <summary>
	/// Methods to convert between string and numeric representation of 
	/// keyboard keys. Also deals with non-printable and special keys 
	/// (e.g. "F1", "NumPad7" etc.)
	/// </summary>
	public interface IKeyTranslator
	{
		int GetKey(string keyString);

		string GetKeyString(int key);
	}
}