using System;
using System.Runtime.Serialization;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class DatasetAccessException : ModelElementAccessException
	{
		public DatasetAccessException(string datasetName) : base(GetMessage(datasetName)) { }

		public DatasetAccessException(string datasetName, Exception e)
			: base(GetMessage(datasetName), e) { }

		protected DatasetAccessException(SerializationInfo info, StreamingContext context)
			: base(info, context) { }

		private static string GetMessage(string datasetName)
		{
			return string.Format("Error accessing dataset {0}", datasetName);
		}
	}
}
