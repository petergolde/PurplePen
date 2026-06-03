using Avalonia;
using Map_SkiaStd;
using Microsoft.Extensions.DependencyInjection;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using PurplePen.ViewModels;
using System;
using System.Globalization;
using System.IO;

namespace AvPurplePen
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Initialize things needed for PurplePenCore.
            RegisterServices();

            // Set up user settings.
            string userSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PurplePen", "PurplePenSettings.json");
            UserSettings.Initialize(userSettingsPath);

            InitUILanguage();
            FontDesc.InitializeFonts();

            // Initialization code. Don't use any Avalonia APIs or any
            // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
            // yet and stuff might break.
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Initialize all the services that PurplePenCore (and the application as a whole) uses.
        private static void RegisterServices()
        {
            ServiceProvider serviceProvider;

            // Register all the services that PurplePenCore requires.
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<IGraphicsBitmapLoader, SkiaBitmapGraphicsLoader>();
            services.AddSingleton<IBitmapGraphicsTargetProvider, SkiaBitmapGraphicsTargetProvider>();
            services.AddSingleton<IFontLoader>(SkiaFontLoader.Instance); 
            services.AddSingleton<ITextMetrics, Skia_TextMetrics>();
            services.AddSingleton<IFileLoaderProvider, SkiaFileLoaderProvider>();
            services.AddSingleton<IPdfWriter, PdfWriter>();
            services.AddSingleton<IApplicationIdleService, ApplicationIdleServiceAdapter>();

            // Transient (not singleton): PdfLoadingUI holds per-conversion state
            // (completion flag, dialog handle), so each PDF validation must get a
            // fresh instance. CoreMapUtil.ValidatePdf resolves it once per call.
            services.AddTransient<IPdfLoadingStatus, PdfLoadingUI>();

            // IDialogService depends on the MainWindow, which is created later by App.
            // The factory defers construction until first use, by which time App.MainWindow is set.
            services.AddSingleton<IDialogService>(sp => new DialogService(App.MainWindow!));
            services.AddSingleton<IUILanguage, UILanguageService>();
            services.AddSingleton<IEventDispatcherService, EventDispatcherService>();

            serviceProvider = services.BuildServiceProvider();
            Services.RegisterServiceProvider(serviceProvider);
        }

        // Initialize the UI language. If there is no language set, keep with the default language.
        private static void InitUILanguage()
        {
            string uiLanguage = UserSettings.Current.UILanguage;

            if (!string.IsNullOrEmpty(uiLanguage)) {
                try {
                    System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(uiLanguage);
                }
                catch (Exception) { }        // Ignore problem -- e.g. this culture name isn't supported.
            }
        }



        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        { 
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }
    }
}
