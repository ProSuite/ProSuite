using System.Collections.Generic;

namespace ProSuite.Commons.GeoDb
{
	public interface ILasDatasetDef : IDatasetDef
	{
		/// <summary>
		/// The file path of the LAS dataset if it has been created.
		/// </summary>
		string FilePath { get; }

		/// <summary>
		/// The list of LAS/LAZ files referenced in the dataset.
		/// </summary>
		IList<string> LasFiles { get; }
	}
}
