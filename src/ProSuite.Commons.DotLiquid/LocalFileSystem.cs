using System;
using System.IO;
using System.Text.RegularExpressions;
using DotLiquid;
using DotLiquid.Exceptions;
using DotLiquid.FileSystems;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DotLiquid
{
	/// <summary>
	/// Replacement for the DotLiquid's default local file system
	/// </summary>
	public class LocalFileSystem : IFileSystem
	{
		[NotNull] private readonly string _root;
		[NotNull] private readonly Regex _validTemplateNameRegex;
		[NotNull] private readonly Regex _validTemplatePathRegex;

		public LocalFileSystem([NotNull] string root)
		{
			Assert.ArgumentNotNullOrEmpty(root, nameof(root));

			_root = root;

			_validTemplateNameRegex = new Regex(@"^[^.\/][a-zA-Z0-9_\/]+$",
			                                    RegexOptions.Compiled);

			_validTemplatePathRegex = new Regex(string.Format("^{0}",
			                                                  _root.Replace(@"\", @"\\")
			                                                       .Replace("(", @"\(")
			                                                       .Replace(")", @"\)")),
			                                    RegexOptions.Compiled |
			                                    RegexOptions.IgnoreCase);
		}

		[CLSCompliant(false)]
		public string ReadTemplateFile(Context context, string templateName)
		{
			Assert.ArgumentNotNull(context, nameof(context));
			Assert.ArgumentNotNullOrEmpty(templateName, nameof(templateName));

			var templatePath = (string) context[templateName];

			string fullPath = FullPath(templatePath);

			if (! File.Exists(fullPath))
			{
				throw new FileSystemException("Template not found", templatePath);
			}

			return File.ReadAllText(fullPath);
		}

		public string FullPath([NotNull] string templatePath)
		{
			Assert.ArgumentNotNull(templatePath, nameof(templatePath));
			AssertValidTemplateName(templatePath);

			string fullPath;
			if (templatePath.Contains("/"))
			{
				string directory = Assert.NotNull(Path.GetDirectoryName(templatePath));

				fullPath = Path.Combine(Path.Combine(_root, directory),
				                        GetTemplateFileName(Path.GetFileName(templatePath)));
			}
			else
			{
				fullPath = Path.Combine(_root, GetTemplateFileName(templatePath));
			}

			AssertValidTemplatePath(fullPath);

			return fullPath;
		}

		[NotNull]
		private static string GetTemplateFileName([NotNull] string templateName)
		{
			return $"_{templateName}.liquid";
		}

		private void AssertValidTemplateName([NotNull] string templatePath)
		{
			if (! _validTemplateNameRegex.IsMatch(templatePath))
			{
				throw new FileSystemException("Illegal template name", templatePath);
			}
		}

		private void AssertValidTemplatePath([NotNull] string fullPath)
		{
			string absolutePath = Path.GetFullPath(fullPath);

			if (! _validTemplatePathRegex.IsMatch(absolutePath))
			{
				throw new FileSystemException("Illegal template path", absolutePath);
			}
		}
	}
}