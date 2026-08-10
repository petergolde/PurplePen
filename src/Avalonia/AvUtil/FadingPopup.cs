using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.LogicalTree;
using Avalonia.Media;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AvUtil
{
    // Shows a single tooltip-like popup at an arbitrary location, with timed fade in / fade out.
    //
    // This is a general-purpose helper for any Avalonia program: create one instance (typically one
    // per window) and call Show()/Hide(). It wraps an Avalonia Popup rather than deriving from it, so
    // the public surface is just the two methods plus a few properties. The popup hosts its
    // content in its own top-level, so the single-pixel border and drop shadow are never clipped by
    // the owner window's edges.
    //
    // Only one popup is shown at a time. The lifecycle is:
    //     Show() called  ->  wait ShowDelay  ->  fade in over FadeInDuration  ->
    //     wait HideDelay  ->  fade out over FadeOutDuration  ->  hidden.
    // Calling Show() again restarts this cycle (resetting the timers); if the popup is already visible
    // it simply stays up and the HideDelay timer is restarted. Calling Hide() fades it out immediately.
    public sealed class FadingPopup
    {
        // How long to wait after Show() before the popup begins to appear.
        public TimeSpan ShowDelay { get; set; } = TimeSpan.FromMilliseconds(50);

        // How long the fade-in (opacity 0 -> 1) takes.
        public TimeSpan FadeInDuration { get; set; } = TimeSpan.FromMilliseconds(100);

        // How long the popup stays fully visible before it begins to fade out on its own.
        public TimeSpan HideDelay { get; set; } = TimeSpan.FromSeconds(5);

        // How long the fade-out (opacity 1 -> 0) takes.
        public TimeSpan FadeOutDuration { get; set; } = TimeSpan.FromMilliseconds(300);

        // Horizontal offset, in logical pixels, added to the position passed to Show(). Lets the popup
        // be nudged away from the pointer. Applied on each Show().
        public double XOffset { get; set; } = 0;

        // Vertical offset, in logical pixels, added to the position passed to Show(). Applied on each Show().
        public double YOffset { get; set; } = 0;

        // Background brush painted behind the content (inside the border). Defaults to white.
        public IBrush? Background
        {
            get => border.Background;
            set => border.Background = value;
        }

        // The wrapped popup and its visual chrome. Created once and reused for every Show().
        private readonly Popup popup;
        private readonly Border border;
        private readonly ContentControl contentControl;
        private readonly Transitions transitions;
        private readonly DoubleTransition opacityTransition;

        // Cancels the in-flight Show()/Hide() sequence when a new call supersedes it.
        private CancellationTokenSource? cts;

        // The control the popup is currently parented to in the logical tree. The popup must be
        // connected to the logical tree so its content can resolve the application's control themes
        // (otherwise templated content such as a TextBox gets no template and collapses to nothing).
        private Control? logicalParent;

        // Builds the reusable popup, border (single black pixel border + drop shadow), and content host.
        public FadingPopup()
        {
            contentControl = new ContentControl();

            border = new Border {
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Background = Brushes.White,
                // A soft drop shadow. The uniform margin below leaves room for it inside the popup
                // so it is not clipped by the popup's top-level bounds.
                BoxShadow = BoxShadows.Parse("0 2 8 0 #50000000"),
                Margin = new Thickness(10),
                Child = contentControl,
            };

            // A transition used to animate the border's opacity. Its Duration is changed before each
            // fade so the same transition serves both fade-in and fade-out.
            opacityTransition = new DoubleTransition {
                Property = Visual.OpacityProperty,
                Duration = FadeInDuration,
            };
            transitions = new Transitions { opacityTransition };

            popup = new Popup {
                Child = border,
                IsLightDismissEnabled = false,    // we control hiding explicitly; clicks elsewhere don't dismiss.
                Placement = PlacementMode.AnchorAndGravity,
                PlacementAnchor = PopupAnchor.TopLeft,
                PlacementGravity = PopupGravity.BottomRight,
            };
        }

        // Shows the popup containing the given content at the given location.
        //   relativeTo: the control the position is measured against; also supplies the top-level that
        //               hosts the popup.
        //   position:   the location, in coordinates relative to relativeTo, of the popup's top-left
        //               (before XOffset/YOffset are added).
        //   content:    the content to display (any object; e.g. a string or a Control such as a TextBox).
        // Calling Show() again resets the timers. If the popup is already visible it moves to the new
        // location.
        public async void Show(Control relativeTo, Point position, object content)
        {
            CancellationToken token = StartNewSequence();

            contentControl.Content = content;

            // Connect the popup to the logical tree under the target control so its content inherits the
            // application's styles / control themes. Without this, templated content renders untemplated.
            if (!ReferenceEquals(logicalParent, relativeTo)) {
                ((ISetLogicalParent)popup).SetParent(null);
                ((ISetLogicalParent)popup).SetParent(relativeTo);
                logicalParent = relativeTo;
            }

            // Position the popup. When it is already open, changing these reconfigures its location live.
            popup.PlacementTarget = relativeTo;
            popup.HorizontalOffset = position.X + XOffset;
            popup.VerticalOffset = position.Y + YOffset;

            bool wasOpen = popup.IsOpen;
            try {
                if (!wasOpen) {
                    await Task.Delay(ShowDelay, token);
                    SetOpacityInstant(0);
                    popup.IsOpen = true;
                    AnimateOpacity(1, FadeInDuration);
                    await Task.Delay(FadeInDuration, token);
                }
                else {
                    // Already visible: make sure we are back at full opacity (in case Show() arrived
                    // while fading out), then fall through to restart the hide timer.
                    AnimateOpacity(1, FadeInDuration);
                }

                await Task.Delay(HideDelay, token);

                AnimateOpacity(0, FadeOutDuration);
                await Task.Delay(FadeOutDuration, token);
                popup.IsOpen = false;
            }
            catch (OperationCanceledException) {
                // Superseded by a later Show()/Hide(); that call now owns the popup state.
            }
        }

        // Fades the popup out immediately (over FadeOutDuration) and hides it. Does nothing if the
        // popup is not currently shown.
        public async void Hide()
        {
            CancellationToken token = StartNewSequence();

            if (!popup.IsOpen)
                return;

            try {
                AnimateOpacity(0, FadeOutDuration);
                await Task.Delay(FadeOutDuration, token);
                popup.IsOpen = false;
            }
            catch (OperationCanceledException) {
                // Superseded by a later Show()/Hide().
            }
        }

        // Cancels any in-flight sequence and returns the cancellation token for the new one.
        private CancellationToken StartNewSequence()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            return cts.Token;
        }

        // Sets the border's opacity immediately, with no animation (transitions disabled).
        private void SetOpacityInstant(double value)
        {
            border.Transitions = null;
            border.Opacity = value;
        }

        // Animates the border's opacity to the given value over the given duration.
        private void AnimateOpacity(double value, TimeSpan duration)
        {
            opacityTransition.Duration = duration;
            border.Transitions = transitions;
            border.Opacity = value;
        }
    }
}
