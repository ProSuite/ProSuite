namespace ProSuite.Microservices.Client
{
	public static class ProtobufGeoDbUtils
	{
		/// <summary>
		/// The name of the domain property of the FieldMsg that notifies the client that the
		/// respective field is the subtype field. This could be removed if the proto model is
		/// extended.
		/// </summary>
		public const string SubtypeDomainName = "__SubType__";
	}
}
