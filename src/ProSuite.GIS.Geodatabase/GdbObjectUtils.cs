using System;
using System.Globalization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase
{
	public static class GdbObjectUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Determines whether a field value in a row is Null.
		/// </summary>
		/// <param name="row">The row to test the value for</param>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <returns>
		/// 	<c>true</c> if the value is Null; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsNullOrEmpty([NotNull] IRow row, int fieldIndex)
		{
			object value = row.get_Value(fieldIndex);

			return IsNullOrEmpty(value);
		}

		/// <summary>
		/// Determines whether a field value in a row is Null.
		/// </summary>
		/// <param name="fieldValue">The field value.</param>
		/// <returns>
		/// 	<c>true</c> if the value is Null; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsNullOrEmpty([CanBeNull] object fieldValue)
		{
			if (fieldValue == null)
			{
				return true;
			}

			if (fieldValue is DBNull)
			{
				return true;
			}

			return string.IsNullOrEmpty(fieldValue.ToString());
		}

		/// <summary>
		/// Gets the display value for field value of an object
		/// </summary>
		/// <param name="obj">The object</param>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <returns></returns>
		[CanBeNull]
		public static object GetDisplayValue([NotNull] IObject obj, int fieldIndex)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			object value = obj.get_Value(fieldIndex);

			if (value == null || value is DBNull)
			{
				return null;
			}

			var subtypes = obj.Class as ISubtypes;
			object subtypeValue = null;
			if (subtypes != null && subtypes.HasSubtype)
			{
				subtypeValue = obj.get_Value(subtypes.SubtypeFieldIndex);
			}

			int? subtypeCode = GetNullableSubtypeCode(subtypeValue);

			return DatasetUtils.GetDisplayValue(obj.Class, fieldIndex, value,
			                                    subtypeCode);
		}

		private static int? GetNullableSubtypeCode([CanBeNull] object subtypeFieldValue)
		{
			if (subtypeFieldValue == null || subtypeFieldValue is DBNull)
			{
				return null;
			}

			return subtypeFieldValue as int? ?? Convert.ToInt32(subtypeFieldValue);
		}

		[CanBeNull]
		public static T? ConvertRowValue<T>([NotNull] IRow row, int fieldIndex)
			where T : struct
		{
			Assert.ArgumentNotNull(row, nameof(row));

			object value = row.get_Value(fieldIndex);

			if (value == null || value == DBNull.Value)
			{
				_msg.VerboseDebug(
					() => $"ConvertRowValue: Field value at <index> {fieldIndex} of row is null.");

				return null;
			}

			try
			{
				//// work-around for Change-Type not supporting nullable types in .NET 2.0
				//// http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=94624
				//NullableConverter nullableConverter = new NullableConverter(typeof(T));
				//Type conversionType = nullableConverter.UnderlyingType;

				return (T?) Convert.ChangeType(value, typeof(T));
			}
			catch (Exception ex)
			{
				int? rowOid = GetObjectId(row);

				_msg.ErrorFormat(
					"ConvertRowValue: Error converting value {0} of type {1} into type {2} for row <oid> {3} at field index {4} in {5}: {6}",
					value, value.GetType(), typeof(T), fieldIndex, rowOid,
					((IDataset) row.Table).Name, ex.Message);

				throw;
			}
		}

		public static int? GetObjectId([NotNull] IRow row)
		{
			return GetObjectId((IObject) row);
		}

		public static int? GetObjectId([NotNull] IObject obj)
		{
			return obj.HasOID
				       ? (int?) obj.OID
				       : null;
		}

		/// <summary>
		/// Returns a string representation of the <see cref="IObject"/>.
		/// </summary>
		/// <param name="obj">The object to get the string representation for.</param>
		/// <returns></returns>
		[NotNull]
		public static string ToString([NotNull] IObject obj)
		{
			var oid = @"[n/a]";
			if (obj.HasOID)
			{
				oid = obj.OID.ToString(CultureInfo.InvariantCulture);
			}

			string className;
			try
			{
				className = DatasetUtils.GetName(obj.Class);
			}
			catch (Exception)
			{
				className = "[error getting class name]";
			}

			return string.Format("oid={0} class={1}", oid, className);
		}

		/// <summary>
		/// Returns a string representation of the <see cref="IObject"/>.
		/// </summary>
		/// <param name="row">The object to get the string representation for.</param>
		/// <returns></returns>
		[NotNull]
		public static string ToString([NotNull] IRow row)
		{
			string oid;
			try
			{
				oid = row.HasOID
					      ? row.OID.ToString(CultureInfo.InvariantCulture)
					      : @"[n/a]";
			}
			catch (Exception e)
			{
				oid = string.Format("[error getting OID: {0}]", e.Message);
			}

			string tableName;
			try
			{
				tableName = DatasetUtils.GetName(row.Table);
			}
			catch (Exception e)
			{
				tableName = string.Format("[error getting table name: {0}]", e.Message);
			}

			return string.Format("oid={0} table={1}", oid, tableName);
		}
	}
}
