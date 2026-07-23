using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace VisualDiff.Views
{
    // A dependency-free Avalonia error message box used for startup and file-operation errors.
    internal sealed class MessageBoxWindow : Window
    {
        public MessageBoxWindow(string message, bool centerOnScreen)
        {
            Title = "VisualDiff";
            CanResize = false;
            ShowInTaskbar = centerOnScreen;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = centerOnScreen
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner;

            TextBlock heading = new TextBlock {
                Text = "VisualDiff could not continue",
                FontSize = 17,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
            };
            TextBlock messageText = new TextBlock {
                Text = message,
                MaxWidth = 560,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            };
            Button okButton = new Button {
                Content = "OK",
                MinWidth = 88,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            okButton.Click += delegate { Close(); };

            StackPanel content = new StackPanel {
                Spacing = 16,
                Children = {
                    heading,
                    messageText,
                    okButton,
                },
            };
            Content = new Border {
                Padding = new Thickness(24, 20),
                Child = content,
            };

            Opened += delegate { okButton.Focus(); };
        }
    }
}
