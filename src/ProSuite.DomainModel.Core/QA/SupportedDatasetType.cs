namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// The kinds of dataset that quality verification can build a model from, as <em>declared</em>
	/// by whoever wrote the specification down rather than as discovered by opening the workspace.
	/// A closed set on purpose: it is the vocabulary of the <c>datasetType</c> attribute in a
	/// condition list, so a value outside it is a configuration error rather than something to
	/// interpret.
	/// <para>
	/// Deliberately not <see cref="Commons.GeoDb.DatasetType"/>, whose contract is that its values
	/// are the Esri dataset type ids. The catalog kinds here have no id of their own: a raster
	/// catalog and a point cloud catalog are both plain polygon feature classes in the
	/// geodatabase, and only the file each row points to says which is which. Every member has a
	/// counterpart there, so the mapping can be added when something first needs it. Geometric
	/// networks are absent because <see cref="Commons.GeoDb.DatasetType"/> cannot express them
	/// either; add them there first if they are ever needed here.
	/// </para>
	/// <para>
	/// Not to be confused with <see cref="TestParameterType"/>, which says what a test parameter
	/// will <em>accept</em>. This says what a dataset <em>is</em>.
	/// </para>
	/// <para>
	/// PLANNED USE - only partly realised today. Right now this is written to a standalone
	/// condition list for the file catalog kinds alone (<see cref="RasterCatalog"/> and, once
	/// implemented, <see cref="PointCloudCatalog"/>), because a harvested model cannot tell them
	/// from any other polygon feature class, and because a document carrying the attribute is
	/// rejected by builds whose compiled-in schema predates it. Once a compatible version is
	/// broadly deployed - expect to revisit around 2027 - the intent is to write it for
	/// <em>every</em> dataset parameter value. That unlocks the real prize on the reading side:
	/// <c>VerifiedModelFactory.CreateNeededModel</c> currently probes each referenced name against
	/// every dataset type with <c>IWorkspace2.NameExists</c>, six COM round-trips per dataset, and
	/// only because it has to guess. With the type declared it can construct the
	/// <c>IDatasetName</c> directly and skip the probing altogether, which is what makes opening
	/// just the needed datasets cheaper than enumerating the whole geodatabase - the enumeration
	/// that <c>CreateFullModel</c> does today.
	/// </para>
	/// </summary>
	public enum SupportedDatasetType
	{
		/// <summary>
		/// Not declared. The reader falls back to whatever it did before the type was carried,
		/// which is what every document written so far relies on.
		/// </summary>
		Null = 0,

		Table,
		FeatureClass,
		Topology,
		Terrain,
		RasterDataset,
		RasterMosaic,

		/// <summary>
		/// A polygon feature class whose rows reference raster files, one per tile.
		/// </summary>
		RasterCatalog,

		/// <summary>
		/// A polygon feature class whose rows reference LAS point cloud files, one per tile.
		/// </summary>
		PointCloudCatalog
	}
}
