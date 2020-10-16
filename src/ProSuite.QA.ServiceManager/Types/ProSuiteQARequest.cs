namespace ProSuite.QA.ServiceManager.Types
{
    public enum ProSuiteQADataFormat
    {
        ArcGIS,
        Json,
        gRPC
    }

    public enum ProSuiteQAServiceType
    {
        GPLocal,
        GPService,
        gRPC,
        Mock
    }

    public enum ProSuiteQASpecificationProviderType
    {
        Xml,
        Ddx,
        Mock
    }

    public enum ProSuiteQAError
    {
        None,
        ServiceUnavailable,
        ServiceFailed,
        Canceled
    }

    public class ProSuiteQARequest
    {
        public ProSuiteQARequest(ProSuiteQAServiceType type)
        {
            this.ServiceType = type;
        }

        public ProSuiteQAServiceType ServiceType { get; }

        public ProSuiteQADataFormat RequestDataFormat { get; }

        public object RequestData { get; set; }
    }

    public class ProSuiteQAResponse
    {
        public ProSuiteQAError Error { get; set; }

        public string ErrorMessage { get; set; }

        public ProSuiteQADataFormat ResponseDataFormat { get; }

//        public ProSuiteQADataType ResponseDataType { get; }

        public object ResponseData { get; set; }
    }
}
