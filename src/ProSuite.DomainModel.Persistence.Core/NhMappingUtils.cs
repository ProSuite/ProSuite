using System;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core;

namespace ProSuite.DomainModel.Persistence.Core
{
	public static class NhMappingUtils
	{
		public static void NotNullField([NotNull] IPropertyMapper propertyMapper,
		                                [NotNull] string columnName,
		                                [CanBeNull] int? length = null)
		{
			propertyMapper.NotNullable(true);

			Field(propertyMapper, columnName, length);
		}

		public static void Field([NotNull] IPropertyMapper propertyMapper,
		                         [NotNull] string columnName,
		                         [CanBeNull] int? length = null)
		{
			propertyMapper.Column(columnName);
			propertyMapper.Access(Accessor.Field);

			if (length != null)
			{
				propertyMapper.Length(length.Value);
			}
		}

		public static void CreateIdMapping([NotNull] IIdMapper map,
		                                   [NotNull] string columnName,
		                                   [CanBeNull] string nativeSequenceName)
		{
			map.Column(columnName);
			map.Type<Int32Type>();
			map.UnsavedValue(-1);
			map.Access(Accessor.Field);

			if (! string.IsNullOrEmpty(nativeSequenceName))
			{
				map.Generator(Generators.Native,
				              gmap => gmap.Params(new { sequence = nativeSequenceName }));
			}
			else
			{
				map.Generator(Generators.Identity);
			}
		}

		public static void CreateDiscriminatorMapping(IDiscriminatorMapper dm,
		                                              int length = 2)
		{
			dm.Column(cm => { cm.Name("TYPE"); });

			//dm.Type(NHibernateUtil.String);
			dm.NotNullable(true);
			dm.Length(length);
		}

		public static void CreateUniqueNameMapping([NotNull] IPropertyMapper pm,
		                                           [NotNull] string columnName = "NAME",
		                                           int length = 100)
		{
			pm.Column(columnName);
			pm.Length(length);
			pm.NotNullable(true);
			pm.Unique(true);
			pm.Access(Accessor.Field);
		}

		public static void CreateUuidMapping(IPropertyMapper pm, string columnName = "UUID")
		{
			pm.Column(cm =>
			{
				cm.Name(columnName);
				cm.SqlType("CHAR(36)");
			});

			pm.Length(36);
			pm.NotNullable(true);
			pm.Unique(true);
			pm.Access(Accessor.Field);
		}

		public static void CreateDescriptionMapping([NotNull] IPropertyMapper pm,
		                                            [NotNull] string columnName = "DESCRIPTION",
		                                            int length = 1000)
		{
			pm.Column(columnName);
			pm.Length(length);
			pm.Access(Accessor.Field);
		}

		public static void CreateClassDescriptorMapping(IComponentMapper<ClassDescriptor> cm,
		                                                [NotNull] string typeNameColumn,
		                                                [NotNull] string assemblyNameColumn,
		                                                [CanBeNull] string uniqueKey = null)
		{
			cm.Property(c => c.TypeName, pm =>
			{
				pm.Column(typeNameColumn);
				pm.Length(260);
				pm.NotNullable(false);

				if (uniqueKey != null)
				{
					pm.UniqueKey(uniqueKey);
				}

				pm.Access(Accessor.Field);
			});

			cm.Property(c => c.AssemblyName, pm =>
			{
				pm.Column(assemblyNameColumn);
				pm.Length(260);
				pm.NotNullable(false);

				if (uniqueKey != null)
				{
					pm.UniqueKey(uniqueKey);
				}

				pm.Access(Accessor.Field);
			});
		}

		public static void MapMetadataProperties<T>([NotNull] ClassMapping<T> mapping)
			where T : EntityWithMetadata
		{
			mapping.Property(d => d.CreatedDate, pm => { pm.Column("CREATED_DATE"); });

			mapping.Property(d => d.CreatedByUser, pm =>
			{
				pm.Column("CREATED_USER");
				pm.Length(100);
			});

			mapping.Property(d => d.LastChangedDate, pm => { pm.Column("LAST_CHANGED_DATE"); });

			mapping.Property(d => d.LastChangedByUser, pm =>
			{
				pm.Column("LAST_CHANGED_USER");
				pm.Length(100);
			});
		}

		public static void MapVersionProperty<T>(ClassMapping<T> mapping)
			where T : VersionedEntityWithMetadata
		{
			mapping.Version(d => d.Version, vm =>
			{
				vm.UnsavedValue(-1);
				vm.Column("VERSION");
				vm.Access(Accessor.Field);
			});
		}

		/// <summary>
		/// The actual DDX version. By default, assume it is up-to-date. In case a legacy DDX is
		/// encountered in the NHConfigurationBuilder it shall be set to the older value.
		/// </summary>
		public static Version ActualDdxVersion { get; set; }

		/// <summary>
		/// The most up-to-date version of the DDX, compatible with the current version of the
		/// software.
		/// </summary>
		public static Version CurrentDdxVersion { get; } = new Version(1, 0, 0, 2);
	}
}
