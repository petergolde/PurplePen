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
                // Show the welcome screen first. It creates and shows the real
                // main window itself (possibly after the New Event wizard) once
                // an event has been created or loaded.
                InitialScreenWindow initialScreen = new InitialScreenWindow {
                    DataContext = new InitialScreenViewModel(),
                };
                desktop.MainWindow = initialScreen;
            }

            base.OnFrameworkInitializationCompleted();

            ApplicationIdleService.Initialize();
        }

    }
}