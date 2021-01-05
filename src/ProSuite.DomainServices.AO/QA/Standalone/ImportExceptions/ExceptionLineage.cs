using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Standalone.ImportExceptions
{
	public class ExceptionLineage
	{
		[NotNull] private readonly List<ManagedExceptionVersion> _exceptionObjects =
			new List<ManagedExceptionVersion>();

		public ExceptionLineage(Guid uuid)
		{
			Assert.ArgumentCondition(! Equals(Guid.Empty, uuid), "uuid must not be empty");

			Uuid = uuid;
		}

		public Guid Uuid { get; }

		public void Include([NotNull] ManagedExceptionVersion exceptionVersion)
		{
			Assert.ArgumentNotNull(exceptionVersion, nameof(exceptionVersion));
			Assert.ArgumentCondition(Uuid == exceptionVersion.LineageUuid,
			                         "not of this lineage");

			_exceptionObjects.Add(exceptionVersion);
		}

		[CanBeNull]
		public ManagedExceptionVersion Current
		{
			get
			{
				// TODO what if there is more than one?
				return _exceptionObjects.FirstOrDefault(
					obj => obj.VersionEndDate == null);
			}
		}

		[CanBeNull]
		public ManagedExceptionVersion GetVersion(Guid versionUuid)
		{
			return _exceptionObjects.FirstOrDefault(obj => obj.VersionUuid == versionUuid);
		}

		[NotNull]
		public IEnumerable<ManagedExceptionVersion> GetAll()
		{
			return _exceptionObjects;
		}
	}
}
