namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	public interface IValueList
	{
		/// <summary>
		/// Gets the value at the specified index
		/// </summary>
		/// <param name="index"></param>
		/// <param name="ensureRcwRefCountIncrease">In case of COM objects the RCW reference
		/// count must be increased exactly by 1 when getting the object in case it is released
		/// after use. Otherwise the RCW reference count will become incorrect.</param>
		/// <returns></returns>
		object GetValue(int index, bool ensureRcwRefCountIncrease = false);

		/// <summary>
		/// Sets the specified object at the specified index.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		void SetValue(int index, object value);

		/// <summary>
		/// Determines whether the the value at the specified index has ever been set.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		bool HasValue(int index);
	}
}
