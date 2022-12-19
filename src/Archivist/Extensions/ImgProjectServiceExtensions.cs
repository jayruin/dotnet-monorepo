using Images;
using ImgProj.Services.Covers;
using ImgProj.Services.Deleters;
using ImgProj.Services.Exporters;
using ImgProj.Services.Importers;
using ImgProj.Services.Loaders;
using ImgProj.Services.PageComparers;
using Microsoft.Extensions.DependencyInjection;

namespace Archivist.Extensions;

public static class ImgProjectServiceExtensions
{
    public static IServiceCollection AddImgProjectServices(this IServiceCollection services)
    {
        return services
            .AddTransient<IImageLoader, ImageLoader>()
            .AddTransient<IImgProjectLoader, ImgProjectLoader>()
            .AddTransient<ICoverGenerator, CoverGenerator>()
            .AddTransient<IPageComparer, PageComparer>()
            .AddTransient<IPageDeleter, PageDeleter>()
            .AddTransient<IPageImporter, PageImporter>()
            .AddTransient<IExporter, CbzExporter>()
            .AddTransient<IExporter, Epub3Exporter>()
            .AddTransient<IExporter, PdfExporter>();
    }
}
