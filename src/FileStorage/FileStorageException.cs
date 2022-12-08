using System;

namespace FileStorage;

public class FileStorageException : Exception
{
    public FileStorageException() : base()
    {
    }

    public FileStorageException(Exception innerException) : base(null, innerException)
    {
    }
}
