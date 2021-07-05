using System;
using System.Runtime.Serialization;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class RelationshipClassAccessException : ModelElementAccessException
	{
		public RelationshipClassAccessException(string relationshipClassName)
			: base(GetMessage(relationshipClassName)) { }

		public RelationshipClassAccessException(string relationshipClassName, Exception e)
			: base(GetMessage(relationshipClassName), e) { }

		protected RelationshipClassAccessException(SerializationInfo info,
		                                           StreamingContext context)
			: base(info, context) { }

		private static string GetMessage(string datasetName)
		{
			return string.Format("Error accessing relationship class {0}", datasetName);
		}
	}
}
