﻿using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;

namespace CrashReporterDotNET
{
    /// <summary>
    /// Set SMTP server details and receiver email fields of this class instance to send crash reports directly in your inbox.
    /// </summary>
    public class ReportCrash
    {
        /// <summary>
        /// Gets or Sets name or IP address of the Host used for SMTP transactions.
        /// </summary>
        public String SmtpHost;

        /// <summary>
        /// Specify whether the SMTP client uses the Secure Socket Layer (SSL) to encrypt the connection.
        /// </summary>
        public Boolean EnableSSL;

        /// <summary>
        /// Gets or Sets the port used for SMTP transactions.
        /// </summary>
        public int Port = 25;

        /// <summary>
        /// Gets or Sets the username used for SMTP transactions.
        /// </summary>
        public String UserName = "";

        /// <summary>
        /// Gets or Sets the password used for SMTP transactions. 
        /// </summary>
        public String Password = "";

        /// <summary>
        /// Gets or Sets email address where you want to receive crash reports.
        /// </summary>
        public String ToEmail;

        /// <summary>
        /// Gets or Sets email address used by crash reporter if user don't provide her email address.
        /// </summary>
        public String FromEmail;

        /// <summary>
        /// Gets or Sets exception that occur during application execution.
        /// </summary>
        public Exception Exception;

        /// <summary>
        /// Text for a few key parts of the UI (for localization).
        /// </summary>
        public string TextIntro = "An error has occurred. In order to help us fix the problem, please enter your email address and additional information, and send us an error report.", 
            TextEmail = "Contact Email", 
            TextMessage = "Please tell us what you were doing in the program, to help fix the problem", 
            TextSend = "Send Report", 
            TextSave = "Save", 
            TextCancel = "Cancel";

        internal string ApplicationTitle;

        internal string ApplicationVersion;

        internal string ScreenShot;

        /// <summary>
        /// Sends exception report directly to receiver email address provided in ToEmail.
        /// </summary>
        public void Send()
        {
            if (Exception != null)
            {
                Send(Exception);
            }
        }

        /// <summary>
        /// Sends exception report directly to receiver email address provided in ToEmail.
        /// </summary>
        /// <param name="exception">Exception object that contains details of the exception.</param>
        public void Send(Exception exception)
        {
            Exception = exception;

            var mainAssembly = Assembly.GetEntryAssembly();
            string appTitle = null;
            var attributes = mainAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), true);
            if (attributes.Length > 0)
            {
                appTitle = ((AssemblyTitleAttribute) attributes[0]).Title;
            }
            ApplicationTitle = !string.IsNullOrEmpty(appTitle) ? appTitle : mainAssembly.GetName().Name;
            ApplicationVersion = mainAssembly.GetName().Version.ToString();

            try
            {
                var captureScreenshot = new CaptureScreenshot();
                ScreenShot = string.Format(@"{0}\{1} Crash Screenshot.png", Path.GetTempPath(),
                                           ApplicationTitle);
                captureScreenshot.CaptureScreenToFile(ScreenShot, ImageFormat.Png);
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
            }
            if (String.IsNullOrEmpty(FromEmail) || String.IsNullOrEmpty(ToEmail) || String.IsNullOrEmpty(SmtpHost))
                return;

            var crashReport = new CrashReport(this);

            var parameterizedThreadStart = new ParameterizedThreadStart(ShowUI);
            var thread = new Thread(parameterizedThreadStart) {IsBackground = false};
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start(crashReport);
            thread.Join();
        }

        private static void ShowUI(object crashReportDialog)
        {
            ((CrashReport) crashReportDialog).ShowDialog();
        }
    }
}