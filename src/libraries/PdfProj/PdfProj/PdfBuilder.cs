using FileStorage;
using Images;
using Pdfs;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Utils;

namespace PdfProj;

public sealed class PdfBuilder : IPdfBuilder
{
    private readonly IPdfLoader _pdfLoader;

    private readonly IImageLoader _imageLoader;

    public PdfBuilder(IPdfLoader pdfLoader, IImageLoader imageLoader)
    {
        _pdfLoader = pdfLoader;
        _imageLoader = imageLoader;
    }

    public async Task BuildAsync(IFile targetJson, IFile output, IDirectory? trash)
    {
        if (trash is not null)
        {
            if (trash.Exists()) trash.Delete();
            trash.Create();
        }
        await using Stream outputStream = output.OpenWrite();
        using IPdfWritableDocument outputPdf = _pdfLoader.OpenWrite(outputStream);
        BuildTarget target = await LoadBuildTargetAsync(targetJson);
        SetPdfTitle(outputPdf, target);
        if (target is RecipeTarget recipeTarget && string.IsNullOrWhiteSpace(recipeTarget.Title))
        {
            await AddCoverImageToPdfAsync(outputPdf, target);
        }
        List<PdfOutlineItem> outline = [];
        await AddToPdfAsync(outputPdf, target, outline, trash);
        foreach (PdfOutlineItem outlineItem in outline)
        {
            outputPdf.AddOutlineItem(outlineItem);
        }
    }

    private async Task AddToPdfAsync(IPdfWritableDocument outputPdf, BuildTarget target, List<PdfOutlineItem> currentOutline, IDirectory? trash)
    {
        if (target is MetadataTarget metadataTarget)
        {
            int offset = outputPdf.NumberOfPages;
            await using Stream pdfStream = metadataTarget.PdfFile.OpenRead();
            PdfCopyPagesResult copyPagesResult = outputPdf.CopyPages(pdfStream, metadataTarget.Password, metadataTarget.Filters);
            if (trash is not null)
            {
                int counter = 1;
                foreach (byte[] deletedImageData in copyPagesResult.DeletedImages)
                {
                    await using MemoryStream memoryStream = new(deletedImageData, false);
                    using IImage image = await _imageLoader.LoadImageAsync(memoryStream);
                    await using Stream trashStream = trash.GetFile($"{metadataTarget.PdfFile.Name}-{counter}.jpg").OpenWrite();
                    await image.SaveToAsync(trashStream, ImageFormat.Jpeg);
                    counter += 1;
                }
            }
            ImmutableArray<PdfOutlineItem> shiftedOutline = metadataTarget.Outline.Select(o => o.Shift(offset)).ToImmutableArray();
            if (string.IsNullOrWhiteSpace(metadataTarget.Title))
            {
                currentOutline.AddRange(shiftedOutline);
            }
            else
            {
                PdfOutlineItem outlineItem = new()
                {
                    Text = metadataTarget.Title,
                    Page = offset + 1,
                    Children = shiftedOutline,
                };
                currentOutline.Add(outlineItem);
            }
        }
        else if (target is RecipeTarget recipeTarget)
        {
            if (string.IsNullOrWhiteSpace(recipeTarget.Title))
            {
                foreach (BuildTarget subTarget in recipeTarget.Targets)
                {
                    await AddToPdfAsync(outputPdf, subTarget, currentOutline, trash);
                }
            }
            else
            {
                bool hasCover = await AddCoverImageToPdfAsync(outputPdf, recipeTarget);
                int offset = outputPdf.NumberOfPages;
                List<PdfOutlineItem> children = [];
                foreach (BuildTarget subTarget in recipeTarget.Targets)
                {
                    await AddToPdfAsync(outputPdf, subTarget, children, trash);
                }
                PdfOutlineItem outlineItem = new()
                {
                    Text = recipeTarget.Title,
                    Page = offset + (hasCover ? 0 : 1),
                    Children = [.. children],
                };
                currentOutline.Add(outlineItem);
            }
        }
    }

    private static void SetPdfTitle(IPdfWritableDocument pdf, BuildTarget target)
    {
        List<string> titles = [];
        Stack<BuildTarget> stack = [];
        stack.Push(target);
        while (stack.TryPop(out BuildTarget? currentTarget))
        {
            if (!string.IsNullOrWhiteSpace(currentTarget.Title))
            {
                titles.Add(currentTarget.Title);
            }
            else
            {
                foreach (BuildTarget subTarget in currentTarget.Targets)
                {
                    stack.Push(subTarget);
                }
            }
        }
        string title = titles.LongestCommonPrefix().Trim();
        if (!string.IsNullOrWhiteSpace(title))
        {
            pdf.SetTitle(title);
        }
    }

    private async Task<bool> AddCoverImageToPdfAsync(IPdfWritableDocument pdf, BuildTarget target)
    {
        if (target.Covers.Length == 1)
        {
            await using Stream coverStream = target.Covers[0].OpenRead();
            using IImage coverImage = await _imageLoader.LoadImageAsync(coverStream);
            await AddImagePageToPdfAsync(pdf, coverImage);
            return true;
        }
        else if (target.Covers.Length > 1)
        {
            List<Stream> coverStreams = target.Covers.Select(c => c.OpenRead()).ToList();
            using IImage coverGrid = await _imageLoader.LoadImagesToGridAsync(coverStreams);
            await AddImagePageToPdfAsync(pdf, coverGrid);
            foreach (Stream coverStream in coverStreams)
            {
                await coverStream.DisposeAsync();
            }
            return true;
        }
        return false;
    }

    private static async Task AddImagePageToPdfAsync(IPdfWritableDocument pdf, IImage image)
    {
        await using MemoryStream memoryStream = new();
        await image.SaveToAsync(memoryStream, ImageFormat.Jpeg);
        pdf.AddImagePage(memoryStream.ToArray());
    }

    private static async Task<BuildTarget> LoadBuildTargetAsync(IFile jsonFile)
    {
        await using Stream stream = jsonFile.OpenRead();
        if (jsonFile.Name.EndsWith(".metadata.json"))
        {
            MetadataJson metadata = await JsonSerializer.DeserializeAsync(stream, JsonContext.Default.MetadataJson) ?? throw new JsonException();
            IDirectory parentDirectory = jsonFile.GetParentDirectory() ?? throw new FileStorageException();
            IFile? cover = string.IsNullOrWhiteSpace(metadata.Cover) ? null : parentDirectory.GetFile(metadata.Cover);
            IFile pdfFile = parentDirectory.GetFile(metadata.Path);
            return new MetadataTarget(jsonFile, cover, pdfFile, metadata.Password, metadata.Outline, metadata.Title, metadata.Filters);
        }
        else if (jsonFile.Name.EndsWith(".recipe.json"))
        {
            RecipeJson recipe = await JsonSerializer.DeserializeAsync(stream, JsonContext.Default.RecipeJson) ?? throw new JsonException();
            IDirectory parentDirectory = jsonFile.GetParentDirectory() ?? throw new FileStorageException();
            IFile? cover = string.IsNullOrWhiteSpace(recipe.Cover) ? null : parentDirectory.GetFile(recipe.Cover);
            ImmutableArray<BuildTarget>.Builder targetsBuilder = ImmutableArray.CreateBuilder<BuildTarget>();
            foreach (string entry in recipe.Entries)
            {
                targetsBuilder.Add(await LoadBuildTargetAsync(parentDirectory.GetFile(entry)));
            }
            return new RecipeTarget(jsonFile, cover, recipe.Title, targetsBuilder.ToImmutable());
        }
        throw new InvalidOperationException();
    }

    private abstract class BuildTarget
    {
        public abstract IFile JsonFile { get; }

        public abstract string? Title { get; }

        public abstract ImmutableArray<IFile> Covers { get; }

        public abstract ImmutableArray<BuildTarget> Targets { get; }
    }

    private sealed class MetadataTarget : BuildTarget
    {
        public override IFile JsonFile { get; }

        public override string? Title { get; }

        public override ImmutableArray<IFile> Covers { get; }

        public override ImmutableArray<BuildTarget> Targets => [];

        public IFile PdfFile { get; }

        public string? Password { get; }
        public ImmutableArray<PdfOutlineItem> Outline { get; }

        public ImmutableArray<PdfImageFilter> Filters { get; }

        public MetadataTarget(IFile jsonFile, IFile? cover,
            IFile pdfFile,
            string? password,
            ImmutableArray<PdfOutlineItem> outline,
            string? title,
            ImmutableArray<PdfImageFilter> filters)
        {
            JsonFile = jsonFile;
            Covers = cover is null ? [] : [cover];
            PdfFile = pdfFile;
            Password = password;
            Outline = outline;
            Title = title;
            Filters = filters;
        }
    }

    private sealed class RecipeTarget : BuildTarget
    {
        public override IFile JsonFile { get; }

        public override string? Title { get; }

        public override ImmutableArray<IFile> Covers { get; }

        public override ImmutableArray<BuildTarget> Targets { get; }

        public RecipeTarget(IFile jsonFile, IFile? cover,
            string? title,
            ImmutableArray<BuildTarget> targets)
        {
            JsonFile = jsonFile;
            Covers = cover is null
                ? targets.SelectMany(e => e.Covers).ToImmutableArray()
                : [cover];
            Title = title;
            Targets = targets;
        }
    }
}
