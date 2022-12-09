using ImgProj.Services.Covers;
using ImgProj.Services.Deleters;
using ImgProj.Services.Exporters;
using ImgProj.Services.ImageGridGenerators;
using ImgProj.Services.ImageResizers;
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
            .AddTransient<IImgProjectLoader, ImgProjectLoader>()
            .AddTransient<IImageResizer, ImageResizer>()
            .AddTransient<IImageGridGenerator, ImageGridGenerator>()
            .AddTransient<ICoverGenerator, CoverGenerator>()
            .AddTransient<IPageComparer, PageComparer>()
            .AddTransient<IPageDeleter, PageDeleter>()
            .AddTransient<IPageImporter, PageImporter>()
            .AddTransient<IExporter, CbzExporter>()
            .AddTransient<IExporter, Epub3Exporter>()
            .AddTransient<IExporter, PdfExporter>();
    }
}
