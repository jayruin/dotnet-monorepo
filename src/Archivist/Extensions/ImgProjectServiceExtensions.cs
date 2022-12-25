using Images;
using ImgProj.Comparing;
using ImgProj.Covers;
using ImgProj.Deleting;
using ImgProj.Exporting;
using ImgProj.Importing;
using Microsoft.Extensions.DependencyInjection;

namespace Archivist.Extensions;

public static class ImgProjectServiceExtensions
{
    public static IServiceCollection AddImgProjectServices(this IServiceCollection services)
    {
        return services
            .AddTransient<IImageLoader, ImageLoader>()
            .AddTransient<ICoverGenerator, CoverGenerator>()
            .AddTransient<IPageComparer, PageComparer>()
            .AddTransient<IPageDeleter, PageDeleter>()
            .AddTransient<IPageImporter, PageImporter>()
            .AddTransient<IExporter, CbzExporter>()
            .AddTransient<IExporter, Epub3Exporter>()
            .AddTransient<IExporter, PdfExporter>();
    }
}
