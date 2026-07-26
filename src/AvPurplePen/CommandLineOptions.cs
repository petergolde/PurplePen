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
        /// The recovery switch is also accepted as --recovery and /recovery, case
        /// insensitively. Unrecognized switches are ignored rather than rejected, so that a
        /// switch added in a future version can never turn an older build into a startup
        /// failure.
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
                    else if (arg[0] == '-' || arg[0] == '/') {
                        // Some other switch. Ignore it (see the note above about forward
                        // compatibility). Note that this means a file whose name begins with a
                        // dash has to be passed as ".\-name.ppen"; that matches how the legacy
                        // WinForms application behaved.
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
        /// Determines whether an argument is the recovery switch, in any of its accepted spellings.
        /// </summary>
        /// <param name="arg">The argument to test.</param>
        /// <returns>True if the argument introduces a recovery snapshot path.</returns>
        private static bool IsRecoverySwitch(string arg)
        {
            return string.Equals(arg, RecoveryManager.RecoverySwitch, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(arg, "--recovery", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(arg, "/recovery", StringComparison.OrdinalIgnoreCase);
        }
    }
}
