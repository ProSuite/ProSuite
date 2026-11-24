using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using NSubstitute;
using NUnit.Framework;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.Test
{
	[TestFixture]
	public class BackgroundVerificationRunTest
	{
		[Test]
		public async Task FailedDataRequestSendsClientExceptionDetailsToServer()
		{
			// Issue C of the QA server issue list: the server only received
			// "Specified cast is not valid." without the type, the stack trace or the table.
			var dataProvider = new FailingDataProvider();

			var writtenMessages = new List<DataVerificationRequest>();
			var stream = Substitute.For<IClientStreamWriter<DataVerificationRequest>>();
			stream.WriteAsync(Arg.Do<DataVerificationRequest>(m => writtenMessages.Add(m)))
			      .Returns(Task.CompletedTask);

			var serverRequest = new DataVerificationResponse
			                    {
				                    DataRequest = new DataRequest
				                                  {
					                                  ClassDef = new ClassDef
					                                             {
						                                             ClassHandle = 417,
						                                             WorkspaceHandle = 2000030
					                                             },
					                                  WhereClause = "FEHLERART = 300",
					                                  SubFields = "*"
				                                  }
			                    };

			bool success = await BackgroundVerificationRun.SatisfyDataRequestAsync(
				               serverRequest, dataProvider, stream);

			Assert.False(success);
			Assert.AreEqual(1, writtenMessages.Count);

			DataVerificationRequest errorResponse = writtenMessages[0];

			Assert.Null(errorResponse.Data);

			string errorMessage = errorResponse.ErrorMessage;
			Console.WriteLine(errorMessage);

			// The original message stays first, so existing server-side handling still works:
			StringAssert.StartsWith("Specified cast is not valid.", errorMessage);

			// The exception type and the client stack trace reach the server log:
			StringAssert.Contains(typeof(InvalidCastException).FullName, errorMessage);
			StringAssert.Contains(nameof(FailingDataProvider.ConvertFieldValue), errorMessage);
		}

		private class FailingDataProvider : IVerificationDataProvider
		{
			public IEnumerable<GdbData> GetData(DataRequest dataRequest)
			{
				ConvertFieldValue(300L);

				yield break;
			}

			public SchemaMsg GetGdbSchema(SchemaRequest schemaRequest)
			{
				throw new NotImplementedException();
			}

			public static int ConvertFieldValue(object value)
			{
				// On .NET Framework (ArcMap), unboxing a long as int throws an exception with
				// this message. On .NET the message names the types, so it is thrown explicitly.
				throw new InvalidCastException("Specified cast is not valid.");
			}
		}
	}
}
