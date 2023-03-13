using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public interface IUniqueIdObject
	{
		[CanBeNull]
		UniqueId UniqueId { get; }
	}
	[Obsolete("Move to separate file")]
	public interface IUniqueIdObjectEdit
	{
		[CanBeNull]
		UniqueId UniqueId { get; set; }
	}
}
