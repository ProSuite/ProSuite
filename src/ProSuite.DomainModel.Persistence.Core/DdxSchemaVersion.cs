using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Persistence.Core
{
	public class DdxSchemaVersion : Entity
	{
		public static int WellKnownId => 1;

		public DdxSchemaVersion()
		{
			// For NHibernate
			IsPersistent = true;
		}

		public DdxSchemaVersion(Version version) : this()
		{
			Major = GetValidValue(version.Major);
			Minor = GetValidValue(version.Minor);
			Build = GetValidValue(version.Build);
			Revision = GetValidValue(version.Revision);

			// The only allowed value with a well-known ID:
			// The ID mapping must be 'Assigned'
			((IEntityTest) this).SetId(WellKnownId);
			IsPersistent = false;
		}

		// Probably not necessary
		public new bool IsPersistent { get; set; }

		/// <summary>
		/// Returns at least 0. Some parts of the version can be -1 which
		/// should be avoided.
		/// </summary>
		/// <param name="versionPart"></param>
		/// <returns></returns>
		private static int GetValidValue(int versionPart)
		{
			return Math.Max(0, versionPart);
		}

		[UsedImplicitly]
		public int Major { get; set; }

		[UsedImplicitly]
		public int Minor { get; set; }

		[UsedImplicitly]
		public int Build { get; set; }

		[UsedImplicitly]
		public int Revision { get; set; }

		public Version Version => new Version(Major, Minor, Build, Revision);

		/// <summary>
		/// The first version that supports filters and transformers.
		/// </summary>
		public static Version FiltersAndTransformers { get; set; } = new Version(1, 0);

		/// <summary>
		/// The first version that supports simple terrains.
		/// </summary>
		public static Version SimpleTerrains { get; set; } = new Version(1, 0);
	}
}
