using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace JpegMetaCopy
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
                return OutputUseage();
            try
            {
                GetFileParts(args[0], out var sourceFilePath, out var sourceDescription);
                GetFileParts(args[1], out var destinationFilePath, out var destinationDescription);
                var sourceFiles = Directory.GetFiles(sourceFilePath, "*" + sourceDescription);
                var escapedSourceFilePath = Regex.Escape(sourceFilePath);
                var escapedSourceDescription = Regex.Escape(sourceDescription);
                bool noFilesFound = true;
                foreach (var pathToSource in sourceFiles)
                {
                    var fileName = Regex.Replace(pathToSource, escapedSourceFilePath, "", RegexOptions.IgnoreCase);
                    fileName = Regex.Replace(fileName, escapedSourceDescription, "", RegexOptions.IgnoreCase);
                    var pathToDestin = destinationFilePath + fileName + destinationDescription;
                    if (!File.Exists(pathToDestin))
                        continue;
                    noFilesFound = false;
                    using (var sourceFile = new MetadataReadWriter(pathToSource, true))
                    using (var destinFile = new MetadataReadWriter(pathToDestin, false))
                    {
                        var sourceMeta = sourceFile.Metadata;
                        var destinMeta = destinFile.Metadata;
                        Console.WriteLine(Path.GetFileName(fileName));
                        ReplaceTitle(sourceMeta, destinMeta);
                        ReplaceSubject(sourceMeta, destinMeta);
                        ReplaceComment(sourceMeta, destinMeta);
                        MergeKeywords(sourceMeta, destinMeta);
                    }
                }
                if (noFilesFound)
                    return PrintNoFileError();
                return 0;
            }
            catch (UserException ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }
        }

        private static int PrintNoFileError()
        {
            Console.WriteLine("No files in the destination path matched files in the source path. " +
                "Remeber that the program will see files as matching (having the same name) " +
                "if the portion of the file name represented by the (*) is the same.");
            return -1;
        }

        private static int OutputUseage()
        {
            Console.WriteLine("USEAGE: JpegMetaCopy [source path with wildcard] [destination path with wildcard]");
            Console.WriteLine();
            Console.WriteLine(@"Ex: JpegMetaCopy c:\pictures\2D\*.jpg c:\pictures\3D\*.card.jpg");
            Console.WriteLine($@"
For each picture in the source path with an equivalently named file in the
destination, the program will copy metadata to the destination file.
Specifically, the source's title, subject, and comment will be copied to the
destination file if the source file has values in those field. The source's
keywords/tags will be merged into the destination file's tags. The exception
to this is that the following tags will not be copied from the source file:
'{string.Join("','", doNotCopy)}'

The above example would therefore copy tags
from c:\pictures\2D\P123456.jpg 
to   c:\pictures\3D\P123456.card.jpg
because the portions of the filenames represented by the wildcard are 
identical.");
            return -1;
        }

        private static void GetFileParts(string source, out string sourceFilePath, out string sourceDescription)
        {
            var s = source.Split(new char[] { '*' });
            if (s.Length != 2)
                throw new UserException("Source and destination path must include the (*) wildcard.");
            sourceFilePath = s[0];
            sourceDescription = s[1];
            if (string.IsNullOrWhiteSpace(sourceFilePath))
                sourceFilePath = ".";
            if (string.IsNullOrWhiteSpace(sourceDescription))
                sourceDescription = "";
            sourceFilePath = sourceFilePath.TrimEnd(new char[] { '\\' }) + "\\";
        }

        private static void ReplaceTitle(BitmapMetadata sourceMeta, BitmapMetadata destinMeta)
        {
            if (!string.IsNullOrWhiteSpace(sourceMeta.Title))
                destinMeta.Title = sourceMeta.Title;
        }

        private static void ReplaceSubject(BitmapMetadata sourceMeta, BitmapMetadata destinMeta)
        {
            if (!string.IsNullOrWhiteSpace(sourceMeta.Subject))
                destinMeta.Subject = sourceMeta.Subject;
        }

        private static void ReplaceComment(BitmapMetadata sourceMeta, BitmapMetadata destinMeta)
        {
            if (!string.IsNullOrWhiteSpace(sourceMeta.Comment))
                destinMeta.Comment = sourceMeta.Comment;
        }

        private static List<string> doNotCopy = new List<string>() { "analygraph", "3D", "stereoscopic" };
        private static void MergeKeywords(BitmapMetadata sourceMeta, BitmapMetadata destinMeta)
        {
            var sourceWords = sourceMeta.Keywords?.Where(k => !doNotCopy.Contains(k, StringComparer.OrdinalIgnoreCase)) ?? new List<string>();
            var destinWords = destinMeta.Keywords?.ToList() ?? new List<string>();
            destinMeta.Keywords = (new ReadOnlyCollectionBuilder<string>(sourceWords.Concat(destinWords))).ToReadOnlyCollection();
        }
    }

    public class UserException : Exception
    {
        public UserException(string message) : base(message) { }
    }
}
