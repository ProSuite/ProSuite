namespace ProSuite.Commons.AO.Test
{
	public static class TestCategory
	{
		public const string Sde = "Sde";
		public const string Fast = "Fast";
		public const string Slow = "Slow";
		public const string x86 = "x86";
		public const string Repro = "Repro";

		/// <summary>
		/// Tests that require internet access (e.g. to a public ArcGIS REST service).
		/// These are excluded from offline/CI runs.
		/// </summary>
		public const string Online = "Online";
	}
}
