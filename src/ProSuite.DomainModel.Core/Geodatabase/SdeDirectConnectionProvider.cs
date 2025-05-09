using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.Geodatabase
{
	public abstract class SdeDirectConnectionProvider : SdeConnectionProvider
	{
		[UsedImplicitly] private string _databaseName;

		[UsedImplicitly] private DatabaseType _databaseType = DatabaseType.SqlServer;

		#region Constructors

		protected SdeDirectConnectionProvider() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SdeDirectConnectionProvider"/> class.
		/// </summary>
		/// <param name="name">The name of the connection provider.</param>
		/// <param name="databaseType">The direct connect driver.</param>
		/// <param name="databaseName">Name of the database.</param>
		/// <param name="repositoryName">Name of the SDE repository.</param>
		protected SdeDirectConnectionProvider(string name,
		                                      DatabaseType databaseType,
		                                      [NotNull] string databaseName,
		                                      [NotNull] string repositoryName)
			: base(name, repositoryName)
		{
			Assert.ArgumentNotNullOrEmpty(databaseName, nameof(databaseName));
			Assert.ArgumentNotNullOrEmpty(repositoryName, nameof(repositoryName));

			_databaseType = databaseType;
			_databaseName = databaseName;
		}

		#endregion

		[UsedImplicitly]
		public DatabaseType DatabaseType
		{
			get { return _databaseType; }
			set { _databaseType = value; }
		}

		[UsedImplicitly]
		public string DatabaseName
		{
			get { return _databaseName; }
			set { _databaseName = value; }
		}

		public override DbConnectionType ConnectionType =>
			DbConnectionType.DatabaseConnectionProperties;

		public string DbmsTypeName
		{
			get
			{
				switch (DatabaseType)
				{
					case DatabaseType.Oracle:
					case DatabaseType.Oracle9:
					case DatabaseType.Oracle10:
					case DatabaseType.Oracle11:
						return DatabaseType.Oracle.ToString();

					default:
						return DatabaseType.ToString();
				}
			}
		}
	}
}
