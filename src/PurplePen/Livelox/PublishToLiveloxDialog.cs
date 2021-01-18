/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using PurplePen.Livelox.ApiContracts;

namespace PurplePen.Livelox
{
    partial class PublishToLiveloxDialog : BaseDialog
    {
        private readonly Controller controller;
        private readonly SymbolDB symbolDB;
        private readonly SettingsProvider settingsProvider = new SettingsProvider();
        private ImportableEvent existingImportableEvent;
        private bool isExecutingNow = false;
        private readonly Dictionary<Control, int> initialHeights = new Dictionary<Control, int>();
        private readonly List<IAbortable> ongoingCalls = new List<IAbortable>();
        private Control dialogParent;
        private bool aborted = false;

        public LiveloxPublishSettings PublishSettings { get; }

        public PublishToLiveloxDialog(Controller controller, SymbolDB symbolDB, LiveloxPublishSettings publishSettings)
        {
            InitializeComponent();
            this.controller = controller;
            this.symbolDB = symbolDB;
            this.PublishSettings = publishSettings;
            initialHeights.Add(this, Height);
            initialHeights.Add(settingsGroupBox, settingsGroupBox.Height);
            initialHeights.Add(existingEventGroupBox, existingEventGroupBox.Height);
            initialHeights.Add(userPanel, userPanel.Height);
        }

        public void InitializeImportableEvent(Form form, Action<LiveloxApiCall<ImportableEvent>> callback)
        {
            dialogParent = form;

            var importableEventId = controller.GetEventDB().GetEvent().liveloxImportableEventId;

            Action nullCallback = () =>
            {
                StopExecuting();
                dialogParent = null;
                callback(new LiveloxApiCall<ImportableEvent>()
                {
                    Result = null
                });
            };

            if (importableEventId != null)
            {
                var settings = settingsProvider.LoadSettings();
                var user = settings.Users.FirstOrDefault();
                if (user != null)
                {
                    StartExecuting(LiveloxResources.LoadingLiveloxEvent, closeDialogOnAbort: false);
                    var liveloxApiClient = CreateLiveloxApiClient(user.TokenInformation);
                    liveloxApiClient.GetImportableEvent(importableEventId, call =>
                    {
                        existingImportableEvent = call.Result;
                        
                        var statusCodeException = call.Exception as StatusCodeException;
                        
                        if (statusCodeException?.StatusCode == HttpStatusCode.NotFound /* the event, or its uploaded files, has been removed */ ||
                            statusCodeException?.StatusCode == HttpStatusCode.Forbidden /* the user doesn't have access to the event */ ) 
                        {
                            
                            // pretend the event hasn't been published
                            nullCallback();
                            return;
                        }

                        if (call.Exception is OAuth2Exception)
                        {
                            // an authorization problem - remove the user from the saved user list
                            settings.Users = settings.Users.Skip(1).ToArray();
                            settingsProvider.SaveSettings(settings);
                        }

                        StopExecuting();
                        dialogParent = null;
                        callback(call);
                    });
                }
                else
                {
                    nullCallback();
                }
            }
            else
            {
                nullCallback();
            }
        }

        public void Abort(bool closeDialog)
        {
            aborted = true;
            var callsToAbort = ongoingCalls.ToArray();
            foreach (var call in callsToAbort)
            {
                call.Abort();
                ongoingCalls.Remove(call);
            }

            if (closeDialog && !IsDisposed)
            {
                Close();
            }
        }

        private Control DialogParent => dialogParent ?? (IsHandleCreated ? this : Parent);

        private void UpdateDialog()
        {
            resolutionTextBox.Text = PublishSettings.GetResolution(controller.MapScale).ToString(CultureInfo.CurrentCulture);

            var settings = settingsProvider.LoadSettings();
            foreach (var user in settings.Users)
            {
                userComboBox.Items.Add(user);
            }

            if (settings.Users.Any())
            {
                userComboBox.Items.Add(new User()
                {
                    FirstName = $"[{LiveloxResources.AnotherUser}]",
                    PersonId = -1
                });
                userComboBox.SelectedIndex = 0;
            }

            var ev = existingImportableEvent?.ImportedEvent;
            if (ev != null)
            {
                var startTime = ev.TimeInterval.Start.Value.ToLocalTime();
                var endTime = ev.TimeInterval.End.Value.ToLocalTime();
                eventNameLabel.Text = ev.Name;
                eventOrganisersLabel.Text = string.Join(", ", ev.Organisers.Select(o => o.Name));
                eventTimeIntervalLabel.Text = startTime.ToShortDateString() + " " + startTime.ToShortTimeString() +
                                              " - " +
                                              (endTime.Date == startTime.Date ? "" : endTime.ToShortDateString() + " ") + endTime.ToShortTimeString();
            }

            ReflowUi();
        }

        private void UpdateSettings()
        {
            if (double.TryParse(resolutionTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var resolution))
            {
                if (LiveloxPublishSettings.IsLargeScaleMap(controller.MapScale))
                {
                    PublishSettings.largeScaleMapResolution = resolution;
                }
                else
                {
                    PublishSettings.smallScaleMapResolution = resolution;
                }
            }
        }

        private void CreateImportableEvent()
        {
            var selectedUser = GetSelectedUser();

            if (selectedUser == null)
            {
                AskForConsent(CreateImportableEvent);
            }
            else
            {
                CreateImportableEvent(selectedUser);
            }
        }

        private void UpdateImportableEvent()
        {
            var selectedUser = GetSelectedUser();
            if (selectedUser == null)
            {
                AskForConsent(UpdateImportableEvent);
            }
            else
            {
                UpdateImportableEvent(selectedUser);
            }
        }

        private User GetSelectedUser()
        {
            var selectedUser = userComboBox.SelectedItem as User;
            if (selectedUser == null || selectedUser.PersonId == -1)
            {
                return null;
            }
            return selectedUser;
        }

        private void AskForConsent(Action<User> nextStep)
        {
            bool rememberConsent;
            using (var consentRedirectDialog = new ConsentRedirectionDialog())
            {
                var dialogResult = consentRedirectDialog.ShowDialog(DialogParent);

                if (dialogResult == DialogResult.Cancel)
                {
                    return;
                }

                rememberConsent = consentRedirectDialog.RememberConsent;
            }

            var refreshTokenLifeLength = rememberConsent
                ? (TimeSpan?)null
                : TimeSpan.FromHours(1);

            StartExecuting(LiveloxResources.RedirectingToLivelox);

            var liveloxApiClient = CreateLiveloxApiClient(null);
            Action<LiveloxApiCall<User>> callback = call =>
            {
                SetProgressInfo(null);
                if (!call.Success)
                {
                    ShowErrorBox(call, true);
                    return;
                }

                var user = call.Result;
                
                // only save the user if it checked the "remember me" checkbox
                if (rememberConsent)
                {
                    var s = settingsProvider.LoadSettings();
                    s.Users = new[] { user }
                        .Concat(s.Users.Where(o => o.PersonId != user.PersonId))
                        .ToArray();
                    settingsProvider.SaveSettings(s);
                }
                
                nextStep(user);
            };

            liveloxApiClient.AskForUserConsent(this, refreshTokenLifeLength, callback, SetProgressInfo);
        }

        private void CreateImportableEvent(User user)
        {
            var manager = new PublishManager();
            string temporaryDirectory = null;
            try
            {
                StartExecuting(LiveloxResources.AssemblingCourseSettingInformation);
                UpdateSettings();
                temporaryDirectory = manager.CreateTemporaryDirectory();
                var importableEvent = manager.CreateImportableEvent(controller, symbolDB, PublishSettings.GetResolution(controller.MapScale), temporaryDirectory);

                SetProgressInfo(LiveloxResources.UploadingCourseSettingInformation);
                var liveloxApiClient = CreateLiveloxApiClient(user.TokenInformation);
                liveloxApiClient.CreateImportableEvent(importableEvent, call =>
                {
                    if (!call.Success)
                    {
                        manager.DeleteTemporatyDirectory(temporaryDirectory);
                        StopExecuting();
                        ShowErrorBox(call, true);
                        return;
                    }

                    var importableEventLink = call.Result;

                    // zip all files and upload them
                    var zipBytes = CreateZipFileBytes(temporaryDirectory, importableEvent);

                    liveloxApiClient.UploadFile(importableEventLink.Id, "files.zip", zipBytes, uploadFilesCall =>
                    {
                        if (!uploadFilesCall.Success)
                        {
                            manager.DeleteTemporatyDirectory(temporaryDirectory);
                            StopExecuting();
                            ShowErrorBox(uploadFilesCall, true);
                            return;
                        }

                        PersistLiveloxEventIdToDB(importableEventLink.Id);
                        PersistUserList(user);

                        manager.DeleteTemporatyDirectory(temporaryDirectory);
                        StopExecuting();
                        ShowImportableEventCreatedDialog(importableEventLink);
                    });
                });
            }
            catch (Exception ex)
            {
                manager.DeleteTemporatyDirectory(temporaryDirectory);
                StopExecuting();
                ShowErrorBox(ex, true);
            }
        }

        private void UpdateImportableEvent(User user)
        {
            var manager = new PublishManager();
            string temporaryDirectory = null;
            try
            {
                StartExecuting(LiveloxResources.AssemblingCourseSettingInformation);
                UpdateSettings();
                temporaryDirectory = manager.CreateTemporaryDirectory();
                var importableEvent = manager.CreateImportableEvent(controller, symbolDB, PublishSettings.GetResolution(controller.MapScale), temporaryDirectory);

                SetProgressInfo(LiveloxResources.UploadingCourseSettingInformation);
                var liveloxApiClient = CreateLiveloxApiClient(user.TokenInformation);
                liveloxApiClient.UpdateImportableEvent(existingImportableEvent.Link.Id, importableEvent,
                    updateImportableEventCall =>
                    {
                        if (!updateImportableEventCall.Success)
                        {
                            manager.DeleteTemporatyDirectory(temporaryDirectory);
                            StopExecuting();
                            if ((updateImportableEventCall.Exception as StatusCodeException)?.StatusCode == HttpStatusCode.Forbidden)
                            {
                                ShowErrorBox(new Exception(LiveloxResources.AccessDeniedToLiveloxEvent), true);
                            }
                            else
                            {
                                ShowErrorBox(updateImportableEventCall, true);
                            }
                            return;
                        }

                        var importableEventLink = updateImportableEventCall.Result;

                        // zip all files and upload them
                        var zipBytes = CreateZipFileBytes(temporaryDirectory, importableEvent);
                        liveloxApiClient.UploadFile(importableEventLink.Id, "files.zip", zipBytes, uploadFilesCall =>
                        {
                            if (!uploadFilesCall.Success)
                            {
                                manager.DeleteTemporatyDirectory(temporaryDirectory);
                                StopExecuting();
                                ShowErrorBox(uploadFilesCall, true);
                                return;
                            }

                            PersistUserList(user);
                            
                            if (importableEventLink.LiveloxImportEventUrl != null)
                            {
                                manager.DeleteTemporatyDirectory(temporaryDirectory);
                                PersistLiveloxEventIdToDB(importableEventLink.Id);
                                StopExecuting();
                                ShowImportableEventCreatedDialog(importableEventLink);
                            }
                            else
                            {
                                SetProgressInfo(LiveloxResources.UpdatingLiveloxEvent);
                                liveloxApiClient.ImportImportableEvent(existingImportableEvent.Link.Id, importImportableEventCall =>
                                {
                                    if (!importImportableEventCall.Success)
                                    {
                                        manager.DeleteTemporatyDirectory(temporaryDirectory);
                                        StopExecuting();
                                        ShowErrorBox(importImportableEventCall, true);
                                        return;
                                    }

                                    importableEventLink = importImportableEventCall.Result;

                                    PersistLiveloxEventIdToDB(importableEventLink.Id);

                                    manager.DeleteTemporatyDirectory(temporaryDirectory);
                                    StopExecuting();
                                    ShowImportableEventUpdatedDialog(importableEventLink);
                                });
                            }
                        });
                    });
            }
            catch (Exception ex)
            {
                manager.DeleteTemporatyDirectory(temporaryDirectory);
                StopExecuting();
                ShowErrorBox(ex, true);
            }
        }

        private LiveloxApiClient CreateLiveloxApiClient(OAuth2TokenInformation tokenInformation)
        {
            return new LiveloxApiClient(tokenInformation, OnApiClientRequestCreated, OnApiClientRequestCompleted);
        }

        private static byte[] CreateZipFileBytes(string directory, ImportableEvent importableEvent)
        {
            var fileNames = importableEvent.Maps.Select(map => map.FileName)
                .Concat(importableEvent.CourseDataFileNames)
                .Concat(importableEvent.CourseImageFileNames)
                .ToArray();

            var buffer = new byte[4096];
            using (var zipStream = new MemoryStream())
            {
                using (var zipOutputStream = new ZipOutputStream(zipStream))
                {
                    foreach (var fileName in fileNames)
                    {
                        using (var fileStream = new FileStream(Path.Combine(directory, fileName), FileMode.Open, FileAccess.Read))
                        {
                            var zipEntry = new ZipEntry(ZipEntry.CleanName(fileName))
                            {
                                IsUnicodeText = true
                            };
                            zipOutputStream.PutNextEntry(zipEntry);
                            int sourceBytes;
                            do
                            {
                                sourceBytes = fileStream.Read(buffer, 0, buffer.Length);
                                zipOutputStream.Write(buffer, 0, sourceBytes);
                            } while (sourceBytes > 0);
                        }
                    }

                    zipOutputStream.Finish();
                    return zipStream.ToArray();
                }
            }
        }

        private void PersistLiveloxEventIdToDB(string liveloxImportableEventId)
        {
            var eventDB = controller.GetEventDB();
            var undoMgr = controller.GetUndoMgr();
            const int commandNumber = 27635; // what number to use here?
            undoMgr.BeginCommand(commandNumber, CommandNameText.SetLiveloxImportableEventId);
            ChangeEvent.SetLiveloxImportableEventId(eventDB, liveloxImportableEventId);
            undoMgr.EndCommand(commandNumber);
        }

        private void PersistUserList(User user)
        {
            var settings = settingsProvider.LoadSettings();
            if (settings.Users.Any(o => o.PersonId == user.PersonId))
            {
                // the user is to be remembered,
                // place it first in the list
                settings.Users = new[] { user }
                    .Concat(settings.Users.Where(o => o.PersonId != user.PersonId))
                    .ToArray();
                settingsProvider.SaveSettings(settings);
            }
        }

        private void RemoveSelectedUser()
        {
            if (userComboBox.SelectedItem is User selectedUser)
            {
                Action executeRemoveSelectedUser = () =>
                {
                    var settings = settingsProvider.LoadSettings();
                    settings.Users = settings.Users.Where(o => o.PersonId != selectedUser.PersonId).ToArray();
                    settingsProvider.SaveSettings(settings);
                    userComboBox.Items.Remove(selectedUser);
                    userComboBox.SelectedIndex = 0;
                };

                StartExecuting(LiveloxResources.RemovingUser);
                var liveloxApiClient = CreateLiveloxApiClient(selectedUser.TokenInformation);
                liveloxApiClient.RevokeToken(selectedUser.TokenInformation.RefreshToken, "refresh_token",
                    deleteRefreshTokenCall =>
                    {
                        if (!deleteRefreshTokenCall.Success)
                        {
                            StopExecuting();

                            try
                            {
                                if((deleteRefreshTokenCall.Exception as StatusCodeException)?.StatusCode == HttpStatusCode.Unauthorized ||
                                    JsonConvert.DeserializeObject<ApiError>(deleteRefreshTokenCall.Exception.Message)?.Error == "invalid_grant")
                                {
                                    // the token was not found in Livelox, just remove it locally
                                    executeRemoveSelectedUser();
                                    return;
                                }
                            }
                            catch
                            {
                                // just swallow and continue
                            }

                            ShowErrorBox(deleteRefreshTokenCall, false);
                            return;
                        }

                        liveloxApiClient.RevokeToken(liveloxApiClient.TokenInformation.AccessToken, "access_token",
                            deleteAccessTokenCall =>
                            {
                                InvokeOnUiThread(() =>
                                {
                                    StopExecuting();
                                    if (!deleteAccessTokenCall.Success)
                                    {
                                        ShowErrorBox(deleteAccessTokenCall, false);
                                        return;
                                    }
                                    executeRemoveSelectedUser();
                                });
                            });
                    });
            }
        }

        private void ShowEvent()
        {
            ShowUrlInBrowser(existingImportableEvent.Link.LiveloxShowEventUrl);
        }

        private void EditEvent()
        {
            ShowUrlInBrowser(existingImportableEvent.Link.LiveloxEditEventUrl);
        }

        private void StartExecuting(string info = null, bool closeDialogOnAbort = true)
        {
            InvokeOnUiThread(() =>
            {
                isExecutingNow = true;
                controller.ShowProgressDialog(false, () => Abort(closeDialogOnAbort));
                if (info != null)
                {
                    controller.UpdateProgressDialog(info, 0);
                }

                ReflowUi();
            });
        }

        private void StopExecuting()
        {
            InvokeOnUiThread(() =>
            {
                controller.EndProgressDialog();
                isExecutingNow = false;
                ReflowUi();
            });
        }

        private void SetProgressInfo(string info)
        {
            InvokeOnUiThread(() =>
            {
                if(info == null)
                {
                    controller.EndProgressDialog();
                }
                else
                {
                    controller.UpdateProgressDialog(info, 0);
                }
            });
        }

        private void ReflowUi()
        {
            var userPanelVisible = userComboBox.Items.Count > 0;
            var liveloxEvent = existingImportableEvent?.ImportedEvent;

            settingsGroupBox.Visible = showSettingsCheckBox.Checked;
            userPanel.Visible = userPanelVisible;
            existingEventGroupBox.Visible = liveloxEvent != null;

            publishButton.Visible = liveloxEvent == null;
            publishButtonMarginPanel.Visible = publishButton.Visible;
            updateEventButton.Visible = liveloxEvent != null;
            publishToOtherEventButton.Visible = liveloxEvent != null;
            publishToOtherEventButtonMarginPanel.Visible = publishToOtherEventButton.Visible;
            removeUserLink.Visible = GetSelectedUser() != null;

            publishButton.Enabled = !isExecutingNow;
            updateEventButton.Enabled = !isExecutingNow;
            publishToOtherEventButton.Enabled = !isExecutingNow;
            removeUserLink.Enabled = !isExecutingNow;

            settingsGroupBox.Height = initialHeights[settingsGroupBox] - (userPanelVisible ? 0 : 1) * (initialHeights[userPanel] + 16);

            Height = initialHeights[this]
                     - (settingsGroupBox.Visible ? initialHeights[settingsGroupBox] - settingsGroupBox.Height : initialHeights[settingsGroupBox])
                     - (existingEventGroupBox.Visible ? 0 : initialHeights[existingEventGroupBox]);
        }

        private static void ShowUrlInBrowser(string url)
        {
            System.Diagnostics.Process.Start(url);
        }

        private void ShowImportableEventCreatedDialog(ImportableEventLink importableEventLink)
        {
            InvokeOnUiThread(() =>
            {
                // we're done in this dialog; close it
                Hide();

                ShowDialogBox(LiveloxResources.ImportableEventCreatedInformation, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

                // show import user interface in Livelox in browser
                ShowUrlInBrowser(importableEventLink.LiveloxImportEventUrl);

                Close();
            });
        }

        private void ShowImportableEventUpdatedDialog(ImportableEventLink importableEventLink)
        {
            InvokeOnUiThread(() =>
            {
                // we're done in this dialog; close it
                Hide();

                var result = ShowDialogBox(LiveloxResources.ImportableEventUpdatedInformation, MiscText.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    // show edit user interface in Livelox in browser
                    ShowUrlInBrowser(importableEventLink.LiveloxEditEventUrl);
                }

                Close();
            });
        }

        private void ShowErrorBox<T>(LiveloxApiCall<T> call, bool closeDialog)
        {
            ShowErrorBox(call.Exception, closeDialog);
        }

        private void ShowErrorBox(Exception ex, bool closeDialog)
        {
            InvokeOnUiThread(() =>
            {
                if (!aborted)
                {
                    string message;
                    if ((ex as StatusCodeException)?.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        message = LiveloxResources.UnauthorizedMessage;
                    }
                    else
                    {
                        message = ex?.Message;
                    }

                    ShowDialogBox(message, MiscText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (closeDialog)
                {
                    Close();
                }
            });
        }

        private DialogResult ShowDialogBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            try
            {
                return MessageBox.Show(DialogParent, text, caption, buttons, icon);
            }
            catch
            {
                // just swallow
                return DialogResult.OK;
            }
        }

        private void PublishToLiveloxDialog_Shown(object sender, EventArgs e)
        {
            UpdateDialog();
        }

        private void publishButton_Click(object sender, EventArgs e)
        {
            CreateImportableEvent();
        }

        private void publishToOtherEventButton_Click(object sender, EventArgs e)
        {
            CreateImportableEvent();
        }

        private void updateEventButton_Click(object sender, EventArgs e)
        {
            UpdateImportableEvent();
        }

        private void showSettingsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ReflowUi();
        }

        private void removeUserLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RemoveSelectedUser();
        }

        private void showEventLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowEvent();
        }

        private void editEventLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            EditEvent();
        }

        private void learnMoreLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowUrlInBrowser("https://www.livelox.com/Documentation");
        }

        private void userComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ReflowUi();
        }

        private void OnApiClientRequestCreated(IAbortable call)
        {
            ongoingCalls.Add(call);
        }

        private void OnApiClientRequestCompleted(IAbortable call)
        {
            ongoingCalls.Remove(call);
        }

        private void InvokeOnUiThread(Action action)
        {
            try
            {
                DialogParent.InvokeOnUiThread(action);
            }
            catch
            {
                // just swallow
            }
        }
    }
}