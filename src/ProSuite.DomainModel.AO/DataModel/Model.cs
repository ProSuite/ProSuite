using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class Model : DdxModel
	{
		public const string EnvironmentVariableNoModelSchemaCache =
			"PROSUITE_NO_MODEL_SCHEMA_CACHE";

		#region Fields

		private bool _keepDatasetLocks;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Model"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected Model() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Model"/> class.
		/// </summary>
		/// <param name="name">The name of the model.</param>
		protected Model(string name) : base(name) { }

		#endregion

		public object CachedMasterDatabaseWorkspaceContext { get; set; }
		public bool? CachedIsMasterDatabaseAccessible { get; set; }
		public string CachedMasterDatabaseNoAccessReason { get; set; }

		public bool KeepDatasetLocks
		{
			get { return _keepDatasetLocks; }
			set
			{
				if (value == _keepDatasetLocks)
				{
					return;
				}

				// Value changed. Discard current workspace proxy
				_keepDatasetLocks = value;

				//if (_masterDatabaseWorkspaceContext == null)
				if (CachedMasterDatabaseWorkspaceContext == null)
				{
					return;
				}

				//_masterDatabaseWorkspaceContext = null;
				CachedMasterDatabaseWorkspaceContext = null;

				if (_keepDatasetLocks)
				{
					return;
				}

				// make sure any locks are released immediately
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		[NotNull]
		public abstract string TranslateToModelElementName(
			[NotNull] string masterDatabaseDatasetName);

		public override string ToString()
		{
			return Name ?? "<no name>";
		}

		#region Non-public members

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual SpatialReferenceDescriptor CreateDefaultSpatialReferenceDescriptor() // TODO Drop? No usages in Topgis (nor in ProSuite/GoTop)
		{
			return null;
		}

		[PublicAPI]
		public bool DisableAutomaticSchemaCaching { get; set; }

		public virtual bool AutoEnableSchemaCache => false; // TODO public (was: protected)

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual IEnumerable<ConnectionProvider> GetConnectionProvidersCore() // TODO Drop? No usages in Topgis (nor in ProSuite/GoTop)
		{
			return new List<ConnectionProvider>();
		}

		#endregion
	}
}
