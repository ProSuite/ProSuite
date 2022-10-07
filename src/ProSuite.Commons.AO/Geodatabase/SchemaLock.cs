using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// Acquire and release an exclusive schema lock
	/// </summary>
	/// <remarks>
	/// Usage pattern:
	/// <code>
	/// using (new SchemaLock(datasetOrClass))
	/// {
	///   schema changes on dataset or object class, such as
	///   adding or deleting a field, using IClassSchemaEdit, etc.
	/// }
	/// </code>
	/// </remarks>
	public class SchemaLock : IDisposable
	{
		private readonly ISchemaLock _schemaLock;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public SchemaLock([NotNull] IDataset dataset)
			: this((ISchemaLock) dataset) { }

		public SchemaLock([NotNull] IClass objectClass)
			: this((ISchemaLock) objectClass) { }

		public SchemaLock([NotNull] ISchemaLock schemaLock)
		{
			Assert.ArgumentNotNull(schemaLock, nameof(schemaLock));

			_schemaLock = schemaLock;

			string tableName, userName;
			if (HasExclusiveLock(out tableName, out userName))
			{
				_msg.DebugFormat(
					"Already exclusively locked, but trying anyway (table={0}, user={1})",
					tableName ?? "<unknown>", userName ?? "<unknown>");
			}

			// Note: experience shows that
			// (a) even if there's no exclusive lock, Lock() may fail, and
			// (b) even if there's an exclusive lock, Lock() may succeed (same thread cleverness?)

			Lock();
		}

		public void Dispose()
		{
			Unlock();
		}

		public bool HasExclusiveLock()
		{
			return HasExclusiveLock(out string _, out string _);
		}

		public bool HasExclusiveLock([CanBeNull] out string tableName,
		                             [CanBeNull] out string userName)
		{
			tableName = null;
			userName = null;
			IEnumSchemaLockInfo existingLocks;
			_schemaLock.GetCurrentSchemaLocks(out existingLocks);

			if (existingLocks == null) return false;

			existingLocks.Reset();

			ISchemaLockInfo info;
			while ((info = existingLocks.Next()) != null)
			{
				if (info.SchemaLockType == esriSchemaLock.esriExclusiveSchemaLock)
				{
					tableName = info.TableName;
					userName = info.UserName;
					return true;
				}
			}

			return false;
		}

		private void Lock()
		{
			try
			{
				_schemaLock.ChangeSchemaLock(esriSchemaLock.esriExclusiveSchemaLock);
			}
			catch (Exception inner)
			{
				throw new Exception("Cannot acquire Exclusive Schema Lock", inner);
			}
		}

		private void Unlock()
		{
			try
			{
				// Hint: cannot unlock, can only "demote" from exclusive to shared:
				_schemaLock.ChangeSchemaLock(esriSchemaLock.esriSharedSchemaLock);
			}
			catch (Exception inner)
			{
				throw new Exception("Cannot acquire Shared Schema Lock", inner);
			}
		}
	}
}
