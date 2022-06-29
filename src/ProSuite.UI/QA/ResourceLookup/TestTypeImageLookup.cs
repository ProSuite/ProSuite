using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Properties;

namespace ProSuite.UI.QA.ResourceLookup
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
		private const string _keyProhibition = "prohibition";
		private const string _keyStop = "stop";
		private const string _keyUnknown = "unknown";
		private const string _keyTransform = "transform";

		#region Constructor

		/// <summary>
		/// Initializes the <see cref="TestTypeImageLookup"/> class.
		/// </summary>
		static TestTypeImageLookup()
		{
			_mapKeyToImage.Add(_keyWarning, TestTypeImages.TestTypeWarning);
			_mapKeyToImage.Add(_keyProhibition, TestTypeImages.TestTypeProhibition);
			_mapKeyToImage.Add(_keyStop, TestTypeImages.TestTypeStop);
			_mapKeyToImage.Add(_keyUnknown, TestTypeImages.TestTypeUnknown);
			_mapKeyToImage.Add(_keyTransform, TestTypeImages.Transform);

			foreach (KeyValuePair<string, Image> pair in _mapKeyToImage)
			{
				_mapImageToKey.Add(pair.Value, pair.Key);
			}

			int i = 0;
			_defaultSort.Add(_keyWarning, ++i);
			_defaultSort.Add(_keyProhibition, ++i);
			_defaultSort.Add(_keyStop, ++i);
			_defaultSort.Add(_keyUnknown, ++i);
			_defaultSort.Add(_keyTransform, ++i);
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
			if (instanceDescriptor is TransformerDescriptor transformerDescriptor)
			{
				return GetImage(transformerDescriptor);
			}

			throw new NotImplementedException();
		}

		[CanBeNull]
		public static Image GetImage([CanBeNull] TransformerDescriptor transformerDescriptor)
		{
			return GetImage(_keyTransform);
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
		public static Image GetImage([CanBeNull] QualitySpecificationElement element)
		{
			return element == null
				       ? null
				       : GetImage(element.AllowErrors, element.StopOnError);
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
		public static string GetImageKey([CanBeNull] TransformerDescriptor transformerDescriptor)
		{
			return transformerDescriptor == null
				       ? null
				       : GetImageKey(GetImage(transformerDescriptor));
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
		public static string GetImageKey([CanBeNull] QualitySpecificationElement element)
		{
			return element == null
				       ? null
				       : GetImageKey(GetImage(element));
		}

		[NotNull]
		public static Image GetImage([NotNull] string key)
		{
			Image image;
			return _mapKeyToImage.TryGetValue(key, out image)
				       ? image
				       : _mapKeyToImage[_keyUnknown];
		}

		public static int GetDefaultSortIndex([NotNull] TestDescriptor testDescriptor)
		{
			return GetDefaultSortIndex(GetImageKey(testDescriptor));
		}

		public static int GetDefaultSortIndex([NotNull] InstanceDescriptor instanceDescriptor)
		{
			if (instanceDescriptor is TransformerDescriptor transformerDescriptor)
			{
				return GetDefaultSortIndex(transformerDescriptor);
			}

			throw new NotImplementedException();
		}

		public static int GetDefaultSortIndex([NotNull] TransformerDescriptor transformerDescriptor)
		{
			return GetDefaultSortIndex(GetImageKey(transformerDescriptor));
		}

		public static int GetDefaultSortIndex([NotNull] QualityCondition qualityCondition)
		{
			return GetDefaultSortIndex(GetImageKey(qualityCondition));
		}

		public static int GetDefaultSortIndex(
			[CanBeNull] QualitySpecificationElement element)
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
				return GetImage(_keyProhibition);
			}

			return GetImage(conditionType == QualityConditionType.StopOnError
				                ? _keyStop
				                : _keyUnknown);
		}

		[NotNull]
		private static string GetImageKey([CanBeNull] Image image)
		{
			if (image == null)
			{
				return _keyUnknown;
			}

			string key;
			return _mapImageToKey.TryGetValue(image, out key)
				       ? key
				       : _keyUnknown;
		}

		#endregion
	}
}
