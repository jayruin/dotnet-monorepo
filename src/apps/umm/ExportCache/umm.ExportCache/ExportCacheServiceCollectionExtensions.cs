using FileStorage.Filesystem;
using MediaTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace umm.ExportCache;

public static class ExportCacheServiceCollectionExtensions
{
    public static IServiceCollection AddExportCache(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        string exportCachePrefix = "exportcache";
        IConfiguration exportCacheConfiguration = configuration.GetSection(exportCachePrefix);
        string? exportCacheType = exportCacheConfiguration["type"];
        if (string.IsNullOrWhiteSpace(exportCacheType)) return serviceCollection;
        if (exportCacheType.Equals("filesystem", StringComparison.OrdinalIgnoreCase))
        {
            var options = exportCacheConfiguration.Get<FilesystemExportCacheOptions>();
            string? path = options?.Path;
            bool handlesFiles = options?.Files ?? false;
            bool handlesDirectories = options?.Directories ?? false;
            List<string>? mediaTypes = options?.MediaTypes;
            Dictionary<string, FilesystemExportCacheVendorOverrideOptions>? vendorOverrides = options?.VendorOverrides;
            if (!string.IsNullOrWhiteSpace(path) && mediaTypes is not null && (handlesFiles || handlesDirectories))
            {
                serviceCollection.AddTransient<IExportCache, FilestorageExportCache>(sp => new(
                    sp.GetRequiredService<IMediaTypeFileExtensionsMapping>(),
                    new()
                    {
                        RootDirectory = new FilesystemFileStorage().GetDirectory(path),
                        HandleFiles = handlesFiles,
                        HandleDirectories = handlesDirectories,
                        MediaTypes = [.. mediaTypes],
                        VendorOverrides = (vendorOverrides ?? [])
                            .ToFrozenDictionary(
                                kvp => kvp.Key,
                                kvp => new FilestorageExportCacheVendorOverrideOptions()
                                {
                                    MediaTypes = (kvp.Value.MediaTypes ?? []).ToFrozenSet(),
                                }),
                    }));
            }
        }
        return serviceCollection;
    }

    internal sealed class FilesystemExportCacheOptions
    {
        public string? Path { get; set; }
        public bool Files { get; set; }
        public bool Directories { get; set; }
        public List<string>? MediaTypes { get; set; }
        public Dictionary<string, FilesystemExportCacheVendorOverrideOptions>? VendorOverrides { get; set; }
    }

    internal sealed class FilesystemExportCacheVendorOverrideOptions
    {
        public List<string>? MediaTypes { get; set; }
    }
}
