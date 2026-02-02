namespace ProSuite.Commons.GeoDb
{
	public interface ILasDatasetDef : IDatasetDef
	{
		IDataStore FileStore { get; }

		/// <summary>
		/// The file path of the LAS dataset if it has been created.
		/// </summary>
		string LasDatasetPath { get; }

		/// <summary>
		/// The directory where the LAS files are stored.
		/// </summary>
		string LasFileDir { get; }

		/// <summary>
		/// Whether the Dataset is editable.
		/// </summary>
		bool IsEditable { get; }

		string WorkingGdbPath { get; }

		string MassPointFeatureClassName { get; }
	}
}
