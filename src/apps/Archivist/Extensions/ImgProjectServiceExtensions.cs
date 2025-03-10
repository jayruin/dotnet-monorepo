using Images;
using ImgProj.Comparing;
using ImgProj.Covers;
using ImgProj.Deleting;
using ImgProj.Exporting;
using ImgProj.Importing;
using MediaTypes;
using Microsoft.Extensions.DependencyInjection;
using Pdfs;

namespace Archivist.Extensions;

public static class ImgProjectServiceExtensions
{
    public static IServiceCollection AddImgProjectServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMediaTypeFileExtensionsMapping>(MediaTypeFileExtensionsMapping.Default)
            .AddTransient<IImageLoader, ImageLoader>()
            .AddTransient<IPdfLoader, PdfLoader>()
            .AddTransient<ICoverGenerator, CoverGenerator>()
            .AddTransient<IPageComparer, PageComparer>()
            .AddTransient<IPageDeleter, PageDeleter>()
            .AddTransient<IPageImporter, PageImporter>()
            .AddTransient<IExporter, CbzExporter>()
            .AddTransient<IExporter, Epub3Exporter>()
            .AddTransient<IExporter, PdfExporter>();
    }
}
