using FileStorage;
using Images;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Navigation;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfEdit;

public static class Builder
{
    public static void Build(Recipe recipe, IFile outputFile, IDirectory? trash, IEnumerable<int> groupNumbers)
    {
        Encoding encoding = new UTF8Encoding();

        IImmutableDictionary<string, IFile> pdfs = Loader.LoadPdfs(recipe.Pdfs);
        IImmutableDictionary<string, string> passwords = Loader.LoadMapping(recipe.Passwords);
        IImmutableDictionary<string, string> titles = Loader.LoadMapping(recipe.Titles);
        IImmutableDictionary<string, ImmutableArray<TocNode>> tocs = Loader.LoadTocs(recipe.Tocs);

        if (trash is not null)
        {
            if (trash.Exists) trash.Delete();
            trash.Create();
        }

        ImmutableArray<int> groupNumbersArray = groupNumbers.ToImmutableArray();
        if (groupNumbersArray.Length == 0)
        {
            groupNumbersArray = Enumerable.Range(1, recipe.Groups.Length).ToImmutableArray();
        }
        ImmutableArray<Group> groups = groupNumbersArray.Select(n => recipe.Groups[n - 1]).ToImmutableArray();

        using Stream outputStream = outputFile.OpenWrite();
        using PdfWriter outputPdfWriter = new(outputStream);
        using PdfDocument outputPdfDocument = new(outputPdfWriter);

        PdfOutline currentOutline;
        int pageOffset = 0;
        AddImageGridPage(outputPdfDocument, groups.Select(g => g.Cover));
        pageOffset += 1;
        foreach (Group group in groups)
        {
            PdfOutline groupOutline = outputPdfDocument.GetOutlines(true);
            if (group.Text is not null)
            {
                AddImageAsPage(outputPdfDocument, group.Cover);
                TocNode groupNode = new()
                {
                    Text = group.Text,
                    Page = 1,
                    Children = ImmutableArray<TocNode>.Empty,
                };
                groupOutline = AddToOutline(groupNode, pageOffset, outputPdfDocument, groupOutline);
                pageOffset += 1;
            }
            foreach (string id in group.Content)
            {
                currentOutline = groupOutline;
                IFile pdfFile = pdfs[id];
                using Stream pdfStream = pdfFile.OpenRead();
                ReaderProperties readerProperties = new();
                if (passwords.ContainsKey(id))
                {
                    readerProperties = readerProperties.SetPassword(encoding.GetBytes(passwords[id]));
                }
                using PdfReader pdfReader = new(pdfStream, readerProperties);
                using PdfDocument pdfDocument = new(pdfReader);
                ClearOutline(pdfDocument);
                List<(float, float)> sizes = recipe.Filters
                    .Where(f => f.Ids.Contains(id))
                    .Select(f => (f.Width, f.Height))
                    .ToList();
                DeleteImages(pdfDocument, sizes, trash, id);
                pdfDocument.CopyPagesTo(1, pdfDocument.GetNumberOfPages(), outputPdfDocument);
                if (titles.ContainsKey(id))
                {
                    TocNode titleNode = new()
                    {
                        Text = titles[id],
                        Page = 1,
                        Children = ImmutableArray<TocNode>.Empty,
                    };
                    currentOutline = AddToOutline(titleNode, pageOffset, outputPdfDocument, currentOutline);
                }
                if (tocs.ContainsKey(id))
                {
                    foreach (TocNode tocNode in tocs[id])
                    {
                        AddToOutline(tocNode, pageOffset, outputPdfDocument, currentOutline);
                    }
                }
                pageOffset = outputPdfDocument.GetNumberOfPages();
            }
        }
    }

    private static void ClearOutline(PdfDocument pdfDocument)
    {
        pdfDocument.GetOutlines(true)?.RemoveOutline();
        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
        {
            IList<PdfOutline>? pageOutlines = pdfDocument.GetPage(i).GetOutlines(true);
            if (pageOutlines is null) continue;
            foreach (PdfOutline pageOutline in pageOutlines)
            {
                pageOutline.RemoveOutline();
            }
        }
    }

    private static PdfOutline AddToOutline(TocNode tocNode, int pageOffset, PdfDocument pdfDocument, PdfOutline pdfOutline)
    {
        PdfOutline newPdfOutline = pdfOutline.AddOutline(tocNode.Text);
        PdfPage pdfPage = pdfDocument.GetPage(tocNode.Page + pageOffset);
        PdfDestination pdfDestination = PdfExplicitDestination.CreateFit(pdfPage);
        newPdfOutline.AddDestination(pdfDestination);
        foreach (TocNode child in tocNode.Children)
        {
            AddToOutline(child, pageOffset, pdfDocument, newPdfOutline);
        }
        return newPdfOutline;
    }

    private static void AddImageAsPage(PdfDocument pdfDocument, IImage image)
    {
        using MemoryStream memoryStream = new();
        image.SaveTo(memoryStream, ImageFormat.Jpeg);
        ImageData imageData = ImageDataFactory.Create(memoryStream.ToArray());
        PageSize pageSize = new(imageData.GetWidth(), imageData.GetHeight());
        PdfPage pdfPage = pdfDocument.AddNewPage(pageSize);
        PdfCanvas pdfCanvas = new(pdfPage);
        pdfCanvas.AddImageAt(imageData, 0, 0, false);
    }

    private static void AddImageAsPage(PdfDocument pdfDocument, IFile imageFile)
    {
        IImageLoader imageLoader = new ImageLoader();
        using Stream imageStream = imageFile.OpenRead();
        IImage image = imageLoader.LoadImage(imageStream);
        AddImageAsPage(pdfDocument, image);
    }

    private static void AddImageGridPage(PdfDocument pdfDocument, IEnumerable<IFile> imageFiles)
    {
        IImageLoader imageLoader = new ImageLoader();
        List<Stream> imageStreams = imageFiles.Select(f => f.OpenRead()).ToList();
        IImage imageGrid = imageLoader.LoadImagesToGrid(imageStreams);
        foreach (Stream imageStream in imageStreams)
        {
            imageStream.Dispose();
        }
        AddImageAsPage(pdfDocument, imageGrid);
    }

    private static void DeleteImages(PdfDocument pdfDocument, IEnumerable<(float, float)> sizes, IDirectory? trash, string id)
    {
        ImageDeleter deleter = new(sizes, trash, id);
        PdfCanvasProcessor processor = new(deleter);
        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
        {
            processor.ProcessPageContent(pdfDocument.GetPage(i));
        }
    }
}
