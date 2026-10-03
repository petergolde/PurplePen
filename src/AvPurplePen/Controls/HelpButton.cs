// HelpButton.cs
//
// A small circular "?" button (in the style of the macOS help button) that
// opens a help topic in the user's web browser when clicked. Its appearance
// comes from the HelpButton ControlTheme in App.axaml.
//
// Usage:
//     xmlns:controls="using:AvPurplePen.Controls"
//     <controls:HelpButton HelpTopic="EventCustomizeCourseAppearance.htm"/>

using Avalonia;
using Avalonia.Controls;
using PurplePen;
using System;

namespace AvPurplePen.Controls
{
    /// <summary>
    /// Circular help button. When clicked, shows the help topic named by
    /// <see cref="HelpTopic"/> via <see cref="IWebsiteLauncher.ShowHelpTopic"/>.
    /// </summary>
    public class HelpButton : Button
    {
        /// <summary>
        /// Defines the <see cref="HelpTopic"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> HelpTopicProperty =
            AvaloniaProperty.Register<HelpButton, string?>(nameof(HelpTopic));

        /// <summary>
        /// The name of the help topic to show (e.g. "EventCustomizeCourseAppearance.htm").
        /// </summary>
        public string? HelpTopic
        {
            get => GetValue(HelpTopicProperty);
            set => SetValue(HelpTopicProperty, value);
        }

        /// <summary>
        /// Use the HelpButton ControlTheme / styles rather than those for Button.
        /// </summary>
        protected override Type StyleKeyOverride => typeof(HelpButton);

        /// <summary>
        /// Raises the Click event and any bound Command, then shows the help topic.
        /// </summary>
        protected override async void OnClick()
        {
            base.OnClick();

            if (!string.IsNullOrEmpty(HelpTopic)) {
                await Services.WebsiteLauncher.ShowHelpTopic(HelpTopic);
            }
        }
    }
}
