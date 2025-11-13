using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Keyboard;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.Commands
{
	public class CommandDescriptor : EntityWithMetadata, INamed, IAnnotated,
	                                 IEquatable<CommandDescriptor>, ICommandDescriptor
	{
		[UsedImplicitly] [NotNull] private readonly string _clsid;
		[UsedImplicitly] private readonly int? _subtype;

		[UsedImplicitly] private KeyboardShortcut
			_keyboardShortcut; // todo should be a string like "Alt-Shift-Q" and utils to translate to KeyboardShortcut

		[UsedImplicitly] private string _category;
		[UsedImplicitly] private string _message;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private string _name;
		[UsedImplicitly] private CommandType _commandType;

		private CommandKey _key;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandDescriptor"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected CommandDescriptor() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandDescriptor"/> class.
		/// </summary>
		/// <param name="clsid">The CLSID of the command.</param>
		/// <param name="subtype">The optional command subtype.</param>
		/// <param name="commandType">Type of the command.</param>
		/// <param name="name">The name.</param>
		/// <param name="category">The category.</param>
		/// <param name="message">The message.</param>
		public CommandDescriptor(Guid clsid, int? subtype, CommandType commandType,
		                         string name,
		                         string category, string message)
			: this(clsid, subtype, commandType, name, category, message, message) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandDescriptor"/> class.
		/// </summary>
		/// <param name="clsid">The CLSID of the command.</param>
		/// <param name="subtype">The optional command subtype.</param>
		/// <param name="commandType">Type of the command.</param>
		/// <param name="name">The name.</param>
		/// <param name="category">The category.</param>
		/// <param name="message">The message.</param>
		/// <param name="description">The description.</param>
		public CommandDescriptor(Guid clsid, int? subtype, CommandType commandType,
		                         string name,
		                         string category, string message, string description)
		{
			Assert.False(clsid.Equals(Guid.Empty), "empty guid");

			_clsid = GetCLSIDString(clsid);

			_subtype = subtype;
			_name = name;
			_category = category;
			_message = message;
			_description = description;
			_commandType = commandType;
		}

		#endregion

		public string Identifier => _clsid + (_subtype != null ? ":" + _subtype : string.Empty);

		[NotNull]
		public static string GetCLSIDString(Guid clsid)
		{
			return clsid.ToString("B").ToUpper();
		}

		[NotNull]
		public CommandKey Key
		{
			get { return _key ?? (_key = new CommandKey(CLSID, _subtype)); }
		}

		[Required]
		public Guid CLSID
		{
			get { return new Guid(_clsid); }
		}

		[NotNull]
		public string CLSIDText
		{
			get { return _clsid; }
		}

		public string Category
		{
			get { return _category; }
			set { _category = value; }
		}

		public CommandType CommandType
		{
			get { return _commandType; }
			set { _commandType = value; }
		}

		public string Message
		{
			get { return _message; }
			set { _message = value; }
		}

		public KeyboardShortcut KeyboardShortcut
		{
			get { return _keyboardShortcut; }
			set { _keyboardShortcut = value; }
		}

		public int? Subtype
		{
			get { return _subtype; }
		}

		#region IAnnotated Members

		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		#endregion

		#region INamed Members

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		#endregion

		public override string ToString()
		{
			return string.Format("{0}{1} - {2} - {3}", _name,
			                     _subtype != null
				                     ? string.Format(" subtype:{0}", _subtype)
				                     : string.Empty,
			                     _category, _message);
		}

		public bool Equals(CommandDescriptor commandDescriptor)
		{
			if (commandDescriptor == null)
			{
				return false;
			}

			return
				Equals(_clsid, commandDescriptor._clsid) &&
				Equals(_subtype, commandDescriptor._subtype);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as CommandDescriptor);
		}

		public override int GetHashCode()
		{
			return _clsid.GetHashCode() + 29 * _subtype.GetHashCode();
		}
	}
}
