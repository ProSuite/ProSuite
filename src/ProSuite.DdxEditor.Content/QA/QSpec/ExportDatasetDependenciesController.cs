using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	internal class ExportDatasetDependenciesController :
		IExportDatasetDependenciesObserver
	{
		private readonly IExportDatasetDependenciesView _view;
		private readonly Latch _latch = new Latch();

		private const string _extension = ".graphml";

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ExportDatasetDependenciesController"/> class.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="qualitySpecifications">The quality specifications.</param>
		public ExportDatasetDependenciesController(
			[NotNull] IExportDatasetDependenciesView view,
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications)
			: this(view, qualitySpecifications,
			       new List<QualitySpecification>()) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ExportDatasetDependenciesController"/> class.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="qualitySpecifications">The quality specifications.</param>
		/// <param name="selectedQualitySpecifications">The initially selected quality specifications.</param>
		public ExportDatasetDependenciesController(
			[NotNull] IExportDatasetDependenciesView view,
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications,
			[NotNull] IEnumerable<QualitySpecification> selectedQualitySpecifications)
		{
			Assert.ArgumentNotNull(view, nameof(view));
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNull(selectedQualitySpecifications,
			                       nameof(selectedQualitySpecifications));

			_view = view;
			_view.Observer = this;

			_view.BindTo(GetItems(qualitySpecifications));

			_latch.RunInsideLatch(() => _view.Select(GetItems(selectedQualitySpecifications)));

			UpdateAppearance();
		}

		#endregion

		void IExportDatasetDependenciesObserver.ExportTargetChanged()
		{
			UpdateAppearance();
		}

		void IExportDatasetDependenciesObserver.FilePathChanged()
		{
			UpdateAppearance();
		}

		void IExportDatasetDependenciesObserver.DirectoryPathChanged()
		{
			UpdateAppearance();
		}

		void IExportDatasetDependenciesObserver.FilePathFocusLost()
		{
			string filePath = _view.CurrentFilePath;
			if (string.IsNullOrEmpty(filePath))
			{
				return;
			}

			string completePath;
			if (! IsCompleteFilePath(filePath, out completePath))
			{
				_view.CurrentFilePath = completePath;
				UpdateAppearance();
			}
		}

		void IExportDatasetDependenciesObserver.DirectoryPathFocusLost()
		{
			string directoryPath = _view.CurrentDirectoryPath;
			if (string.IsNullOrEmpty(directoryPath))
			{
				return;
			}

			string rootedPath;
			if (! IsPathRooted(directoryPath, out rootedPath))
			{
				_view.CurrentDirectoryPath = rootedPath;
				UpdateAppearance();
			}
		}

		void IExportDatasetDependenciesObserver.OKClicked()
		{
			switch (_view.CurrentExportTarget)
			{
				case ExportTarget.SingleFile:
					ApplySingleFile();
					break;

				case ExportTarget.MultipleFiles:
					ApplyMultipleFiles();
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		void IExportDatasetDependenciesObserver.CancelClicked()
		{
			_view.SetCancelResult();

			_view.Close();
		}

		void IExportDatasetDependenciesObserver.SelectAllClicked()
		{
			_latch.RunInsideLatch(_view.SelectAll);

			UpdateAppearance();
		}

		void IExportDatasetDependenciesObserver.SelectNoneClicked()
		{
			_latch.RunInsideLatch(_view.SelectNone);

			UpdateAppearance();
		}

		void IExportDatasetDependenciesObserver.SelectedItemsChanged()
		{
			if (_latch.IsLatched)
			{
				return;
			}

			UpdateAppearance(_view.SelectedItems.Count);
		}

		void IExportDatasetDependenciesObserver.ExportWorkspaceConnectionsChanged()
		{
			UpdateAppearance();
		}

		private void ApplyMultipleFiles()
		{
			string directoryPath = _view.CurrentDirectoryPath;
			Assert.NotNull(directoryPath, "directoryPath");

			string completePath;
			if (! IsPathRooted(directoryPath, out completePath))
			{
				directoryPath = completePath;
				_view.CurrentDirectoryPath = directoryPath;

				UpdateAppearance();
			}

			if (! ValidateDirectoryPath(directoryPath))
			{
				return;
			}

			IList<QualitySpecification> selection = GetSelectedQualitySpecifications();
			Assert.True(selection.Count > 0, "nothing selected");

			char[] invalidCharacters = Path.GetInvalidFileNameChars();
			const char replacementCharacter = '_';

			var result = new Dictionary<string, ICollection<QualitySpecification>>();
			var deletableFiles = new List<string>();

			foreach (QualitySpecification qualitySpecification in selection)
			{
				string fileName = GenerateFileName(qualitySpecification,
				                                   invalidCharacters,
				                                   replacementCharacter);

				string filePath = Path.Combine(directoryPath, fileName);

				filePath = EnsureUniqueFilePath(filePath, f => result.ContainsKey(f));

				if (File.Exists(filePath))
				{
					deletableFiles.Add(filePath);
				}

				result.Add(filePath, new[] {qualitySpecification});
			}

			if (deletableFiles.Count > 0 && ! ConfirmDeletion(deletableFiles))
			{
				return;
			}

			_view.SetOKResult(result, deletableFiles);
			_view.Close();
		}

		[NotNull]
		private static string EnsureUniqueFilePath([NotNull] string filePath,
		                                           [NotNull] Predicate<string> existsFile)
		{
			if (! existsFile(filePath))
			{
				// already unique
				return filePath;
			}

			string directory = Assert.NotNull(Path.GetDirectoryName(filePath));

			var i = 1;
			while (true)
			{
				string fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);

				string newFileNameNoExt = string.Format("{0}-{1}{2}", fileNameNoExt, i, _extension);

				string candidate = Path.Combine(directory, newFileNameNoExt);

				if (! existsFile(candidate))
				{
					return candidate;
				}

				i++;
			}
		}

		private static string GenerateFileName(
			[NotNull] QualitySpecification qualitySpecification,
			[NotNull] IEnumerable<char> invalidCharacters,
			char replacementCharacter)
		{
			string fileName = qualitySpecification.Name;

			if (StringUtils.IsNullOrEmptyOrBlank(fileName))
			{
				throw new InvalidOperationException(
					string.Format(
						"Quality specification with id={0} has undefined name, unable to generate file name",
						qualitySpecification.Id));
			}

			foreach (char invalidCharacter in invalidCharacters)
			{
				if (fileName.IndexOf(invalidCharacter) >= 0)
				{
					// The resulting file names may no longer be unique
					fileName = fileName.Replace(invalidCharacter, replacementCharacter);
				}
			}

			string correctedFilePath;
			if (! HasExpectedSuffix(fileName, out correctedFilePath))
			{
				fileName = correctedFilePath;
			}

			return fileName;
		}

		private void ApplySingleFile()
		{
			string filePath = _view.CurrentFilePath;
			Assert.NotNull(filePath, "filePath");

			string completePath;
			if (! IsCompleteFilePath(filePath, out completePath))
			{
				filePath = completePath;
				_view.CurrentFilePath = filePath;

				UpdateAppearance();
			}

			if (! ValidateFilePath(filePath))
			{
				return;
			}

			IList<QualitySpecification> selection = GetSelectedQualitySpecifications();
			Assert.True(selection.Count > 0, "nothing selected");

			var deletableFiles = new List<string>();
			if (File.Exists(filePath))
			{
				deletableFiles.Add(filePath);
			}

			if (deletableFiles.Count > 0 && ! ConfirmDeletion(deletableFiles))
			{
				return;
			}

			var result = new Dictionary<string, ICollection<QualitySpecification>>
			             {
				             {filePath, selection}
			             };

			_view.SetOKResult(result, deletableFiles);

			_view.Close();
		}

		private bool ConfirmDeletion([NotNull] IList<string> fileNames)
		{
			string message;
			if (fileNames.Count == 1)
			{
				message = string.Format("The file already exists:" +
				                        "{0}{0}- {1}{0}{0}" +
				                        "Overwrite?",
				                        Environment.NewLine, fileNames[0]);
			}
			else
			{
				string list = StringUtils.Concatenate(fileNames,
				                                      f => string.Format("- {0}", f),
				                                      Environment.NewLine);

				message = string.Format("The following files already exist:" +
				                        "{0}{0}{1}{0}{0}" +
				                        "Overwrite?",
				                        Environment.NewLine, list);
			}

			return _view.Confirm(message);
		}

		private static bool IsCompleteFilePath([NotNull] string filePath,
		                                       [NotNull] out string completePath)
		{
			string changedPath;

			string suffixedPath;
			if (HasExpectedSuffix(filePath, out suffixedPath))
			{
				string rootedPath;
				changedPath = IsPathRooted(filePath, out rootedPath)
					              ? null // no change
					              : rootedPath;
			}
			else
			{
				string rootedPath;
				changedPath = IsPathRooted(suffixedPath, out rootedPath)
					              ? suffixedPath
					              : rootedPath;
			}

			if (changedPath == null)
			{
				completePath = filePath;
				return true;
			}

			completePath = changedPath;
			return false;
		}

		private static bool IsPathRooted([NotNull] string path,
		                                 [NotNull] out string correctedPath)
		{
			if (Path.IsPathRooted(path))
			{
				correctedPath = path;
				return true;
			}

			// TODO revise - should be writable; if not: "My documents"?
			string defaultParentDirectory = Environment.CurrentDirectory;

			correctedPath = Path.GetFullPath(Path.Combine(defaultParentDirectory, path));
			return false;
		}

		private static bool HasExpectedSuffix([NotNull] string filePath,
		                                      [NotNull] out string correctedFilePath)
		{
			Assert.ArgumentNotNull(filePath, nameof(filePath));

			string trimmedPath = filePath.Trim();

			if (string.Equals(Path.GetExtension(trimmedPath), _extension,
			                  StringComparison.OrdinalIgnoreCase))
			{
				correctedFilePath = filePath;
				return true;
			}

			correctedFilePath = Path.ChangeExtension(trimmedPath, _extension);
			return false;
		}

		private void UpdateAppearance()
		{
			UpdateAppearance(_view.SelectedItems.Count);
		}

		private void UpdateAppearance(int selectionCount)
		{
			bool hasValidTarget;
			switch (_view.CurrentExportTarget)
			{
				case ExportTarget.SingleFile:
					_view.FilePathEnabled = true;
					_view.DirectoryPathEnabled = false;

					hasValidTarget = ValidateFilePath(_view.CurrentFilePath);
					break;

				case ExportTarget.MultipleFiles:
					_view.FilePathEnabled = false;
					_view.DirectoryPathEnabled = true;

					hasValidTarget = ValidateDirectoryPath(_view.CurrentDirectoryPath);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			_view.OKEnabled = selectionCount > 0 && hasValidTarget;

			_view.SelectNoneEnabled = selectionCount > 0;
			_view.SelectAllEnabled = _view.ItemCount > selectionCount;

			string format = _view.ItemCount == 1
				                ? "{0} of {1} quality specification selected"
				                : "{0} of {1} quality specifications selected";

			_view.StatusText = string.Format(format, selectionCount, _view.ItemCount);
		}

		private bool ValidateDirectoryPath([CanBeNull] string directoryPath)
		{
			if (StringUtils.IsNullOrEmptyOrBlank(directoryPath))
			{
				return false;
			}

			if (directoryPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
			{
				_view.SetCurrentDirectoryPathError(
					"The directory path contains invalid characters");
				return false;
			}

			_view.SetCurrentDirectoryPathError(null);
			return true;
		}

		private bool ValidateFilePath([CanBeNull] string filePath)
		{
			if (StringUtils.IsNullOrEmptyOrBlank(filePath))
			{
				return false;
			}

			if (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
			{
				_view.SetCurrentFilePathError("The file path contains invalid characters");
				return false;
			}

			_view.SetCurrentFilePathError(null);
			return true;
		}

		[NotNull]
		private static IEnumerable<QualitySpecificationListItem> GetItems(
			[NotNull] IEnumerable<QualitySpecification> qualitySpecifications)
		{
			List<QualitySpecificationListItem> items = qualitySpecifications.Select(
					qspec =>
						new
							QualitySpecificationListItem(
								qspec))
				.ToList();

			items.Sort();

			return items;
		}

		[NotNull]
		private IList<QualitySpecification> GetSelectedQualitySpecifications()
		{
			return _view.SelectedItems.Select(item => item.QualitySpecification)
			            .ToList();
		}
	}
}
