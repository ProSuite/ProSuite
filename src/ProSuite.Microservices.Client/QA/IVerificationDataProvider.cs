using System.Collections.Generic;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.QA
{
	public interface IVerificationDataProvider
	{
		IEnumerable<GdbData> GetData(DataRequest dataRequest);

		SchemaMsg GetGdbSchema(SchemaRequest schemaRequest);
	}
}
