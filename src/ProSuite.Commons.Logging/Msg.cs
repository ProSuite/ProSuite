using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging
{
	public sealed class Msg : MsgBase
	{
		public Msg([CanBeNull] Type type) : base(type) { }
	}
}
