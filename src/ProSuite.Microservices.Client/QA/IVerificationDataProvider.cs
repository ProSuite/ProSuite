using System.Collections.Generic;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	public interface IVerificationDataProvider
	{
		IEnumerable<GdbData> GetData(DataRequest dataRequest);

		SchemaMsg GetGdbSchema(SchemaRequest schemaRequest);
	}
}
