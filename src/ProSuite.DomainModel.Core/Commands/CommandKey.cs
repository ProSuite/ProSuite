using System;

namespace ProSuite.DomainModel.Core.Commands
{
	public class CommandKey : IEquatable<CommandKey>
	{
		private readonly Guid _clsid;
		private readonly int? _subtype;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandKey"/> struct.
		/// </summary>
		/// <param name="clsid">The CLSID.</param>
		/// <param name="subtype">The subtype.</param>
		public CommandKey(Guid clsid, int? subtype)
		{
			_clsid = clsid;
			_subtype = subtype;
		}

		#region IEquatable<CommandKey> Members

		public bool Equals(CommandKey commandKey)
		{
			if (commandKey is null) return false;

			return Equals(_clsid, commandKey._clsid) &&
			       Equals(_subtype, commandKey._subtype);
		}

		#endregion

		public override string ToString()
		{
			string clsid = _clsid.ToString("B");

			return _subtype == null
				       ? clsid
				       : string.Format("{0}:{1}", clsid, _subtype.Value);
		}

		public override bool Equals(object obj)
		{
			return obj is CommandKey key && Equals(key);
		}

		public override int GetHashCode()
		{
			return _clsid.GetHashCode() + 29 * _subtype.GetHashCode();
		}
	}
}
