using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.AO.Geodatabase
{
	[CLSCompliant(false)]
	public abstract class ConnectionProvider : EntityWithMetadata,
	                                           IOpenWorkspace,
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
		protected ConnectionProvider(string name)
		{
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

		[NotNull]
		public virtual string TypeDescription => "Connection Provider";

		/// <summary>
		/// Opens the workspace. 
		/// </summary>
		/// <param name="hWnd">The window handle of the parent window.</param>
		/// <remarks>Always opens the workspace from the factory. 
		/// Can therefore be used on background threads.</remarks>
		/// <returns></returns>
		public abstract IFeatureWorkspace OpenWorkspace(int hWnd = 0);
	}
}
