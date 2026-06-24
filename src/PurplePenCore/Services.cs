using Microsoft.Extensions.DependencyInjection;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace PurplePen
{
    // Extreme rudimentary way of providing services.
    public static class Services
    {
        private static IServiceProvider serviceProvider;

        public static void RegisterServiceProvider(IServiceProvider serviceProvider)
        {
            Services.serviceProvider = serviceProvider;
        }

        public static IServiceProvider ServiceProvider => serviceProvider;
        public static IGraphicsBitmapLoader BitmapLoader => serviceProvider.GetRequiredService<IGraphicsBitmapLoader>();
        public static IBitmapGraphicsTargetProvider BitmapGraphicsTargetProvider => serviceProvider.GetRequiredService<IBitmapGraphicsTargetProvider>();
        public static IFontLoader FontLoader => serviceProvider.GetRequiredService<IFontLoader>();
        public static ITextMetrics TextMetricsProvider => serviceProvider.GetRequiredService<ITextMetrics>();
        public static IFileLoaderProvider FileLoaderProvider => serviceProvider.GetRequiredService<IFileLoaderProvider>();
        public static IPdfLoadingStatus PdfLoadingUI => serviceProvider.GetRequiredService<IPdfLoadingStatus>();
        public static IPdfWriter PdfWriter => serviceProvider.GetRequiredService<IPdfWriter>();
        public static IDialogService DialogService => serviceProvider.GetRequiredService<IDialogService>();
        public static IUILanguage UILanguage => serviceProvider.GetRequiredService<IUILanguage>();
        public static IWebsiteLauncher WebsiteLauncher => serviceProvider.GetRequiredService<IWebsiteLauncher>();
    }

}
