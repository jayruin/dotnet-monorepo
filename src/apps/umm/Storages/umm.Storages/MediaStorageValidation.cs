using System;

namespace umm.Storages;

internal static class MediaStorageValidation
{
    public static void ThrowIfNotSupported(IMediaStorage mediaStorage, string vendorId)
    {
        if (mediaStorage.Supports(vendorId)) return;
        throw new ArgumentException($"{vendorId} is not supported.", nameof(vendorId));
    }
}
