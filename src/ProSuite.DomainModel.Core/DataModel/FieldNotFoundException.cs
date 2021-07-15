using System;
using System.Runtime.Serialization;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class FieldNotFoundException : ModelElementAccessException
	{
		public FieldNotFoundException(string fieldName, string datasetName)
			: base(GetMessage(fieldName, datasetName)) { }

		public FieldNotFoundException(string fieldName, string datasetName, Exception e)
			: base(GetMessage(fieldName, datasetName), e) { }

		protected FieldNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context) { }

		private static string GetMessage(string fieldName, string datasetName)
		{
			return string.Format("Field {0} not found in dataset {1}",
			                     fieldName, datasetName);
		}
	}
}
