using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.Ado
{
	public static class AdoUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull]
		[PublicAPI]
		public static IDbCommand CreateStoredProcedureCommand(
			[NotNull] IDbConnection connection,
			[NotNull] string procedureName)
		{
			IDbCommand command = connection.CreateCommand();

			command.CommandType = CommandType.StoredProcedure;
			command.CommandText = procedureName;

			return command;
		}

		[NotNull]
		[PublicAPI]
		public static IDbCommand CreateSqlTextCommand(
			[NotNull] IDbConnection connection,
			[NotNull] string sqlText)
		{
			return CreateSqlTextCommand(connection, sqlText, null);
		}

		[NotNull]
		public static IDbCommand CreateSqlTextCommand(
			[NotNull] IDbConnection connection,
			[NotNull] string sqlText, [CanBeNull] NotificationCollection notifications)
		{
			IDbCommand command = connection.CreateCommand();

			command.CommandType = CommandType.Text;
			command.CommandText = sqlText;

			//_msg.DebugFormat("Created command with following SQL Text: {0}", sqlText);
			NotificationUtils.Add(notifications, "Created command with following SQL Text: {0}",
			                      sqlText);
			return command;
		}

		[PublicAPI]
		public static void AddInputParameter([NotNull] IDbCommand command,
		                                     [NotNull] string parameterName,
		                                     string value)
		{
			AddInputParameter(command, parameterName, DbType.String, value);
		}

		[PublicAPI]
		public static void AddInputParameter([NotNull] IDbCommand command,
		                                     [NotNull] string parameterName,
		                                     int value)
		{
			AddInputParameter(command, parameterName, DbType.Int32, value);
		}

		[PublicAPI]
		public static void AddInputParameter([NotNull] IDbCommand command,
		                                     [NotNull] string parameterName, DbType dbType,
		                                     object value)
		{
			IDbDataParameter parameter = command.CreateParameter();

			parameter.ParameterName = parameterName;
			parameter.Direction = ParameterDirection.Input;
			parameter.DbType = dbType;
			parameter.Value = value ?? DBNull.Value;

			command.Parameters.Add(parameter);
		}

		[PublicAPI]
		public static void AddOutputParameter([NotNull] IDbCommand command,
		                                      [NotNull] string parameterName,
		                                      DbType dbType,
		                                      [CanBeNull] int? size = null)
		{
			IDbDataParameter parameter = command.CreateParameter();

			parameter.ParameterName = parameterName;
			parameter.Direction = ParameterDirection.Output;
			parameter.DbType = dbType;

			if (size.HasValue)
			{
				parameter.Size = size.Value;
			}

			command.Parameters.Add(parameter);
		}

		[CanBeNull]
		[PublicAPI]
		public static T GetOutputParameterObject<T>([NotNull] IDbCommand command,
		                                            [NotNull] string parameterName)
			where T : class
		{
			return (T) GetOutputParameterValue(command, parameterName);
		}

		[CanBeNull]
		[PublicAPI]
		public static object GetOutputParameterValue([NotNull] IDbCommand command,
		                                             [NotNull] string parameterName)
		{
			var param = (IDbDataParameter) command.Parameters[parameterName];
			object value = param.Value;

			return value == DBNull.Value
				       ? null
				       : value;
		}

		/// <summary>
		/// Makes sure that a given connection is open while a procedure is executed,
		/// and closes the connection again if it was closed.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="procedure">The procedure.</param>
		[PublicAPI]
		public static void WithOpenConnection([NotNull] IDbConnection connection,
		                                      [NotNull] Action procedure)
		{
			Assert.ArgumentNotNull(connection, nameof(connection));
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			bool wasClosed = connection.State == ConnectionState.Closed;

			try
			{
				if (wasClosed)
				{
					connection.Open();
				}

				procedure();
			}
			finally
			{
				if (wasClosed && connection.State != ConnectionState.Closed)
				{
					connection.Close();
				}
			}
		}

		[PublicAPI]
		public static void WithTransaction([NotNull] IDbConnection connection,
		                                   [NotNull] IDbCommand command,
		                                   [NotNull] Action procedure,
		                                   IsolationLevel isolationLevel =
			                                   IsolationLevel.Unspecified)
		{
			Assert.ArgumentNotNull(connection, nameof(connection));
			Assert.ArgumentNotNull(command, nameof(command));
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			using (IDbTransaction transaction =
				BeginTransaction(connection, isolationLevel))
			{
				command.Transaction = transaction;

				try
				{
					procedure();
				}
				catch (Exception)
				{
					try
					{
						transaction.Rollback();
					}
					catch (Exception e)
					{
						_msg.Warn(
							string.Format("Error rolling back transaction: {0}",
							              e.Message), e);
					}

					throw;
				}

				transaction.Commit();
			}
		}

		[PublicAPI]
		public static int ExecuteNonQueryInTransaction(
			[NotNull] IDbConnection connection,
			[NotNull] IDbCommand command,
			IsolationLevel isolationLevel = IsolationLevel.Unspecified)
		{
			Assert.ArgumentNotNull(connection, nameof(connection));
			Assert.ArgumentNotNull(command, nameof(command));

			return ExecuteNonQueryInTransaction(connection, new List<IDbCommand> {command},
			                                    isolationLevel);
		}

		[PublicAPI]
		public static int ExecuteNonQueryInTransaction(
			[NotNull] IDbConnection connection,
			[NotNull] IEnumerable<IDbCommand> commands,
			IsolationLevel isolationLevel = IsolationLevel.Unspecified)
		{
			Assert.ArgumentNotNull(connection, nameof(connection));
			Assert.ArgumentNotNull(commands, nameof(commands));

			int result = 0;

			using (IDbTransaction transaction =
				BeginTransaction(connection, isolationLevel))
			{
				try
				{
					foreach (IDbCommand dbCommand in commands)
					{
						dbCommand.Transaction = transaction;

						result += dbCommand.ExecuteNonQuery();
					}
				}
				catch (Exception)
				{
					try
					{
						transaction.Rollback();
					}
					catch (Exception e)
					{
						_msg.Warn(
							string.Format("Error rolling back transaction: {0}",
							              e.Message), e);
					}

					throw;
				}

				transaction.Commit();
			}

			return result;
		}

		// TODO move elsewhere to get rid of oracle driver dependency?
		[NotNull]
		[PublicAPI]
		public static IEnumerable<T> ReadFieldValues<T>(
			[NotNull] IDbConnection connection,
			[NotNull] string selectStatement,
			[NotNull] string fieldName,
			[NotNull] Func<IDataReader, int, T> readValue)
		{
			Assert.ArgumentNotNull(connection, nameof(connection));
			Assert.ArgumentCondition(connection.State == ConnectionState.Open,
			                         "The connection must be open");
			Assert.ArgumentNotNullOrEmpty(selectStatement, nameof(selectStatement));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));
			Assert.ArgumentNotNull(readValue, nameof(readValue));

			using (IDbCommand command = connection.CreateCommand())
			{
				command.CommandType = CommandType.Text;
				command.CommandText = selectStatement;

				using (IDataReader reader = command.ExecuteReader())
				{
					int fieldIndex = reader.GetOrdinal(fieldName);

					while (reader.Read())
					{
						yield return readValue(reader, fieldIndex);
					}
				}
			}
		}

		[NotNull]
		private static IDbTransaction BeginTransaction([NotNull] IDbConnection connection,
		                                               IsolationLevel isolationLevel)
		{
			Assert.ArgumentNotNull(connection, nameof(connection));

			return isolationLevel == IsolationLevel.Unspecified
				       ? connection.BeginTransaction()
				       : connection.BeginTransaction(isolationLevel);
		}
	}
}