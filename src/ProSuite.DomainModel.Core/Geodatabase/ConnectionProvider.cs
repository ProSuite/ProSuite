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
		[UsedImplicitly] private string _name;

		[UsedImplicitly] private string _description;

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
