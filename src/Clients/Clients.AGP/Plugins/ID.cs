namespace ProSuite.Solution.Plugins
{
	public static class ID
	{
		/// <summary>
		/// ID of the WorkList Plugin Datasource (must correspond with Config.xml) 
		/// </summary>
		public static readonly string WorkListPluginDatasource = "ProSuite_WorkListDatasource";

		// Constants will be embedded into the referencing assembly.
		// If we change them here, we must rebuild dependent assemblies.
		// Using static readonly cannot be embedded, thus no need
		// to rebuild dependent assemblies when values change here.
	}
}
