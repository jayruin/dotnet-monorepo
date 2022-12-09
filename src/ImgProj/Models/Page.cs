using FileStorage;
using System;
using System.IO;

namespace ImgProj.Models;

public sealed class Page
{
    private readonly IFile _file;

    public string Extension { get; init; }

    public string Version { get; init; }

    public Page(IFile file)
    {
        _file = file;
        Extension = file.Extension;
        Version = file.Stem;
    }

    public Stream OpenRead() => _file.OpenRead();

    public static string PadPageNumber(int pageNumber, int pageCount)
    {
        int digits = Convert.ToInt32(Math.Floor(Math.Log(pageCount, 10))) + 1;
        return pageNumber.ToString().PadLeft(digits, '0');
    }
}