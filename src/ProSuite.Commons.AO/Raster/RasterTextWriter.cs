using System;
using System.Collections.Generic;
using System.IO;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Raster
{
	public class RasterTextWriter : IDisposable
	{
		private readonly string _fileName;
		private StreamWriter _writer;
		private bool _closed = true;

		public RasterTextWriter([NotNull] string fileName)
		{
			// create folder if does not exist
			var fileInfo = new FileInfo(fileName);
			Assert.NotNull(fileInfo.Directory).Create();

			_fileName = fileName;
			Open();
		}

		public void WriteXYZHeader()
		{
			_writer.WriteLine("X Y Z");
		}

		public void WriteAsciiGridHeader([NotNull] IRasterProps rasterProperties,
										 float noData)
		{
			try
			{
				var cellSize = rasterProperties.MeanCellSize();
				var extent = rasterProperties.Extent;

				_writer.WriteLine($"ncols         {rasterProperties.Width}");
				_writer.WriteLine($"nrows         {rasterProperties.Height}");
				_writer.WriteLine($"xllcorner     {extent.UpperLeft.X}");
				_writer.WriteLine($"yllcorner     {extent.LowerRight.Y}");
				_writer.WriteLine($"cellsize      {cellSize.X}");
				_writer.WriteLine($"NODATA_value  {noData}");
			}
			catch
			{
				throw new Exception("WriteAsciiGridHeader: Incorrect raster properties!");
			}
		}

		public void AppendData<T>([NotNull] List<T> dataLines)
		{
			if (_writer != null)
			{
				if (_closed)
					Open();

				foreach (var line in dataLines)
					_writer.WriteLine(line.ToString());
			}
		}

		public void AppendData(double value)
		{
			if (_writer != null)
			{
				if (_closed)
					Open();

				// NOTE: int.ToString() is a lot faster than double.ToString()
				int intValue = (int)value;

				// ReSharper disable once CompareOfFloatsByEqualityOperator
				// because this is performance-critical
				if (intValue == value)
				{
					_writer.Write(intValue);
				}
				else
				{
					_writer.Write(value);
				}
			}
		}

		public void AppendData(string value)
		{
			if (_writer != null)
			{
				if (_closed)
					Open();

				_writer.Write(value);
			}
		}

		public void AppendDataLine(string value)
		{
			if (_writer != null)
			{
				if (_closed)
					Open();

				_writer.WriteLine(value);
			}
		}

		public void Dispose()
		{
			if (_writer != null && !_closed)
				Close();
		}

		private void Open()
		{
			_writer = new StreamWriter(_fileName, false);
			_closed = false;
		}

		private void Close()
		{
			_writer.Flush();
			_writer.Close();
			_closed = true;
		}
	}
}
