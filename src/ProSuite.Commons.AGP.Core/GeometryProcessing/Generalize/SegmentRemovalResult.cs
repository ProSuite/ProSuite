using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.Generalize;

/// <summary>
/// The result of the segment removal, i.e. the features to be updated together with
/// the messages regarding the features (or parts thereof) that could not be changed.
/// </summary>
public class SegmentRemovalResult
{
	[NotNull]
	public IList<ResultFeature> ResultFeatures { get; set; } = new List<ResultFeature>();

	/// <summary>
	/// The messages from the server that explain why a feature or a part of a feature
	/// was not changed.
	/// </summary>
	[NotNull]
	public IList<string> NonStorableMessages { get; } = new List<string>(0);
}
