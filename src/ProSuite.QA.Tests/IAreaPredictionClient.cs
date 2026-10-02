using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public interface IAreaPredictionClient
	{
		[NotNull]
		IList<AreaPrediction> GetPredictions([NotNull] string imagePath,
		                                      [NotNull] string apiUrl,
		                                      [NotNull] string modelName,
		                                      TimeSpan timeout,
		                                      double? tileOverlapRatio = null);
	}

	public interface IAreaPredictionBatchClient
	{
		[NotNull]
		IList<AreaPredictionJobResult> GetPredictionJobs(
			[NotNull] IList<string> imagePaths,
			[NotNull] string apiUrl,
			[NotNull] string modelName,
			TimeSpan timeout,
			double? tileOverlapRatio = null);
	}

	public interface IAreaPredictionStreamingBatchClient : IAreaPredictionBatchClient
	{
		[NotNull]
		IList<AreaPredictionJobResult> GetPredictionJobs(
			[NotNull] IList<string> imagePaths,
			[NotNull] string apiUrl,
			[NotNull] string modelName,
			TimeSpan timeout,
			[CanBeNull] Action<string, string> jobSubmitted,
			[CanBeNull] Action<AreaPredictionJobResult> jobCompleted,
			double? tileOverlapRatio = null);
	}
}
