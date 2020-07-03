using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Keyboard
{
	/// <summary>
	/// Represents a keyboard shortcut. 
	/// </summary>
	/// <remarks>Available in this assembly (and not Commons.UI) since this
	/// may be used from domain classes also, which are UI-ignorant.</remarks>
	[Serializable]
	public class KeyboardShortcut : IEquatable<KeyboardShortcut>
	{
		[UsedImplicitly] private readonly bool _alt;
		[UsedImplicitly] private readonly bool _control;
		[UsedImplicitly] private readonly int _key;
		[UsedImplicitly] private readonly bool _shift;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="KeyboardShortcut"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected KeyboardShortcut() { }

		public KeyboardShortcut(int key, bool control = false, bool shift = false,
		                        bool alt = false)
		{
			_key = key;
			_control = control;
			_shift = shift;
			_alt = alt;
		}

		#endregion

		public int Key => _key;

		public bool Control => _control;

		public bool Shift => _shift;

		public bool Alt => _alt;

		public bool UsesModifierKeys => _control || _shift || _alt;

		#region IEquatable<KeyboardShortcut> Members

		public bool Equals(KeyboardShortcut keyboardShortcut)
		{
			if (keyboardShortcut == null)
			{
				return false;
			}

			if (_key != keyboardShortcut._key)
			{
				return false;
			}

			if (! Equals(_control, keyboardShortcut._control))
			{
				return false;
			}

			if (! Equals(_shift, keyboardShortcut._shift))
			{
				return false;
			}

			if (! Equals(_alt, keyboardShortcut._alt))
			{
				return false;
			}

			return true;
		}

		#endregion

		public override string ToString()
		{
			return string.Format("key={0} ctrl={1} shift={2} alt={3}",
			                     _key, _control, _shift, _alt);
		}

		public static bool operator !=(
			KeyboardShortcut keyboardShortcut1, KeyboardShortcut keyboardShortcut2)
		{
			return ! Equals(keyboardShortcut1, keyboardShortcut2);
		}

		public static bool operator ==(
			KeyboardShortcut keyboardShortcut1, KeyboardShortcut keyboardShortcut2)
		{
			return Equals(keyboardShortcut1, keyboardShortcut2);
		}

		public override bool Equals(object obj)
		{
			return ReferenceEquals(this, obj) || Equals(obj as KeyboardShortcut);
		}

		public override int GetHashCode()
		{
			int result = _key;
			result = 29 * result + _control.GetHashCode();
			result = 29 * result + _shift.GetHashCode();
			result = 29 * result + _alt.GetHashCode();
			return result;
		}
	}
}