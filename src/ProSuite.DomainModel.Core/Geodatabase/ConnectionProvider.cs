using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.Geodatabase
{
	public abstract class ConnectionProvider : EntityWithMetadata,
	                                           INamed,
	                                           IAnnotated
	{
		private int _cloneId = -1;

		#region Persistent state

		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionProvider"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ConnectionProvider() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionProvider"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		protected ConnectionProvider([NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			_name = name;
		}

		#endregion

		public new int Id
		{
			get
			{
				if (base.Id < 0 && _cloneId != -1)
				{
					return _cloneId;
				}

				return base.Id;
			}
		}

		/// <summary>
		/// The clone Id can be set if this instance is a (remote) clone of a persistent ConnectionProvider.
		/// </summary>
		/// <param name="id"></param>
		public void SetCloneId(int id)
		{
			Assert.True(base.Id < 0, "Persistent entity or already initialized clone.");
			_cloneId = id;
		}

		[Required]
		[MaximumStringLength(100)]
		[UsedImplicitly]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		[UsedImplicitly]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		public override string ToString()
		{
			return Name;
		}

		public abstract DbConnectionType ConnectionType { get; }

		[NotNull]
		public virtual string TypeDescription => "Connection Provider";
	}
}
