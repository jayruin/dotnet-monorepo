using EpubProj;
using FileStorage;
using FileStorage.Filesystem;
using Images;
using ImgProj.Comparing;
using ImgProj.Covers;
using ImgProj.Deleting;
using ImgProj.Exporting;
using ImgProj.Importing;
using MediaTypes;
using Microsoft.Extensions.DependencyInjection;
using PdfProj;
using Pdfs;

namespace Archivist;

public static class Registrations
{
    public static IServiceCollection RegisterServices(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddTransient<IFileStorage, FilesystemFileStorage>()
            .AddEpubProject()
            .AddImgProject()
            .AddPdfProject();
    }

    private static IServiceCollection AddEpubProject(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMediaTypeFileExtensionsMapping>(MediaTypeFileExtensionsMapping.Default)
            .AddTransient<IEpubProjectLoader, EpubProjectLoader>();
    }

    private static IServiceCollection AddImgProject(this IServiceCollection services)
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

    private static IServiceCollection AddPdfProject(this IServiceCollection services)
    {
        return services
            .AddTransient<IImageLoader, ImageLoader>()
            .AddTransient<IPdfLoader, PdfLoader>()
            .AddTransient<IPdfBuilder, PdfBuilder>();
    }
}
