using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using AvPurplePen.Views;
using Semi.Avalonia;
using System;
using System.Linq;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen
{
    public partial class App : Application
    {
        /// <summary>
        /// The main application window. Set during initialization and used by
        /// the IDialogService factory to create modal dialogs.
        /// </summary>
        public static Window? MainWindow { get; private set; }

        /// <summary>
        /// Custom theme variant for PurplePen, based on Semi.Avalonia's Desert (Light) scheme.
        /// Colors are defined in Themes/PurplePenColors.axaml.
        /// </summary>
        /// 
        //public static readonly ThemeVariant PurplePenTheme = new("PurplePen", ThemeVariant.Light);

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

#if DEBUG
            this.AttachDeveloperTools();
#endif
            // Register our custom color scheme with the SemiTheme so its
            // ThemeDictionaries resolve our variant.
            //SemiTheme semiTheme = (SemiTheme)Styles[0];
            //semiTheme.Resources!.ThemeDictionaries[PurplePenTheme] =
            //    new ResourceInclude(new Uri("avares://AvPurplePen/")) { Source = new Uri("/Themes/PurplePenScheme.axaml", UriKind.Relative) };

            //RequestedThemeVariant = PurplePenTheme;

            RequestedThemeVariant = ThemeVariant.Light;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                MainWindowViewModel mainWindowViewModel = new MainWindowViewModel();
                Controller controller = new Controller(mainWindowViewModel);

                MainWindow mainWindow = new MainWindow {
                    DataContext = mainWindowViewModel,
                };
                desktop.MainWindow = mainWindow;
                App.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();

            ApplicationIdleService.Initialize();
        }

    }
}