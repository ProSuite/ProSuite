using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.Geodatabase
{
	public abstract class SdeConnectionProvider : ConnectionProvider,
	                                              IAlternateCredentials
	{
		private const string _defaultRepositoryName = "SDE";

		[UsedImplicitly] private string _repositoryName;

		[UsedImplicitly] private string _versionName;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="SdeConnectionProvider"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected SdeConnectionProvider() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SdeConnectionProvider"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="repositoryName">Name of the repository.</param>
		protected SdeConnectionProvider([NotNull] string name, [NotNull] string repositoryName) :
			base(name)
		{
			Assert.ArgumentNotNullOrEmpty(repositoryName, nameof(repositoryName));

			_repositoryName = repositoryName;
		}

		#endregion

		[Required]
		[UsedImplicitly]
		public string RepositoryName
		{
			get { return _repositoryName; }
			set { _repositoryName = value; }
		}

		[CanBeNull]
		[UsedImplicitly]
		public string VersionName
		{
			get { return _versionName; }
			set { _versionName = value; }
		}

		public string AlternateUserName { get; private set; }

		public string AlternatePassword { get; private set; }

		#region IAlternateCredentials Members

		public void SetAlternateCredentials(string userName, string password)
		{
			Assert.ArgumentNotNullOrEmpty(userName, nameof(userName));
			Assert.ArgumentNotNullOrEmpty(password, nameof(password));

			AlternateUserName = userName;
			AlternatePassword = password;
		}

		public void ClearAlternateCredentials()
		{
			AlternateUserName = null;
			AlternatePassword = null;
		}

		public bool HasAlternateCredentials => AlternateUserName != null;

		#endregion

		#region Non-public members

		[NotNull]
		protected static string DefaultRepositoryName => _defaultRepositoryName;

		#endregion
	}
}
