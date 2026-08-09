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
using System.Threading;

namespace AvPurplePen
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Install the process-wide exception handlers before anything at all can fail.
            // The Avalonia dispatcher does not exist yet, so its handlers are installed later,
            // from App.Initialize.
            CrashHandler.InstallProcessHandlers();

            try {
                // Initialize things needed for PurplePenCore.
                RegisterServices();

                // Set up user settings.
                string userSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PurplePen", "PurplePenSettings.json");
                UserSettings.Initialize(userSettingsPath);

                InitUILanguage();
                FontDesc.InitializeFonts();

                // Report statistics about the invocation of Purple Pen. This is done asynchronously, so it doesn't block the startup of the application.
                Services.ServiceProvider.GetRequiredService<StatisticsReporter>().ReportStatistics();

                // Initialization code. Don't use any Avalonia APIs or any
                // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
                // yet and stuff might break.
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            // The "when" filter matters as much as the catch body. Under a debugger (without
            // PPEN_DEBUGEXCEPTIONHANDLING) the filter returns false, so the exception keeps
            // propagating and the debugger still breaks on it as unhandled. A plain catch with
            // a rethrow would not do: the stack would already have unwound to here by the time
            // we decided, losing exactly the state the developer wants to look at.
            catch (Exception e) when (!CrashHandler.SkipInstallation) {
                // Anything reaching here either happened before Avalonia was running -- in which
                // case there is no way to show a window -- or escaped the dispatcher loop
                // entirely. Either way the application is over; report what we can and get out.
                CrashHandler.HandleFatalStartupException(e);
                Environment.Exit(1);
            }
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

            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IUILanguage, UILanguageService>();
            services.AddSingleton<IWebsiteLauncher, WebsiteLauncherService>();
            services.AddSingleton<IEventDispatcherService, EventDispatcherService>();
            services.AddSingleton<StatisticsReporter>();

            // Transient (not singleton): PdfLoadingUI holds per-conversion state
            // (completion flag, dialog handle), so each PDF validation must get a
            // fresh instance. CoreMapUtil.ValidatePdf resolves it once per call.
            services.AddTransient<IPdfLoadingStatus, PdfLoadingUI>();

            // Configure IHttpClientProvider with resiliance (retrying) defaults.
            services.AddHttpClient();
            services.ConfigureHttpClientDefaults(
                httpClientBuilder => httpClientBuilder.AddStandardResilienceHandler());

            serviceProvider = services.BuildServiceProvider();
            Services.RegisterServiceProvider(serviceProvider);
        }

        // Initialize the UI language. If there is no language set, keep with the default language.
        private static void InitUILanguage()
        {
            string uiLanguage = UserSettings.Current.UILanguage;

            if (!string.IsNullOrEmpty(uiLanguage)) {
                try {
                    CultureInfo cultureInfo = CultureInfo.GetCultureInfo(uiLanguage);

                    // Only the app-domain default is set, never Thread.CurrentThread.CurrentUICulture:
                    // a thread-level culture would shadow DefaultThreadCurrentUICulture forever and
                    // prevent UILanguageService from changing the language later. See the comment there.
                    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
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
