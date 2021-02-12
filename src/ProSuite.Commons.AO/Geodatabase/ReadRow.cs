using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Delegate used for reading rows with a cursor.
	/// </summary>
	/// <typeparam name="T">The type of the output</typeparam>
	/// <param name="row">The row to read</param>
	/// <param name="result">The result of the read operation</param>
	/// <returns><c>true</c> if the read process should continue, <c>false</c> if the read
	/// process should stop after receiving the result of this invocation.</returns>
	public delegate bool ReadRow<T>(IRow row, out T result);
}
