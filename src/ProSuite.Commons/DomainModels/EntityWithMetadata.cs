using System;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	public abstract class EntityWithMetadata : Entity, IEntityMetadata
	{
		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private DateTime? _lastChangedDate;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private DateTime? _createdDate;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _lastChangedByUser;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _createdByUser;

		#region Implementation of IEntityMetadata

		public DateTime? LastChangedDate
		{
			get { return _lastChangedDate; }
			set { _lastChangedDate = value; }
		}

		public DateTime? CreatedDate
		{
			get { return _createdDate; }
			set { _createdDate = value; }
		}

		public string LastChangedByUser
		{
			get { return _lastChangedByUser; }
			set { _lastChangedByUser = value; }
		}

		public string CreatedByUser
		{
			get { return _createdByUser; }
			set { _createdByUser = value; }
		}

		#endregion
	}
}