# JpegMetaCopy
The program in this project is designed to copy meta data from one set of jpeg files to another, and to do it without having to re-encode the image, that is the file change is lose-less.

For each picture in the source path with an equivalently named file in the
destination, the program will copy metadata to the destination file.
Specifically, the source's title, subject, and comment will be copied to the
destination file if the source file has values in those field. The source's
keywords/tags will be merged into the destination file's tags. The exception
to this is that the following tags will not be copied from the source file:
"analygraph", "3D", "stereoscopic".  

The reason for these exceptions is that I wrote this program in order to 
help me with managing my 3D pictures. I generatlly have a set of 2D pictures
in one folder and a set of 3D images in another. If I make changes to the
tags in one folder, I'd rather not have to go to a lot of work to change the
tags in the other folder.

Usage Example:
```
JpegMetaCopy c:\pictures\2D\*.jpg c:\pictures\3D\*.card.jpg
```

The above example would therefore copy tags
from c:\pictures\2D\P123456.jpg 
to   c:\pictures\3D\P123456.card.jpg
because the portions of the filenames represented by the wildcard are 
identical.
