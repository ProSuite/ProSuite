using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.Geodatabase;

namespace ProSuite.Microservices.Server.AO.QA
{
	/// <summary>
	/// Turns the data requests of the verification stream into the data access functions of
	/// <see cref="RemoteDataset"/>.
	/// </summary>
	public static class VerificationDataAccess
	{
		[CanBeNull]
		public static Func<DataRequest, GdbData> GetData(
			[CanBeNull] Func<DataVerificationResponse, DataVerificationRequest> requestDataFunc)
		{
			if (requestDataFunc == null)
			{
				return null;
			}

			return dataRequest => GetData(
				       requestDataFunc(new DataVerificationResponse
				                       {
					                       DataRequest = dataRequest
				                       }));
		}

		[CanBeNull]
		public static Func<GdbData> ReadNextBatch(
			[CanBeNull] Func<DataVerificationRequest> readNextBatchFunc)
		{
			if (readNextBatchFunc == null)
			{
				return null;
			}

			return () => GetData(readNextBatchFunc());
		}

		[CanBeNull]
		private static GdbData GetData([CanBeNull] DataVerificationRequest clientAnswer)
		{
			if (clientAnswer == null)
			{
				return null;
			}

			if (clientAnswer.Data == null)
			{
				throw new DataAccessException(clientAnswer.ErrorMessage);
			}

			return clientAnswer.Data;
		}
	}
}
