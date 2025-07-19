using FileStorage;
using FileStorage.Filesystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using umm.Storages.Blob;
using umm.Storages.Metadata;
using umm.Storages.Tags;

namespace umm.Storages;

public static class MediaStorageServiceCollectionExtensions
{
    public static IServiceCollection AddMediaStorageServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        string storagesPrefix = "storages";
        serviceCollection.AddTransient<IMediaStorageCluster, MediaStorageCluster>(_ =>
        {
            var cluster = new MediaStorageCluster();
            IConfiguration storagesConfiguration = configuration.GetSection(storagesPrefix);
            FilesystemFileStorage filesystemFileStorage = new();
            foreach (IConfiguration storageConfiguration in storagesConfiguration.GetChildren())
            {
                string? storageType = storageConfiguration["type"];
                if (string.IsNullOrWhiteSpace(storageType)) continue;
                if (storageType.Equals("filesystem", StringComparison.OrdinalIgnoreCase))
                {
                    var options = storageConfiguration.Get<FilesystemOptions>() ?? new();
                    if (!options.Metadata && !options.Blob) continue;
                    List<string> vendorIds = options.Supports ?? [];
                    if (vendorIds.Count == 0) continue;
                    if (string.IsNullOrWhiteSpace(options?.Path)) continue;
                    IDirectory baseDirectory = filesystemFileStorage.GetDirectory(options.Path);
                    if (!baseDirectory.Exists()) continue;
                    IBlobStorage blobStorage = new FileBlobStorage(vendorIds, baseDirectory);
                    IMetadataStorage metadataStorage = new JsonBlobMetadataStorage(blobStorage);
                    ITagsStorage tagsStorage = new TxtBlobTagsStorage(blobStorage);
                    if (options.Metadata)
                    {
                        cluster.Add(metadataStorage);
                    }
                    if (options.Blob)
                    {
                        cluster.Add(blobStorage);
                    }
                    if (options.Tags)
                    {
                        cluster.Add(tagsStorage);
                    }
                }
            }
            return cluster;
        });
        serviceCollection.AddTransient<IMetadataStorage, CompositeMetadataStorage>(sp =>
            new(sp.GetRequiredService<IMediaStorageCluster>().MediaStorages.OfType<IMetadataStorage>().ToList(),
                sp.GetRequiredService<ILogger<CompositeMetadataStorage>>()));
        serviceCollection.AddTransient<IBlobStorage, CompositeBlobStorage>(sp =>
            new(sp.GetRequiredService<IMediaStorageCluster>().MediaStorages.OfType<IBlobStorage>().ToList()));
        serviceCollection.AddTransient<ITagsStorage, CompositeTagsStorage>(sp =>
            new(sp.GetRequiredService<IMediaStorageCluster>().MediaStorages.OfType<ITagsStorage>().ToList(),
                sp.GetRequiredService<ILogger<CompositeTagsStorage>>()));
        return serviceCollection;
    }

    internal interface IMediaStorageCluster
    {
        IReadOnlyCollection<IMediaStorage> MediaStorages { get; }
        void Add(IMediaStorage mediaStorage);
    }

    internal sealed class MediaStorageCluster : IMediaStorageCluster
    {
        private readonly List<IMediaStorage> _mediaStorages;

        public MediaStorageCluster()
        {
            _mediaStorages = [];
        }

        public IReadOnlyCollection<IMediaStorage> MediaStorages => _mediaStorages;

        public void Add(IMediaStorage mediaStorage)
        {
            _mediaStorages.Add(mediaStorage);
        }
    }

    internal sealed class FilesystemOptions
    {
        public string? Path { get; set; }
        public bool Metadata { get; set; }
        public bool Blob { get; set; }
        public bool Tags { get; set; }
        public List<string>? Supports { get; set; }
    }
}
