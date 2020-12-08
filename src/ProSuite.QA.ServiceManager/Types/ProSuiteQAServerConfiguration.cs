namespace ProSuite.QA.ServiceManager.Types
{
    public class ProSuiteQAServerConfiguration
	{
		public ProSuiteQAServerConfiguration()
		{
		}

		public ProSuiteQAServerConfiguration(ProSuiteQAServerConfiguration original)
		{
			ServiceType = original.ServiceType;
			ServiceName = original.ServiceName;
			ServiceConnection = original.ServiceConnection;
		}

		public ProSuiteQAServiceType ServiceType { get; set; }

        public string ServiceName { get; set; }

        public string ServiceConnection { get; set; }

        public int DefaultTileSize { get; set; } = 50000;

		public string DefaultOutputFolder { get; set; } = "";

		public string DefaultCompressValue { get; set; } = "false";

		public bool IsValid
		{
			get
			{
				return !string.IsNullOrEmpty(ServiceConnection);
			}
		}
    }
}
