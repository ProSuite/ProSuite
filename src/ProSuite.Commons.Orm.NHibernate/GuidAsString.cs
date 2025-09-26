using System;
using System.Data.Common;
using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Orm.NHibernate
{
	/// <summary>
	/// Custom user type for NHibernate to store a <see cref="Guid"/>
	/// as a 38 character string in "uppercase braced format", same as
	/// ArcGIS in Oracle. Example: "{DC25998A-4AE8-4C3E-A6C2-B11CB9B00909}"
	/// </summary>
	[UsedImplicitly]
	public class GuidAsString : IUserType
	{
		public SqlType[] SqlTypes => new SqlType[] { SqlTypeFactory.GetString(38) };
		public Type ReturnedType => typeof(Guid);
		public bool IsMutable => false;

		public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
		{
			var value = NHibernateUtil.String.NullSafeGet(rs, names[0], session);
			if (value is null) return null;
			return new Guid((string)value);
		}

		public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
		{
			if (value is null)
			{
				NHibernateUtil.DateTime.NullSafeSet(cmd, null, index, session);
			}
			else
			{
				var guid = (Guid)value;
				var text = FormatGuid(guid);
				NHibernateUtil.String.NullSafeSet(cmd, text, index, session);
			}
		}

		public static string FormatGuid(Guid guid)
		{
			return guid.ToString("B").ToUpper();
		}

		public object DeepCopy(object value)
		{
			return value; // Guid is a value type, so just return it
		}

		public object Replace(object original, object target, object owner)
		{
			return original;
		}

		public object Assemble(object cached, object owner)
		{
			return cached;
		}

		public object Disassemble(object value)
		{
			return value;
		}

		public new bool Equals(object x, object y)
		{
			// Note: objects passed in are client types, not DB types;
			// that is, here we get System.Guid, not any kind of string:
			return x?.Equals(y) ?? y == null;
		}

		public int GetHashCode(object x)
		{
			return x?.GetHashCode() ?? 0;
		}
	}
}
