using System;
using System.IO;
using System.Windows.Media.Imaging;

class MetadataReadWriter : IDisposable
{
    Stream _originalFile;
    BitmapDecoder _decoder;
    string _originalPath;
    public readonly BitmapMetadata Metadata;
    private bool _readOnly;

    public MetadataReadWriter(string originalPath, bool readOnly = true)
    {
        _readOnly = readOnly;
        _originalPath = originalPath;
        _originalFile = File.Open(originalPath, FileMode.Open, FileAccess.Read);

        BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
        _decoder = BitmapDecoder.Create(_originalFile, createOptions, BitmapCacheOption.None);

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
            var outputPath = _originalPath + ".temp.jpg";
            SaveMetadata(outputPath);
            _originalFile.Dispose();
            //ThisLooksUnnecessary();
            File.Delete(_originalPath);
            File.Move(outputPath, _originalPath);
        }
    }

    private void SaveMetadata(string outputPath)
    {
        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_decoder.Frames[0], _decoder.Frames[0].Thumbnail, Metadata, _decoder.Frames[0].ColorContexts));

        using (Stream outputFile = File.Open(outputPath, FileMode.Create, FileAccess.ReadWrite))
        {
            encoder.Save(outputFile);
        }
    }

    private void ThisLooksUnnecessary()
    {
        var outputPath = _originalPath + ".temp.jpg";

        // Now let's use the InPlaceBitmapMetadataWriter.
        using (Stream savedFile = File.Open(outputPath, FileMode.Open, FileAccess.ReadWrite))
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            BitmapDecoder output = BitmapDecoder.Create(savedFile, BitmapCreateOptions.None, BitmapCacheOption.Default);

            InPlaceBitmapMetadataWriter metadata = output.Frames[0].CreateInPlaceBitmapMetadataWriter();

            // Within the InPlaceBitmapMetadataWriter, you can add, update, or remove metadata.
            metadata.SetQuery("/app1/ifd/{uint=899}", "this is a test of the InPlaceBitmapMetadataWriter");
            metadata.RemoveQuery("/app1/ifd/{uint=898}");
            metadata.SetQuery("/app1/ifd/{uint=897}", "Hello there!!");

            if (metadata.TrySave())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("InPlaceMetadataWriter succeeded!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("InPlaceMetadataWriter failed!");
            }

            Console.ForegroundColor = originalColor;
        }
    }
}