/* GetVersion.cs
 *
 * A projectless (file-based) C# program that reads the file version out of a
 * compiled Purple Pen DLL and writes a shell script to stdout that sets the
 * version-related environment variables used by the installer builds.
 *
 * Usage:
 *     dotnet run --file GetVersion.cs -- batch <path-to-dll>   > setversion.cmd
 *     dotnet run --file GetVersion.cs -- bash  <path-to-dll>   > setversion.sh
 *
 * The --file switch is what tells the SDK to run this as a file-based app; it
 * matters when the current directory contains a project file, which the SDK
 * would otherwise try to run instead.
 *
 * The generated script is meant to be executed in the context of the calling
 * shell, so that the variables land in that shell's environment:
 *
 *     call setversion.cmd        (CMD.EXE)
 *     . ./setversion.sh          (bash)
 *
 * Variables set:
 *     VERSION_MAJOR       major component, e.g. 4
 *     VERSION_MINOR       minor component, e.g. 0
 *     VERSION_BUILD       build component, e.g. 0
 *     VERSION_REV         revision component, e.g. 210
 *     VERSION_STRING      all four joined with dots, e.g. 4.0.0.210
 *     VERSION_PRERELEASE  1 for alpha/beta/rc builds, 0 for a final release
 *     SETUP_BASENAME      base file name for the setup file, e.g.
 *                         purplepen-400-beta1
 *     PROGRAM_TITLE       the version as shown to a person, e.g.
 *                         "Purple Pen 4.0.0 Beta 1"
 */

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

// Generates shell scripts describing the version number of a compiled binary.
public static class GetVersion
{
    // The last component of the version number encodes the alpha/beta/RC/stable
    // stage rather than a build number; the tens digit is the sequence number
    // within that stage. So 110 is Alpha 1, 220 is Beta 2, 500 is a stable
    // release. These constants match PurplePenCore/VersionNumber.cs, which is
    // the authority on this encoding.
    private const int Alpha = 100;
    private const int Beta = 200;
    private const int RC = 300;
    private const int Stable = 500;

    // The product name that PROGRAM_TITLE is built from.
    private const string ProgramName = "Purple Pen";

    // Entry point. args[0] is "batch" or "bash", args[1] is the path to a
    // compiled DLL. Returns 0 on success, 1 on any error (with the reason
    // written to stderr, so it does not pollute the generated script).
    public static int Main(string[] args)
    {
        if (args.Length != 2) {
            Console.Error.WriteLine("Usage: GetVersion.cs (batch|bash) <path-to-dll>");
            return 1;
        }

        string format = args[0].ToLowerInvariant();
        if (format != "batch" && format != "bash") {
            Console.Error.WriteLine("GetVersion: first argument must be \"batch\" or \"bash\", not \"{0}\".", args[0]);
            return 1;
        }

        string dllPath = args[1];
        if (!File.Exists(dllPath)) {
            Console.Error.WriteLine("GetVersion: file \"{0}\" does not exist.", dllPath);
            return 1;
        }

        FileVersionInfo versionInfo;
        try {
            versionInfo = FileVersionInfo.GetVersionInfo(Path.GetFullPath(dllPath));
        }
        catch (Exception e) {
            Console.Error.WriteLine("GetVersion: could not read version information from \"{0}\": {1}", dllPath, e.Message);
            return 1;
        }

        int major = versionInfo.FileMajorPart;
        int minor = versionInfo.FileMinorPart;
        int build = versionInfo.FileBuildPart;
        int revision = versionInfo.FilePrivatePart;

        if (major == 0 && minor == 0 && build == 0 && revision == 0) {
            Console.Error.WriteLine("GetVersion: \"{0}\" has no file version.", dllPath);
            return 1;
        }

        ReleaseStage stage = DecodeStage(revision);

        string versionString = string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", major, minor, build, revision);
        bool prerelease = (revision < Stable);
        string setupBasename = string.Format(CultureInfo.InvariantCulture, "purplepen-{0}{1}{2}{3}",
                                             major, minor, build, SetupSuffix(stage));
        string programTitle = string.Format(CultureInfo.InvariantCulture, "{0} {1}.{2}.{3}{4}",
                                            ProgramName, major, minor, build, TitleSuffix(stage));

        StringBuilder output = new StringBuilder();
        WriteVariable(output, format, "VERSION_MAJOR", major.ToString(CultureInfo.InvariantCulture));
        WriteVariable(output, format, "VERSION_MINOR", minor.ToString(CultureInfo.InvariantCulture));
        WriteVariable(output, format, "VERSION_BUILD", build.ToString(CultureInfo.InvariantCulture));
        WriteVariable(output, format, "VERSION_REV", revision.ToString(CultureInfo.InvariantCulture));
        WriteVariable(output, format, "VERSION_STRING", versionString);
        WriteVariable(output, format, "VERSION_PRERELEASE", prerelease ? "1" : "0");
        WriteVariable(output, format, "SETUP_BASENAME", setupBasename);
        WriteVariable(output, format, "PROGRAM_TITLE", programTitle);

        Console.Out.Write(output.ToString());
        return 0;
    }

    // The release stage encoded in the revision component of a version number.
    // Both SETUP_BASENAME and PROGRAM_TITLE are built from this, so that the
    // file name and the displayed title can never disagree about what a build is.
    private struct ReleaseStage
    {
        // Stage name in title case ("Alpha", "Beta", "RC", "Dev"), or null for a
        // stable release, which has no stage name at all.
        public string Name;

        // Sequence number within the stage. Zero means the stage carries no
        // number, so it reads "Beta" rather than "Beta 0".
        public int Sequence;
    }

    // Decodes the release stage out of the revision component of the version
    // number. A revision below Alpha is not a value this scheme defines; it is
    // reported as a "Dev" build rather than being silently treated as stable.
    private static ReleaseStage DecodeStage(int revision)
    {
        ReleaseStage stage = new ReleaseStage();

        if (revision >= Stable)
            stage.Name = null;
        else if (revision >= RC)
            stage.Name = "RC";
        else if (revision >= Beta)
            stage.Name = "Beta";
        else if (revision >= Alpha)
            stage.Name = "Alpha";
        else
            stage.Name = "Dev";

        stage.Sequence = (revision % 100) / 10;
        return stage;
    }

    // Returns the file-name suffix for a release stage: "" for a stable release,
    // otherwise a lower-case, hyphen-separated form such as "-beta1" or "-rc2".
    private static string SetupSuffix(ReleaseStage stage)
    {
        if (stage.Name == null)
            return "";

        return string.Format(CultureInfo.InvariantCulture, "-{0}{1}",
                             stage.Name.ToLowerInvariant(), SequenceText(stage));
    }

    // Returns the displayed suffix for a release stage: "" for a stable release,
    // otherwise a leading space and the stage in title case, such as " Beta 1".
    // This matches the format Util.PrettyVersionString produces inside the
    // application, so the installer and the About box agree.
    private static string TitleSuffix(ReleaseStage stage)
    {
        if (stage.Name == null)
            return "";

        string sequenceText = SequenceText(stage);
        if (sequenceText != "")
            sequenceText = " " + sequenceText;

        return string.Format(CultureInfo.InvariantCulture, " {0}{1}", stage.Name, sequenceText);
    }

    // Returns the stage's sequence number as text, or "" when there is no number.
    private static string SequenceText(ReleaseStage stage)
    {
        return (stage.Sequence == 0) ? "" : stage.Sequence.ToString(CultureInfo.InvariantCulture);
    }

    // Appends one variable assignment to the output in the requested shell's
    // syntax. Batch lines are prefixed with "@" so that they do not echo when
    // the script is CALLed, without changing the caller's echo setting.
    private static void WriteVariable(StringBuilder output, string format, string name, string value)
    {
        if (format == "batch")
            output.AppendFormat(CultureInfo.InvariantCulture, "@set \"{0}={1}\"\r\n", name, value);
        else
            output.AppendFormat(CultureInfo.InvariantCulture, "export {0}='{1}'\n", name, QuoteForBash(value));
    }

    // Escapes a value for inclusion inside a single-quoted bash string. A single
    // quote has to end the string, produce an escaped quote, and start a new one.
    private static string QuoteForBash(string value)
    {
        return value.Replace("'", "'\''");
    }
}
