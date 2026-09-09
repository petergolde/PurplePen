// CommandLineOptions.cs
//
// Parsing of the command line that AvPurplePen is started with. Until crash recovery was
// added, the Avalonia application ignored its arguments entirely; the crash handler's
// Restart button needs to be able to hand the reopened file (and the recovery snapshot)
// to a fresh instance, so the arguments now have to be understood.

using System;
using System.Collections.Generic;

namespace AvPurplePen
{
    /// <summary>
    /// The options that Purple Pen was started with.
    /// </summary>
    internal sealed class CommandLineOptions
    {
        /// <summary>
        /// The event file to open, or null to show the welcome screen.
        /// </summary>
        public string? FileName { get; init; }

        /// <summary>
        /// A crash-recovery snapshot whose contents should be loaded in place of
        /// <see cref="FileName"/>'s contents, or null for a normal open. When set, the event
        /// data comes from the snapshot but the document is presented as
        /// <see cref="FileName"/> and starts out with unsaved changes.
        /// </summary>
        public string? RecoveryFileName { get; init; }

        /// <summary>
        /// Parses the command line. The supported forms are:
        ///
        ///     AvPurplePen                                     show the welcome screen
        ///     AvPurplePen {file.ppen}                         open that event directly
        ///     AvPurplePen {file.ppen} -recovery {snapshot}    open the recovered contents,
        ///                                                     presented as {file.ppen}
        ///
        /// The recovery switch is matched case insensitively, and is the only switch there is.
        /// Other arguments beginning with a dash are ignored rather than rejected, so that a
        /// switch added in a future version can never turn an older build into a startup
        /// failure.
        ///
        /// A leading slash does NOT introduce a switch, on any platform. That convention is a
        /// Windows one, and honouring it costs far more than it is worth: on Linux and macOS
        /// every absolute path begins with a slash, so a path passed by a file manager -- the
        /// desktop entry runs "purplepen %f" -- would be swallowed as an unrecognized switch,
        /// and Purple Pen would start with no file and say nothing about why. On Windows a
        /// slash is a legal directory separator anyway, so a path like "/events/spring.ppen"
        /// is better opened than discarded.
        /// </summary>
        /// <param name="args">The raw command-line arguments, excluding the program name.</param>
        /// <returns>The parsed options; never null.</returns>
        public static CommandLineOptions Parse(IReadOnlyList<string>? args)
        {
            string? fileName = null;
            string? recoveryFileName = null;

            if (args != null) {
                for (int i = 0; i < args.Count; ++i) {
                    string arg = args[i];
                    if (string.IsNullOrEmpty(arg))
                        continue;

                    if (IsRecoverySwitch(arg)) {
                        // The snapshot path is the next argument. If it is missing, ignore the
                        // switch rather than failing to start.
                        if (i + 1 < args.Count) {
                            recoveryFileName = args[i + 1];
                            ++i;
                        }
                    }
                    else if (arg[0] == '-') {
                        // Some other switch. Ignore it (see the note above about forward
                        // compatibility). This does mean that a file whose name begins with a
                        // dash has to be passed as "./-name.ppen", which is how every other
                        // command-line program behaves.
                    }
                    else if (fileName == null) {
                        // The first non-switch argument is the file to open. Later ones are
                        // ignored; Purple Pen only ever edits one event at a time.
                        fileName = arg;
                    }
                }
            }

            // Defensive: if a snapshot was given with no file to present it as, fall back to
            // opening the snapshot as itself. Better than starting with nothing.
            if (fileName == null && recoveryFileName != null) {
                fileName = recoveryFileName;
                recoveryFileName = null;
            }

            return new CommandLineOptions {
                FileName = fileName,
                RecoveryFileName = recoveryFileName
            };
        }

        /// <summary>
        /// Determines whether an argument is the recovery switch.
        ///
        /// One spelling, because there is one producer: <see cref="RecoveryManager"/> writes
        /// <see cref="RecoveryManager.RecoverySwitch"/> when it restarts Purple Pen after a
        /// crash, and nothing else -- no installer, no shortcut, no documented option -- ever
        /// passes a switch. The comparison ignores case only because that costs nothing.
        /// </summary>
        /// <param name="arg">The argument to test.</param>
        /// <returns>True if the argument introduces a recovery snapshot path.</returns>
        private static bool IsRecoverySwitch(string arg)
        {
            return string.Equals(arg, RecoveryManager.RecoverySwitch, StringComparison.OrdinalIgnoreCase);
        }
    }
}
