using System.IO;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core
{
	/// <summary>
	/// Represents an ArcMap layer file, as an immutable value type
	/// </summary>
	public class LayerFile
	{
		[UsedImplicitly] private string _fileName;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LayerFile"/> class.
		/// </summary>
		/// <remarks>Needed for nhibernate.</remarks>
		protected LayerFile() : this(null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="LayerFile"/> class.
		/// </summary>
		/// <param name="fileName">Full path to the layerfile (optional).</param>
		public LayerFile([CanBeNull] string fileName)
		{
			_fileName = fileName;
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Gets or sets the layer file path.
		/// </summary>
		/// <value>The full path to the layer file.</value>
		/// <remarks>Setter required for screen binding</remarks>
		[CanBeNull]
		[UsedImplicitly]
		public string FileName
		{
			get { return _fileName; }
			set { _fileName = value; }
		}

		public bool Exists => ! string.IsNullOrEmpty(_fileName) && File.Exists(_fileName);

		public override string ToString()
		{
			return _fileName ?? "<filename not defined>";
		}

		public override int GetHashCode()
		{
			return _fileName?.GetHashCode() ?? 0;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var layerFile = obj as LayerFile;
			if (layerFile == null)
			{
				return false;
			}

			if (! Equals(_fileName, layerFile._fileName))
			{
				return false;
			}

			return true;
		}

		#endregion
	}
}