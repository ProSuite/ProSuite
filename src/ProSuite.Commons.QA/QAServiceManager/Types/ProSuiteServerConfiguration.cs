
namespace ProSuite.Commons.QA.ServiceManager.Types
{

    public class ProSuiteQAServerConfiguration
    {
        public ProSuiteQAServiceType ServiceType { get; set; }

        public ProSuiteQASpecificationProviderType SpecificationsProviderType { get; set; }

        public string ServiceName { get; set; }

        public string ServiceConnection { get; set; }

    }
}
