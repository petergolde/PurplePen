// IUserInterface implementation part of MainWindowViewModel.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace PurplePen.ViewModels
{
    public partial class MainWindowViewModel: IUserInterface
    {
        public void Initialize(Controller controller, SymbolDB symbolDB)
        {
            this.controller = controller;
            this.symbolDB = symbolDB;

            DescriptionViewerViewModel.SymbolDB = symbolDB;
            DescriptionViewerViewModel.Controller = controller;
            CoursePartBannerViewModel.Controller = controller;
        }

        public Size Size => throw new NotImplementedException();

        public void QueueIdleEvent()
        {
            Services.ServiceProvider.GetRequiredService<IApplicationIdleService>().QueueIdleEvent();
        }

        public void PostDelayedAction(Action action)
        {
            Services.ServiceProvider.GetRequiredService<IEventDispatcherService>().PostMessage(action);
        }

        public async Task InfoMessage(string message)
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = message,
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Information
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        public async Task WarningMessage(string message)
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = message,
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Warning
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        public async Task ErrorMessage(string message)
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = message,
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Error
            };
            await Services.DialogService.ShowDialogAsync(vm);
        }

        public async Task<bool> OKCancelMessage(string message, bool okDefault)
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = message,
                Buttons = MessageBoxButtons.OkCancel,
                DefaultButton = okDefault ? MessageBoxButton.Ok : MessageBoxButton.Cancel,
                Icon = MessageBoxIcon.Question
            };
            await Services.DialogService.ShowDialogAsync(vm);
            return vm.ChosenButton == MessageBoxButton.Ok;
        }

        public async Task<YesNoCancel> YesNoCancelQuestion(string message, bool yesDefault)
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = message,
                Buttons = MessageBoxButtons.YesNoCancel,
                DefaultButton = yesDefault ? MessageBoxButton.Yes : MessageBoxButton.No,
                Icon = MessageBoxIcon.Question
            };
            await Services.DialogService.ShowDialogAsync(vm);
            if (vm.ChosenButton == MessageBoxButton.Yes)
                return YesNoCancel.Yes;
            else if (vm.ChosenButton == MessageBoxButton.No)
                return YesNoCancel.No;
            else
                return YesNoCancel.Cancel;
        }

        public async Task<bool> YesNoQuestion(string message, bool yesDefault)
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = message,
                Buttons = MessageBoxButtons.YesNo,
                DefaultButton = yesDefault ? MessageBoxButton.Yes : MessageBoxButton.No,
                Icon = MessageBoxIcon.Question
            };
            await Services.DialogService.ShowDialogAsync(vm);
            return vm.ChosenButton == MessageBoxButton.Yes;
        }

        public async Task<YesNoCancel> MovingSharedControl(string controlCode, string otherCourses)
        {
            MoveControlChoiceDialogViewModel vm = new MoveControlChoiceDialogViewModel {
                Code = controlCode,
                CourseList = otherCourses
            };
            await Services.DialogService.ShowDialogAsync(vm);
            switch (vm.Choice) {
                case MoveControlChoice.Move:
                    return YesNoCancel.Yes;
                case MoveControlChoice.Duplicate:
                    return YesNoCancel.No;
                default:
                    return YesNoCancel.Cancel;
            }
        }

        // ===== Operation-in-progress dialog plumbing =====
        //
        // The WinForms IUserInterface contract is three plain (synchronous)
        // calls: Show / Update / End. We surface the same shape on top of
        // IDialogService.ShowOwnedDialog. All state lives on the handle:
        //   * progressDialog.ViewModel.IsIndeterminate
        //   * "Did we close it ourselves?" → progressDialog.ClosedProgrammatically

        private INonModalDialog<OperationInProgressDialogViewModel>? progressDialog = null;

        /// <summary>
        /// Shows the "Operation in Progress" dialog as a modal owned dialog.
        /// The owner is disabled (classic modal) while the operation runs,
        /// but this call returns immediately so the caller can keep working
        /// and poll <see cref="UpdateProgressDialog"/>.
        /// </summary>
        public void ShowProgressDialog(bool knownDuration, Action onCancelPressed)
        {
            // Defensive: tear down a previous dialog that wasn't ended.
            if (progressDialog != null) {
                EndProgressDialog();
            }

            OperationInProgressDialogViewModel vm = new OperationInProgressDialogViewModel {
                InformationLabel = "",
                ProgressAmount = knownDuration ? 0.0 : (double?)null,
            };

            // `dialog` is a local (not progressDialog directly) so the
            // closure below captures THIS dialog rather than whatever the
            // field happens to point at when the continuation eventually
            // fires — important if ShowProgressDialog is called again
            // before the previous dialog's close-continuation runs.
            INonModalDialog<OperationInProgressDialogViewModel> dialog =
                Services.DialogService.ShowOwnedDialog(vm, disableOwner: true);
            progressDialog = dialog;

            // When the dialog closes, fire onCancelPressed iff the close
            // was user-initiated (handle.ClosedProgrammatically stays
            // false in that case).
            if (onCancelPressed != null) {
                _ = dialog.ClosedTask.ContinueWith(
                    _ => {
                        if (!dialog.ClosedProgrammatically)
                            onCancelPressed();
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
            }

            Services.ServiceProvider.GetRequiredService<IEventDispatcherService>().ProcessPendingMessages();
        }

        /// <summary>
        /// Updates the status text and (for determinate mode) the progress
        /// fraction. Returns true once the user has clicked Cancel so the
        /// caller can abort.
        /// </summary>
        public bool UpdateProgressDialog(string info, double fractionDone)
        {
            if (progressDialog == null)
                return true;   // No dialog open — treat as cancelled (matches WinForms).

            progressDialog.ViewModel.InformationLabel = info;
            if (!progressDialog.ViewModel.IsIndeterminate)
                progressDialog.ViewModel.ProgressAmount = fractionDone;

            Services.ServiceProvider.GetRequiredService<IEventDispatcherService>().ProcessPendingMessages();

            // User cancelled iff the dialog closed and we didn't close it.
            return progressDialog.ClosedTask.IsCompleted && !progressDialog.ClosedProgrammatically;
        }

        /// <summary>
        /// Tears down the progress dialog. Safe to call when the user has
        /// already cancelled (the window is already closed — Close() is a
        /// no-op) or when nothing was shown.
        /// </summary>
        public void EndProgressDialog()
        {
            progressDialog?.Close();
            progressDialog = null;
        }

        public string GetOpenFileName()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> FindMissingMapFile(string missingMapFile)
        {
            throw new NotImplementedException();
        }

        public bool GetCurrentLocation(out PointF location, out float pixelSize)
        {
#if PORTING
            // TODO: get correct pixelSize.
            if (MouseLocationInMap.HasValue) {
                location = MouseLocationInMap.Value;
                pixelSize = 0.1F;
                return true;
            }
            else {
                location = new PointF();
                pixelSize = 0.1F;
                return false;
            }
#endif
        }

        public int LogicalToDeviceUnits(int value)
        {
            throw new NotImplementedException();
        }


        public void ShowTopologyView()
        {
#if PORTING
            // Not yet implemented.
            return;
#endif
        }

    }
}
