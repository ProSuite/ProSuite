using System;
using System.Xml;
using System.Xml.Linq;

namespace ProSuite.Commons.Xml
{
	/// <summary>
	/// Convenience wrapper around <see cref="XElement"/>, providing equality,
	/// ToString() and error utilities. Useful to derive specialised wrappers
	/// that provide typed access XML (using the new XML DOM).
	/// </summary>
	public abstract class XmlWrapperBase : IEquatable<XmlWrapperBase>
	{
		protected XElement Xml { get; }

		protected XmlWrapperBase(XElement xml)
		{
			Xml = xml ?? throw new ArgumentNullException(nameof(xml));
		}

		public bool Equals(XmlWrapperBase other)
		{
			if (other is null) return false;
			return XNode.DeepEquals(Xml, other.Xml);
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			var other = obj as XmlWrapperBase;
			if (other is null) return false;
			return XNode.DeepEquals(Xml, other.Xml);
		}

		public override int GetHashCode()
		{
			return Xml != null ? Xml.GetHashCode() : 0;
		}

		/// <remarks>Appends line info, if available</remarks>
		protected FormatException FormatError(string message)
		{
			return new FormatException(AppendLineInfo(message ?? "error"));
		}

		public string AppendLineInfo(string message)
		{
			if (message is null) return null;

			if (Xml is IXmlLineInfo info && info.HasLineInfo())
			{
				message = $"{message} (line {info.LineNumber}, position {info.LinePosition})";
			}

			return message;
		}

		public override string ToString()
		{
			return Xml.ToString();
		}
	}
}
