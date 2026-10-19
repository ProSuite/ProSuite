using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	public class AreaPredictionJobResult
	{
		public AreaPredictionJobResult([NotNull] string imagePath,
		                               [CanBeNull] string jobId,
		                               [NotNull] IList<AreaPrediction> predictions,
		                               [CanBeNull] string diagnostics,
		                               [CanBeNull] string error = null)
		{
			Assert.ArgumentNotNullOrEmpty(imagePath, nameof(imagePath));
			Assert.ArgumentNotNull(predictions, nameof(predictions));

			ImagePath = imagePath;
			JobId = jobId;
			Predictions = predictions;
			Diagnostics = diagnostics;
			Error = error;
		}

		[NotNull] public string ImagePath { get; }
		[CanBeNull] public string JobId { get; }
		[NotNull] public IList<AreaPrediction> Predictions { get; }
		[CanBeNull] public string Diagnostics { get; }
		[CanBeNull] public string Error { get; }
		public bool Succeeded => string.IsNullOrEmpty(Error);
	}
}
