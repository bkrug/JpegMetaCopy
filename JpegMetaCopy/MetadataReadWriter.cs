using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

//Code based from this source: http://rwlodarcmsdnblog.codeplex.com/SourceControl/latest#UsingInPlaceBitmapMetadataWriter/UsingInPlaceBitmapMetadataWriter.cs
class MetadataReadWriter : IDisposable
{
    Stream _originalFile;
    BitmapDecoder _decoder;
    string _originalPath;
    string _paddedPath => _originalPath + ".padded.jpg";
    //string _inPlacePath => _originalPath + ".InPlaceWriter.jpg";

    public readonly BitmapMetadata Metadata;
    private bool _readOnly;
    private static BitmapCreateOptions _createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;

    public MetadataReadWriter(string originalPath, bool readOnly = true)
    {
        _readOnly = readOnly;
        _originalPath = originalPath;
        _originalFile = File.Open(originalPath, FileMode.Open, FileAccess.Read);

        _decoder = BitmapDecoder.Create(_originalFile, _createOptions, BitmapCacheOption.None);

        if (!_decoder.CodecInfo.FileExtensions.Contains("jpg"))
            throw new Exception("The file you passed in is not a JPEG.");
        if (_decoder.Frames[0] == null || _decoder.Frames[0].Metadata == null)
            throw new Exception("No frames or metadata exist!!!!");

        uint paddingAmount = 128; // 2Kb padding for this example, but really this can be any value. 
                                  // Our recommendation is to keep this between 1Kb and 5Kb as most metadata updates are not large.
        Metadata = _decoder.Frames[0].Metadata.Clone() as BitmapMetadata;
        if (!_readOnly)
        {
            Metadata.SetQuery("/app1/ifd/PaddingSchema:Padding", paddingAmount);
            Metadata.SetQuery("/app1/ifd/exif/PaddingSchema:Padding", paddingAmount);
            Metadata.SetQuery("/xmp/PaddingSchema:Padding", paddingAmount);
        }
    }

    public void Dispose()
    {
        if (!_readOnly)
        {
            SaveMetadata();
            _originalFile.Dispose();
            //UsInPlaceWriter();
            File.Delete(_originalPath);
            File.Move(_paddedPath, _originalPath);

            //Compare(_originalPath, _paddedPath);
            //Compare(_originalPath, _inPlacePath);

            //FileInfo originalInfo = new FileInfo(_originalPath);
            //FileInfo outputInfo = new FileInfo(_paddedPath);
            //FileInfo finalInfo = new FileInfo(_inPlacePath);

            //Console.WriteLine("Original File Size: \t\t\t{0}", originalInfo.Length);
            //Console.WriteLine("After Padding File Size: \t\t{0}", outputInfo.Length);
            //Console.WriteLine("After InPlaceBitmapWriter File Size: \t{0}", finalInfo.Length);
        }
    }

    private void SaveMetadata()
    {
        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_decoder.Frames[0], _decoder.Frames[0].Thumbnail, Metadata, _decoder.Frames[0].ColorContexts));

        using (Stream outputFile = File.Open(_paddedPath, FileMode.Create, FileAccess.ReadWrite))
        {
            encoder.Save(outputFile);
        }
    }

    //private void UsInPlaceWriter()
    //{
    //    File.Copy(_paddedPath, _inPlacePath, true);

    //    // Now let's use the InPlaceBitmapMetadataWriter.
    //    using (Stream savedFile = File.Open(_paddedPath, FileMode.Open, FileAccess.ReadWrite))
    //    {
    //        ConsoleColor originalColor = Console.ForegroundColor;

    //        BitmapDecoder output = BitmapDecoder.Create(savedFile, BitmapCreateOptions.None, BitmapCacheOption.Default);

    //        InPlaceBitmapMetadataWriter metadata = output.Frames[0].CreateInPlaceBitmapMetadataWriter();

    //        // Within the InPlaceBitmapMetadataWriter, you can add, update, or remove metadata.
    //        //metadata.SetQuery("/app1/ifd/{uint=899}", "this is a test of the InPlaceBitmapMetadataWriter");
    //        //metadata.RemoveQuery("/app1/ifd/{uint=898}");
    //        //metadata.SetQuery("/app1/ifd/{uint=897}", "Hello there!!");

    //        if (Metadata.Title != null)
    //            metadata.Title = Metadata.Title;
    //        if (Metadata.Subject != null)
    //            metadata.Subject = Metadata.Subject;
    //        if (Metadata.Comment != null)
    //            metadata.Comment = Metadata.Comment;
    //        if (Metadata.Keywords != null)
    //            metadata.Keywords = Metadata.Keywords;

    //        if (metadata.TrySave())
    //        {
    //            Console.ForegroundColor = ConsoleColor.Green;
    //            Console.WriteLine("InPlaceMetadataWriter succeeded!");
    //        }
    //        else
    //        {
    //            Console.ForegroundColor = ConsoleColor.Red;
    //            Console.WriteLine("InPlaceMetadataWriter failed!");
    //        }

    //        Console.ForegroundColor = originalColor;
    //    }
    //}

    //private static void Compare(string originalPath, string outputPath)
    //{
    //    // For sanity, let's verify that the original and the output contain image bits that are the same.
    //    using (Stream originalFile = File.Open(originalPath, FileMode.Open, FileAccess.Read))
    //    {
    //        BitmapDecoder original = BitmapDecoder.Create(originalFile, _createOptions, BitmapCacheOption.None);

    //        using (Stream savedFile = File.Open(outputPath, FileMode.Open, FileAccess.Read))
    //        {
    //            BitmapDecoder output = BitmapDecoder.Create(savedFile, _createOptions, BitmapCacheOption.None);

    //            if (!Compare(original.Frames[0], output.Frames[0], 0, "foo", Console.Out))
    //                throw new Exception("Files don't match: " + outputPath);
    //        }
    //    }
    //}

    ///// <summary>
    ///// Compares 2 BitmapSources. Basically a poor man's image comparison routine.
    ///// </summary>
    ///// <param name="originalImage">Original BitmapSource</param>
    ///// <param name="resultImage">Result BitmapSource</param>
    ///// <param name="tolerance">Tolerance to use for determining a fail</param>
    ///// <param name="errorFilename">Filename prefix for the output files if a fail is detected</param>
    ///// <param name="log">Where to log the results to</param>
    ///// <returns>true is BitmapSources are within tolerance; false otherwise</returns>
    //private static bool Compare(BitmapSource originalImage, BitmapSource resultImage, byte tolerance, string errorFilename, TextWriter log)
    //{
    //    bool result = true;

    //    if (originalImage.PixelWidth != resultImage.PixelWidth)
    //    {
    //        log.WriteLine("\t\t\tBitmap widths are not equal.");
    //        return false;
    //    }

    //    if (originalImage.PixelHeight != resultImage.PixelHeight)
    //    {
    //        log.WriteLine("\t\t\tBitmap heights are not equal.");
    //        return false;
    //    }

    //    if (originalImage.Format != resultImage.Format)
    //    {
    //        log.WriteLine("\t\t\tBitmap pixel formats are not equal.");
    //        return false;
    //    }

    //    PixelFormat pf = PixelFormats.Bgra32;

    //    FormatConvertedBitmap src0 = new FormatConvertedBitmap(originalImage, pf, null, 0);
    //    FormatConvertedBitmap src1 = new FormatConvertedBitmap(resultImage, pf, null, 0);

    //    GC.AddMemoryPressure(((src0.Format.BitsPerPixel * src0.PixelWidth + 7) / 8) * src0.PixelHeight);
    //    GC.AddMemoryPressure(((src1.Format.BitsPerPixel * src1.PixelWidth + 7) / 8) * src1.PixelHeight);

    //    int width = src0.PixelWidth;
    //    int height = src0.PixelHeight;
    //    int bpp = pf.BitsPerPixel;
    //    int stride = (bpp * width + 7) / 8;

    //    byte[] scanline0 = new byte[stride];
    //    byte[] scanline1 = new byte[stride];
    //    Int32Rect lineRect = new Int32Rect(0, 0, width, 1);

    //    log.WriteLine("Comparison progress...");

    //    for (int y = 0; y < height; y++)
    //    {
    //        if (y % 15 == 0)
    //        {
    //            log.Write("{0}, ", y);
    //        }

    //        lineRect.Y = y;
    //        src0.CopyPixels(lineRect, scanline0, stride, 0);
    //        src1.CopyPixels(lineRect, scanline1, stride, 0);

    //        for (int b = 0; b < stride; b++)
    //        {
    //            if (Math.Abs(scanline0[b] - scanline1[b]) > tolerance)
    //            {
    //                // log.WriteLine(string.Format("\t\t\tBitmap pixels are not equal (y = {0})", y));
    //                log.WriteLine(string.Format("\t\t\tExpected {0}, Found {1}, Tolerance {2}", scanline0[b], scanline1[b], tolerance));
    //                result = false;
    //            }
    //        }
    //    }

    //    log.WriteLine();

    //    src0 = null;
    //    src1 = null;
    //    scanline0 = null;
    //    scanline1 = null;

    //    if (!result)
    //    {
    //        using (Stream stm = File.Create(errorFilename + ".original.png"))
    //        {
    //            PngBitmapEncoder png = new PngBitmapEncoder();
    //            png.Frames.Add(BitmapFrame.Create(originalImage));
    //            png.Save(stm);
    //        }
    //        using (Stream stm = File.Create(errorFilename + ".result.png"))
    //        {
    //            PngBitmapEncoder png = new PngBitmapEncoder();
    //            png.Frames.Add(BitmapFrame.Create(resultImage));
    //            png.Save(stm);
    //        }
    //    }

    //    return result;
    //}
}