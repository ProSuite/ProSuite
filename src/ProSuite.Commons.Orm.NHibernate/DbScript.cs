using System;
using System.Data;
using System.IO;
using System.Text;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Impl;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Orm.NHibernate
{
	[UsedImplicitly]
	public class DbScript
	{
		[NotNull] private readonly ISessionFactory _sessionFactory;

		[NotNull] [UsedImplicitly] private Configuration _configuration;

		[CanBeNull] private string _script;

		private const string _noValueString = "_";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes a new instance of the <see cref="DbScript"/> class.
		/// </summary>
		/// <param name="configurationBuilder"></param>
		[CLSCompliant(false)]
		public DbScript(INhConfigurationBuilder configurationBuilder)
		{
			_configuration = configurationBuilder.GetConfiguration();
			_sessionFactory = _configuration.BuildSessionFactory();
		}

		[UsedImplicitly]
		public string Script
		{
			set { _script = value; }
		}

		/// <summary>
		/// Whether a statement that fails aborts the script. The default is to log the failure
		/// and carry on, which suits scripts whose statements are allowed to fail (dropping an
		/// object that is not there yet). A caller that cannot tell a harmless failure from a
		/// broken result - schema setup, above all - sets this instead, so that what went wrong
		/// reaches it rather than a log nobody reads.
		/// </summary>
		[UsedImplicitly]
		public bool FailOnError { get; set; }

		/// <summary>
		/// Runs the script.
		/// </summary>
		public void Run()
		{
			if (string.IsNullOrEmpty(_script) || _script.Trim() == _noValueString)
			{
				return;
			}

			using (var reader = new StringReader(_script))
			{
				var factory = _sessionFactory as SessionFactoryImpl;
				IDbConnection dbConnection =
					Assert.NotNull(factory).ConnectionProvider.GetConnection();

				using (dbConnection)
				{
					using (IDbCommand cmd = dbConnection.CreateCommand())
					{
						var statementBuilder = new StringBuilder();

						string line;
						while ((line = reader.ReadLine()) != null)
						{
							string trimmedLine = line.Trim();

							if (string.IsNullOrEmpty(trimmedLine) || IsComment(trimmedLine))
							{
								continue;
							}

							if (trimmedLine.EndsWith(";"))
							{
								statementBuilder.AppendLine(trimmedLine.TrimEnd(';'));

								Execute(statementBuilder.ToString().Trim(), cmd, FailOnError);

								statementBuilder = new StringBuilder();
							}
							else
							{
								statementBuilder.AppendLine(trimmedLine);
							}
						}
					}
				}
			}
		}

		private static void Execute([NotNull] string statement, [NotNull] IDbCommand cmd,
		                            bool failOnError)
		{
			Assert.ArgumentNotNullOrEmpty(statement, nameof(statement));
			Assert.ArgumentNotNull(cmd, nameof(cmd));

			_msg.Debug(statement);

			try
			{
				cmd.CommandText = statement;
				cmd.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				// Include the statement: without it the message names only the database
				// error, which is not enough to tell which script line failed.
				string message = $"{e.Message} Failed statement: {statement}";

				if (failOnError)
				{
					throw new InvalidOperationException(message, e);
				}

				_msg.Warn(message, e);
			}
		}

		private static bool IsComment([NotNull] string statement)
		{
			Assert.ArgumentNotNull(statement, nameof(statement));

			return statement.StartsWith("--") || statement.StartsWith("rem ");
		}
	}
}
