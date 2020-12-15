using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	[CLSCompliant(false)]
	public abstract class SdeConnectionProvider : ConnectionProvider,
	                                              IOpenSdeWorkspace,
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
		protected SdeConnectionProvider(string name, string repositoryName) : base(name)
		{
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

		public abstract IFeatureWorkspace OpenWorkspace(string versionName, int hWnd = 0);

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
