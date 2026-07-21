
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.IO;
using System.Reflection;

[TestClass]
public class GlobalTestSetup
{
    private static ServiceProvider serviceProvider;

    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        // Register the service provider for PurplePenCore.
        // Register all the services that PurplePenCore requires.
        ServiceCollection services = new ServiceCollection();
#if true
        services.AddSingleton<IGraphicsBitmapLoader, SkiaBitmapGraphicsLoader>();
        services.AddSingleton<IBitmapGraphicsTargetProvider, SkiaBitmapGraphicsTargetProvider>();
        services.AddSingleton<IFontLoader>(SkiaFontLoader.Instance);
        services.AddSingleton<ITextMetrics, Skia_TextMetrics>();
        services.AddSingleton<IFileLoaderProvider, SkiaFileLoaderProvider>();
#else
        services.AddSingleton<IGraphicsBitmapLoader, GDIPlus_GraphicsBitmapLoader>();
        services.AddSingleton<IBitmapGraphicsTargetProvider, GDIPlus_BitmapGraphicsTargetProvider>();
        services.AddSingleton<IFontLoader>(GdiplusFontLoader.Instance);
        services.AddSingleton<ITextMetrics, GDIPlus_TextMetrics>();
        services.AddSingleton<IFileLoaderProvider, GdiPlus_FileLoaderProvider>();
#endif        
        services.AddSingleton<IPdfWriter, PdfWriter>();
        services.AddSingleton<IPdfLoadingStatus, PdfLoadingUI>();

        serviceProvider = services.BuildServiceProvider();
        Services.RegisterServiceProvider(serviceProvider);


        /* ========================================================================================================
         * ASSEMBLY RESOLUTION WORKAROUND FOR .NET FRAMEWORK 4.8 + .NET STANDARD 2.0
         * ========================================================================================================
         * * WHY THIS IS NECESSARY:
         * This project targets .NET Framework 4.8, but it consumes shared libraries targeting .NET Standard 2.0. 
         * Those shared libraries depend on the `netstandard2.0` contract of `Microsoft.Extensions.Logging.Abstractions`.
         * * However, when NuGet restores packages for this .NET 4.8 test project, its internal fallback rules decide 
         * that the `net462` implementation of the Logging package is a "closer match" for .NET 4.8 than the 
         * `netstandard2.0` version. It places the `net462` DLL in the bin folder. 
         * * At runtime, the .NET Standard library demands the exact assembly signature it was compiled against. 
         * The CLR sees the `net462` DLL, notices the manifest/version mismatch, and throws a `FileLoadException`.
         * * WHY APP.CONFIG DOESN'T WORK HERE:
         * Normally, an `<assemblyBinding>` redirect in `app.config` forces the runtime to accept the physical DLL. 
         * However, because this is a test project, the test runner (e.g., MSTest or NUnit via testhost.exe) spins 
         * up its own isolated AppDomain to execute the tests. This test host frequently ignores, mangles, or fails 
         * to load the local `app.config` binding redirects. 
         * * HOW THIS FIXES IT:
         * We wire up an event handler to `AppDomain.CurrentDomain.AssemblyResolve`. This event fires exactly when 
         * the .NET runtime throws its hands up and says "I can't find this exact assembly version." 
         * * When it asks for *any* version of `Microsoft.Extensions.Logging.Abstractions`, we intercept the request 
         * and hand it the assembly containing `typeof(ILogger)`. This forces the runtime to accept the `net462` 
         * DLL that NuGet already loaded into memory, satisfying the .NET Standard library's requirement and 
         * bypassing the strict manifest check.
         * * FUTURE CLEANUP:
         * This entire file/method is a legacy workaround for the .NET Framework 4.8 assembly binder. 
         * Once this test project and its dependencies are fully migrated to .NET 10, the modern CoreCLR 
         * handles transitive dependencies and roll-forwards natively. 
         * * -> THIS CODE CAN BE SAFELY DELETED ONCE THE TARGET FRAMEWORK IS UPGRADED TO .NET 10. <-
         * ======================================================================================================== */

        AppDomain.CurrentDomain.AssemblyResolve += ResolveLoggingAbstractions;

        // Initialize settings to a default state. This ensures that tests start with a clean slate and don't accidentally read/write real user settings.
        string userSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PurplePen_Tests", "UserSettings.json");
        if (File.Exists(userSettingsPath))
            File.Delete(userSettingsPath);
        UserSettings.Initialize(userSettingsPath);


        FontDesc.InitializeFonts();
    }

    private static Assembly ResolveLoggingAbstractions(object sender, ResolveEventArgs args)
    {
        // If the runtime is looking for the Logging Abstractions, force it to use the one we have
        if (args.Name.StartsWith("Microsoft.Extensions.Logging.Abstractions", StringComparison.OrdinalIgnoreCase)) {
            return typeof(Microsoft.Extensions.Logging.ILogger).Assembly;
        }
        return null;
    }

    [AssemblyCleanup]
    public static void Cleanup()
    {
        serviceProvider.Dispose();
        serviceProvider = null;
    }
}