using System;
using System.Runtime.Serialization;

namespace ProSuite.Commons.Exceptions
{
	public class DataAccessException : Exception
	{
		public long RowId { get; } = -1;
		public string TableName { get; }

		public DataAccessException() { }

		public DataAccessException(string message)
			: base(message) { }

		public DataAccessException(string message, Exception e)
			: base(message, e) { }

		public DataAccessException(string message, long rowId, string tableName, Exception e)
			: base(message, e)
		{
			RowId = rowId;
			TableName = tableName;
		}

		protected DataAccessException(SerializationInfo info,
		                              StreamingContext context)
			: base(info, context) { }
	}
}
