namespace ProSuite.Microservices.Client
{
	public interface IClientChannelConfig
	{
		/// <summary>
		/// The host name. In case localhost is used, the application can start a new (local) server process.
		/// </summary>
		string HostName { get; set; }

		/// <summary>
		/// The port. In case the host is localhost and the specified port is below 0 the next free
		/// ephemeral port will be used to communicate with a newly started server process.
		/// In case the host is localhost and a positive port number is specified, an already
		/// running process using the same port will be
		/// - used directly (no new process is started) if it is healthy
		/// - killed and restarted, in case it is not healthy.
		/// If no current process is using the specified port, a new server process is started.
		/// </summary>
		int Port { get; set; }

		/// <summary>
		/// Whether the channel uses transport layer security (SSL/TLS). If true, the trusted root
		/// certificates from the user's certificate store are used for the channel credentials.
		/// </summary>
		bool UseTls { get; set; }

		/// <summary>
		/// Optionally, use 'mutual TLS' by providing the client certificate (including an exportable
		/// private key). Specify the certificate subject or thumbprint of the certificate in the
		/// current user's certificate store.
		/// </summary>
		string ClientCertificate { get; set; }
	}
}
