using System;
using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Security.Permissions;
using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.Type;
using NHibernate.UserTypes;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Persistence.Core.DataModel
{
	[Serializable]
	public abstract class AttributeRoleType : IUserType, ISerializable
	{
		[NonSerialized] private static readonly NullableType _type = NHibernateUtil.Int32;

		[NonSerialized] private static readonly SqlType[] _types = {_type.SqlType};

		#region IUserType

		/// <summary>
		/// Compare two instances of the class mapped by this type for persistent "equality"
		/// ie. equality of persistent state
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		bool IUserType.Equals(object x, object y)
		{
			if (x == y)
			{
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}

			return x.Equals(y);
		}

		public int GetHashCode(object x)
		{
			return x.GetHashCode();
		}

		/// <summary>
		/// Retrieve an instance of the mapped class from a JDBC resultset.
		/// Implementors should handle possibility of null values.
		/// </summary>
		/// <param name="rs">a IDataReader</param>
		/// <param name="names">column names</param>
		/// <param name="session"></param>
		/// <param name="owner">the containing entity</param>
		/// <returns></returns>
		/// <exception cref="T:NHibernate.HibernateException">HibernateException</exception>
		public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session,
		                          object owner)
		{
			object value = _type.NullSafeGet(rs, names, session);
			return value == null
				       ? null
				       : Resolve((int) value);
		}

		/// <summary>
		/// Write an instance of the mapped class to a prepared statement.
		/// Implementors should handle possibility of null values.
		/// A multi-column type should be written to parameters starting from index.
		/// </summary>
		/// <param name="cmd">a IDbCommand</param>
		/// <param name="value">the object to write</param>
		/// <param name="index">command parameter index</param>
		/// <param name="session"></param>
		/// <exception cref="T:NHibernate.HibernateException">HibernateException</exception>
		public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
		{
			if (value == null)
			{
				((IDbDataParameter) cmd.Parameters[index]).Value = DBNull.Value;
			}
			else
			{
				var role = (AttributeRole) value;
				_type.Set(cmd, role.Id, index, session);
			}
		}

		/// <summary>
		/// Return a deep copy of the persistent state, stopping at entities and at collections.
		/// </summary>
		/// <param name="value">generally a collection element or entity field</param>
		/// <returns>a copy</returns>
		public object DeepCopy(object value)
		{
			return value; // immutable, can return same instance
		}

		public object Replace(object original, object target, object owner)
		{
			return original;
		}

		public object Assemble(object cached, object owner)
		{
			return DeepCopy(cached);
		}

		public object Disassemble(object value)
		{
			return DeepCopy(value);
		}

		/// <summary>
		/// The SQL types for the columns mapped by this type.
		/// </summary>
		/// <value></value>
		public SqlType[] SqlTypes
		{
			get { return _types; }
		}

		/// <summary>
		/// The type returned by <c>NullSafeGet()</c>
		/// </summary>
		/// <value></value>
		public Type ReturnedType
		{
			get { return typeof(AttributeRole); }
		}

		/// <summary>
		/// Are objects of this type mutable?
		/// </summary>
		/// <value></value>
		public bool IsMutable
		{
			get { return false; }
		}

		/// <summary>
		/// Resolves the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		protected abstract AttributeRole Resolve(int value);

		#endregion

		#region ISerializable

		[SecurityPermission(SecurityAction.LinkDemand,
		                    Flags = SecurityPermissionFlag.SerializationFormatter)]
		public virtual void
			GetObjectData(SerializationInfo info, StreamingContext context) { }

		#endregion
	}
}
