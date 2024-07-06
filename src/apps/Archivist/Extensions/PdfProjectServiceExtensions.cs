using Images;
using Microsoft.Extensions.DependencyInjection;
using PdfProj;
using Pdfs;

namespace Archivist.Extensions;

public static class PdfProjectServiceExtensions
{
    public static IServiceCollection AddPdfProjectServices(this IServiceCollection services)
    {
        return services
            .AddTransient<IImageLoader, ImageLoader>()
            .AddTransient<IPdfLoader, PdfLoader>()
            .AddTransient<IPdfBuilder, PdfBuilder>();
    }
}
