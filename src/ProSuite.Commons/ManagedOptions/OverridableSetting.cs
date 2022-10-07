using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.Commons.ManagedOptions
{
	/// <summary>
	/// Allows maintaining a value type setting both centrally and locally. If the setting 
	/// can be overwritten by the user the override is stored locally.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class OverridableSetting<T> where T : struct
	{
		// Needed for de-serialization
		public OverridableSetting() { }

		public OverridableSetting(T? value, bool canOverride)
		{
			Value = value;
			Override = canOverride;
		}

		public T? Value { get; set; }

		// TODO: Consider renaming to 'UseOverride' which would for the local settings mean 'IsOverridden' 
		//		 and for the central settings 'CanOverride' -> but both things must be accessible ->
		//		 consider composite that references both the local and the central setting and has both CanOverride and HasOverride
		//		 and internally uses a property 'Override' to allow for the same XML schema?
		// TODO: Reconsider / specifically handle the following use case:
		//	1. User overrides central setting A with B
		//	2. Central settings are changed from A to B
		//	3. User reads central settings: 
		//		- Should the local settings be stored without override (if the form is closed with ok?) -> override is deleted by setting local property to null
		//		  if central settings change back to A, the user gets A next time he reads the settings
		//		- Or should the local override remain? But probably when the local override was defined in the same session (same form?)
		//		  the central setting should be used again if changing the value back to the original value!
		//  This situation would probably need some kind of user notification in both possible implementations

		/// <summary>
		/// If used as central setting: Whether the value can be overridden or not
		/// If used as local setting: whether the local value is overriding the central value
		/// </summary>
		public bool Override { get; set; }

		public bool HasValue
		{
			get { return Value != null; }
		}

		public T NonNullValue
		{
			get
			{
				Assert.False(Value == null, "Value is null");
				return (T) Value;
			}
		}

		public OverridableSetting<T> Clone()
		{
			return new OverridableSetting<T>(Value, Override);
		}
	}
}
