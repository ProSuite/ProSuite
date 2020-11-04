using System;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	///   Provides sorting by string value for Guids. This allows for compatibility between
	///   File-Geodatabase and DBMS-based Geodatabases.
	/// </summary>
	public class GuidFieldSortCallback : ITableSortCallBack
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Implementation of ITableSortCallBack

		public int Compare(object value1, object value2, int fieldIndex, int fieldSortIndex)
		{
			// TODO: sometimes the OID gets provided (with fieldSortIndex == 1) -> we never asked for it!
			//		 test whether we can safely ignore it by if (fieldSortIndex > 0) return 0;
			try
			{
				if (fieldSortIndex > 0)
				{
					_msg.VerboseDebugFormat(
						"Assuming equal IDs: value1 {0}, value2 {1}, fieldIndex {2}, fieldSortIndex {3}",
						value1, value2, fieldIndex, fieldSortIndex);

					return 0;
				}

				if (Convert.IsDBNull(value1))
				{
					_msg.WarnFormat("Sort field (fieldindex {0}) contains NULL (value1).",
					                fieldIndex);

					return Convert.IsDBNull(value2) ? 0 : -1;
				}

				if (Convert.IsDBNull(value2))
				{
					_msg.WarnFormat("Sort field (fieldindex {0}) contains NULL (value2).",
					                fieldIndex);

					return Convert.IsDBNull(value1) ? 0 : 1;
				}

				// NOTE: To avoid exception with explicit cast 
				// TODO: Check performance
				string value1String = Convert.ToString(value1);
				string value2String = Convert.ToString(value2);

				return string.Compare(value1String, value2String,
				                      StringComparison.InvariantCultureIgnoreCase);
			}
			catch (Exception e)
			{
				_msg.Debug(
					string.Format("Error comparing object values {0} and {1}", value1, value2), e);
				throw;
			}
		}

		#endregion
	}
}
