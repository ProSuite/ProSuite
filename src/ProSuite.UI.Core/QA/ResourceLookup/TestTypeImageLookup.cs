using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.Properties;

namespace ProSuite.UI.Core.QA.ResourceLookup
{
	public static class TestTypeImageLookup
	{
		private static readonly SortedList<string, Image> _mapKeyToImage =
			new SortedList<string, Image>();

		private static readonly Dictionary<Image, string> _mapImageToKey =
			new Dictionary<Image, string>();

		private static readonly SortedList<string, int> _defaultSort =
			new SortedList<string, int>();

		private const string _keyWarning = "warning";
		private const string _keyError = "error";
		private const string _keyStop = "stop";
		private const string _keyUnknown = "unknown";
		private const string _keyTransformer = "transformer";
		private const string _keyIssueFilter = "issuefilter";

		#region Constructor

		static TestTypeImageLookup()
		{
			_mapKeyToImage.Add(_keyWarning, TestTypeImages.TestTypeWarning);
			_mapKeyToImage.Add(_keyError, TestTypeImages.TestTypeError);
			_mapKeyToImage.Add(_keyStop, TestTypeImages.TestTypeStop);
			_mapKeyToImage.Add(_keyUnknown, TestTypeImages.TestTypeUnknown);
			_mapKeyToImage.Add(_keyTransformer, TestTypeImages.Transformer);
			_mapKeyToImage.Add(_keyIssueFilter, TestTypeImages.IssueFilter);

			foreach (KeyValuePair<string, Image> pair in _mapKeyToImage)
			{
				_mapImageToKey.Add(pair.Value, pair.Key);
			}

			int i = 10;
			_defaultSort.Add(_keyWarning, ++i);
			_defaultSort.Add(_keyError, ++i);
			_defaultSort.Add(_keyStop, ++i);
			_defaultSort.Add(_keyUnknown, ++i);
			_defaultSort.Add(_keyTransformer, ++i);
			_defaultSort.Add(_keyIssueFilter, ++i);
		}

		#endregion

		[NotNull]
		public static ImageList CreateImageList(bool addSortTag = false)
		{
			var result = new ImageList();

			foreach (KeyValuePair<string, Image> pair in _mapKeyToImage)
			{
				string key = pair.Key;

				Image image = pair.Value;
				if (addSortTag)
				{
					image.Tag = GetDefaultSortIndex(key);
				}

				result.Images.Add(key, image);
			}

			return result;
		}

		[NotNull]
		internal static string GetImageKey(QualityConditionType conditionType)
		{
			return GetImageKey(GetImage(conditionType));
		}

		[CanBeNull]
		[ContractAnnotation("notnull => notnull")]
		public static Image GetImage([CanBeNull] TestDescriptor testDescriptor)
		{
			return testDescriptor == null
				       ? null
				       : GetImage(testDescriptor.AllowErrors,
				                  testDescriptor.StopOnError);
		}

		[CanBeNull]
		public static Image GetImage([CanBeNull] InstanceDescriptor instanceDescriptor)
		{
			if (instanceDescriptor is TestDescriptor testDescriptor)
			{
				return GetImage(testDescriptor);
			}

			if (instanceDescriptor is TransformerDescriptor)
			{
				return GetImage(_keyTransformer);
			}

			if (instanceDescriptor is IssueFilterDescriptor)
			{
				return GetImage(_keyIssueFilter);
			}

			throw new NotImplementedException();
		}

		[CanBeNull]
		[ContractAnnotation("notnull => notnull")]
		public static Image GetImage([CanBeNull] QualityCondition qualityCondition)
		{
			return qualityCondition == null
				       ? null
				       : GetImage(qualityCondition.AllowErrors,
				                  qualityCondition.StopOnError);
		}

		[CanBeNull]
		[ContractAnnotation("notnull => notnull")]
		public static Image GetImage([CanBeNull] InstanceConfiguration instanceConfiguration)
		{
			if (instanceConfiguration is QualityCondition qualityCondition)
			{
				return GetImage(qualityCondition);
			}

			if (instanceConfiguration is TransformerConfiguration)
			{
				return GetImage(_keyTransformer);
			}

			if (instanceConfiguration is IssueFilterConfiguration)
			{
				return GetImage(_keyIssueFilter);
			}

			throw new NotImplementedException(
				$"Unsupported instance configuration: {instanceConfiguration}");
		}

		[CanBeNull]
		[ContractAnnotation("notnull => notnull")]
		public static Image GetImage([CanBeNull] QualitySpecificationElement element)
		{
			return element == null
				       ? null
				       : GetImage(element.AllowErrors, element.StopOnError);
		}

		[NotNull]
		public static Image GetImage([NotNull] string key)
		{
			return _mapKeyToImage.TryGetValue(key, out Image image)
				       ? image
				       : _mapKeyToImage[_keyUnknown];
		}

		[CanBeNull]
		[ContractAnnotation("notnull => notnull")]
		public static string GetImageKey([CanBeNull] TestDescriptor testDescriptor)
		{
			return testDescriptor == null
				       ? null
				       : GetImageKey(GetImage(testDescriptor));
		}

		[CanBeNull]
		[ContractAnnotation("notnull => notnull")]
		public static string GetImageKey([CanBeNull] InstanceDescriptor descriptor)
		{
			return descriptor == null
				       ? null
				       : GetImageKey(GetImage(descriptor));
		}

		[CanBeNull]
		[ContractAnnotation("notnull => notnull")]
		public static string GetImageKey([CanBeNull] QualityCondition qualityCondition)
		{
			return qualityCondition == null
				       ? null
				       : GetImageKey(GetImage(qualityCondition));
		}

		[CanBeNull]
		[ContractAnnotation("notnull => notnull")]
		public static string GetImageKey([CanBeNull] InstanceConfiguration configuration)
		{
			return configuration == null
				       ? null
				       : GetImageKey(GetImage(configuration));
		}

		[CanBeNull]
		[ContractAnnotation("notnull => notnull")]
		public static string GetImageKey([CanBeNull] QualitySpecificationElement element)
		{
			return element == null
				       ? null
				       : GetImageKey(GetImage(element));
		}

		public static int GetDefaultSortIndex([NotNull] TestDescriptor testDescriptor)
		{
			return GetDefaultSortIndex(GetImageKey(testDescriptor));
		}

		public static int GetDefaultSortIndex([NotNull] InstanceDescriptor instanceDescriptor)
		{
			return GetDefaultSortIndex(GetImageKey(instanceDescriptor));
		}

		public static int GetDefaultSortIndex([NotNull] QualityCondition qualityCondition)
		{
			return GetDefaultSortIndex(GetImageKey(qualityCondition));
		}

		public static int GetDefaultSortIndex([NotNull] InstanceConfiguration configuration)
		{
			return GetDefaultSortIndex(GetImageKey(configuration));
		}

		public static int GetDefaultSortIndex([CanBeNull] QualitySpecificationElement element)
		{
			return element == null
				       ? 0
				       : GetDefaultSortIndex(GetImageKey(element));
		}

		public static int GetDefaultSortIndex([NotNull] string key)
		{
			return _defaultSort[key];
		}

		#region Non-public members

		[NotNull]
		private static Image GetImage(bool allowErrors, bool stopOnError)
		{
			if (allowErrors)
			{
				return GetImage(QualityConditionType.Allowed);
			}

			return GetImage(stopOnError
				                ? QualityConditionType.StopOnError
				                : QualityConditionType.ContinueOnError);
		}

		[NotNull]
		private static Image GetImage(QualityConditionType conditionType)
		{
			if (conditionType == QualityConditionType.Allowed)
			{
				return GetImage(_keyWarning);
			}

			if (conditionType == QualityConditionType.ContinueOnError)
			{
				return GetImage(_keyError);
			}

			if (conditionType == QualityConditionType.StopOnError)
			{
				return GetImage(_keyStop);
			}

			return GetImage(_keyUnknown);
		}

		[NotNull]
		private static string GetImageKey([CanBeNull] Image image)
		{
			if (image == null)
			{
				return _keyUnknown;
			}

			return _mapImageToKey.TryGetValue(image, out string key)
				       ? key
				       : _keyUnknown;
		}

		#endregion
	}
}
