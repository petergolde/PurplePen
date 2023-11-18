# Ideas for Purple Pen Web Application

This is some sketched out ideas for Purple Pen web application.

## Framework

Use an SPA Framework, probably Quasar, along with a ASP.NET Core back-end. The
ASP.Net core back-end calls into a core version of Purple Pen that is independent
of Windows Forms.

Authentication through standard ASP.NET Core cookie authentication.

Server side should be split into web front end and "logic" that isn't
front-end specific. Database usage probably split into repository class
also.

Also look at: https://xel-toolkit.org/ for UI components.

## Core version of Purple Pen

Separate out a core version of Purple Pen, by slowly moving files into 
a new assembly. That assembly cannot reference the UI assembly, or 
Windows Forms or System.Drawing (except for core stuff like Rect or PointF
or Color).

We may need to port System.Drawing.Drawing2D.Matrix from old sources.
Maybe look at https://source.winehq.org/source/dlls/gdiplus/matrix.c
Or use SkMatrix from Skia. Or port code from Skia. PdfSharp/PdfSharpCore also
has a version we may be able to use. We also have an embedded matrix
class we can use. Also look at System.Numerics.Matrix3x2.

Idea is to keep the Windows Forms version continuing to work and be enhanced,
while also building the web version. Probably means changing the Windows
Forms version to use SkiaSharp.

### SkiaSharp

Use SkiaSharp for drawing; we already have this in MapModel but need
to bring it forward to newer versions of SkiaSharp.

Does SkiaSharp have anything that can do the color dimming we
use for the background map, or the Darken/merging we do for overlaying?
I think so, there is a Darken blend mode we can use.

### Fonts

Look at "CrossCore fonts" -- see https://en.wikipedia.org/wiki/Croscore_fonts.
These are open source fonts that are metrically compatible with Arial, Times New Roman, and Courier New,
and Calibri and Cambria. So we can use these fonts for those. Other fonts people would
have to upload as TTF files, and they are responsbile for licensing issues.

Also look at this page for other fonts we could use (e.g., Gelasio, etc.): 
https://wiki.archlinux.org/title/Metric-compatible_fonts#List%20of%20Metric-Compatible%20Fonts


## Thread safety/multi-document

The core part of Purple Pen needs to handle multiple documents and 
be thread safe. We should be mostly there, since we have eventDB parameter
in most places and so forth.

Not sure whether the DotSpatial stuff is thread safe; we might want to 
put a lock around it just to be sure. Similar with the PDF creation.
If we use an executable for PDF reading like before, then there should be
no issue there.

Is there a good way to test for multi-threading safety? I'm not sure. Should
maybe generally search the source code for static variables. This makes me
nervous.

Could use a global lock for all of Purple Pen, but that would be bad for performance.
That sounds ugly.

## PDF creation.

There is a version of PdfSharp that works on .NET Core. 

https://github.com/ststeiger/PdfSharpCore

One thing I think it is missing is TextOutline. We should be able to make
that work using SkiaSharp, in particular SkPaint.GetTextPath, then enumerating
the resulting path. Hopefully there are no conics, but I don't think
that there should be.

Here's pages that describes converting a quadratic Bezier to a cubic:

https://fontforge.org/docs/techref/bezier.html

https://stackoverflow.com/questions/3162645/convert-a-quadratic-bezier-to-a-cubic-one

One issue is the global FontResolver in PdfSharpCore. This may need to be changed
to be per document, or have a way to clear the font resolver and the GlyphTypefaceCache if we single-thread 
all of PDF generation. Or maybe there is a FontLoader (created from an IFontResolver) that
can be used to load and cache fonts.

Other parts of PdfSharpCore might not be multi-threading sufficient, though it looks pretty good.

Also would be nice to add support for SkiaSharp.

## PDF reading

Needing PdiumSharp. There are a bunch of versions on GitHub. Need to look
them over to see which is best. Probably still package as an EXE.

Use similar caching strategy as before.

For eventual use by Mac version, could consider a Web service that converts
as well.

## Printing

We won't have direct printing, just use the PDF creating code. Can have
some little bit of HTML to open a new tab with the PDF and activate
the print dialog.

## Color conversion CMYK->RGB

I did some work on emulating the color conversion stuff. It is in 
PurplePen/src/Tools/LinearColorConverter

### OCAD Compatible mode

Eliminate OCAD Compatible mode for printing, because there is no way to test for it.

## Crashes/Exception handling

What to do about crashes/exception handling?

## Recording usage

Should record some telemetry about usage. Don't need much. Possibly use a separate database
for that.

## Database usage

Probably use MySQL or Postgres + blob storage. Like ClueCorner did.

## File handling and sharing

We want to store Purple Pen documents in the cloud, and allow sharing them. When sharing, we shouldn't
be required to log in just to view or print.

We should allow viewing a shared document when not logged in. But if you save, you should be 
required to log in. Also maybe a comment. Or maybe, if you edit a shared version,
then it becomes your own version?

We should allow creating a new document when not logged in, then later log in to save or share it. 

Sharing should just create a link. Provide way to revoke the link. Also provide a way to create 
a read-only link.

Documents should disappear after 3 month of no activity. Also have a  
max number of Purple Pen files per account (25?). (Could evenutally have a pay option to increase
that).

Need a way to prevent same document from being edited by multiple people -- some kind of tracking
with keep alive.

### Versioning

Maybe save multiple versions per file and allow going back? This can be a subsequent enhancement, but
should not make architecture hostile to it. Have option to "create new version", but always create new
version if a different user is editing. Be good if different versions with same map file reuse
the map file storage; then we don't use too much storage per version.

Maybe have auto-save, but can explicitly save to create a new version (checkpoint). Versions should
maybe have comments.

## Handling custom fonts 

Need User Interface for user to upload custom fonts that are used by the map. Probably should
not export those fonts again. Would probably be good for fonts to be shared among maps for that user.

## Logging in/authentication

Strongly consider only using Google accounts for logging in. This way we don't deal with password
forgetting, confirmation, two-factor, etc. Logging in is just for handling access to documents.
OK if we don't have any info about accounts except email.