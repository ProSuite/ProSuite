using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.Geodatabase
{
	public class SdeDirectDbUserConnectionProvider : SdeDirectConnectionProvider
	{
		[CanBeNull] [UsedImplicitly] private EncryptedString _encryptedPassword;

		[UsedImplicitly] private string _userName;

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public SdeDirectDbUserConnectionProvider() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SdeDirectDbUserConnectionProvider"/> class, 
		/// using the default SDE repository name.
		/// </summary>
		/// <param name="databaseType">Type of the database.</param>
		/// <param name="databaseName">Name of the database.</param>
		/// <param name="userName">Name of the user.</param>
		/// <param name="plainTextPassword">The plain text password.</param>
		public SdeDirectDbUserConnectionProvider(DatabaseType databaseType,
		                                         [NotNull] string databaseName,
		                                         [NotNull] string userName,
		                                         [NotNull] string plainTextPassword)
			: this(databaseType, databaseName,
			       userName, plainTextPassword,
			       DefaultRepositoryName) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SdeDirectDbUserConnectionProvider"/> class.
		/// </summary>
		/// <param name="databaseType">Type of the database.</param>
		/// <param name="databaseName">Name of the database.</param>
		/// <param name="userName">Name of the user.</param>
		/// <param name="plainTextPassword">The plain text password.</param>
		/// <param name="repositoryName">Name of the SDE repository owner.</param>
		public SdeDirectDbUserConnectionProvider(DatabaseType databaseType,
		                                         [NotNull] string databaseName,
		                                         [NotNull] string userName,
		                                         [NotNull] string plainTextPassword,
		                                         [NotNull] string repositoryName)
			: base(GetDefaultName(databaseName, repositoryName, userName),
			       databaseType, databaseName, repositoryName)
		{
			Assert.ArgumentNotNullOrEmpty(userName, nameof(userName));
			Assert.ArgumentNotNull(plainTextPassword, nameof(plainTextPassword));

			_userName = userName;
			_encryptedPassword = new EncryptedString
			                     {
				                     PlainTextValue = plainTextPassword
			                     };
		}

		#endregion

		[Required]
		[UsedImplicitly]
		public string PlainTextPassword
		{
			get
			{
				return _encryptedPassword != null
					       ? _encryptedPassword.PlainTextValue
					       : string.Empty;
			}
			set
			{
				if (_encryptedPassword == null)
				{
					_encryptedPassword = new EncryptedString();
				}

				_encryptedPassword.PlainTextValue = value;
			}
		}

		[Required]
		[UsedImplicitly]
		public string UserName
		{
			get { return _userName; }
			set { _userName = value; }
		}

		[UsedImplicitly]
		private EncryptedString EncryptedPassword => _encryptedPassword;

		public string EncryptedPasswordValue
		{
			get => EncryptedPassword.EncryptedValue;
			set => EncryptedPassword.EncryptedValue = value;
		}

		[NotNull]
		private static string GetDefaultName([NotNull] string databaseName,
		                                     [NotNull] string repositoryName,
		                                     [NotNull] string userName)
		{
			return $"{databaseName}:{repositoryName}:{userName}";
		}

		public override string TypeDescription => "SDE Direct Username/Password Connection";
	}
}
