namespace Epub;

public static class EpubMimetypes
{
    public static class Application
    {
        public const string EpubZip = "application/epub+zip";

        public const string Ncx = "application/x-dtbncx+xml";

        public const string OctetStream = "application/octet-stream";

        public const string OebpsPackageXml = "application/oebps-package+xml";

        public const string XhtmlXml = "application/xhtml+xml";
    }

    public static class Font
    {
        public const string Otf = "font/otf";

        public const string Ttf = "font/ttf";

        public const string Woff = "font/woff";

        public const string Woff2 = "font/woff2";
    }

    public static class Image
    {
        public const string Gif = "image/gif";

        public const string Jpeg = "image/jpeg";

        public const string Png = "image/png";

        public const string SvgXml = "image/svg+xml";
    }

    public static class Text
    {
        public const string Css = "text/css";

        public const string Javascript = "text/javascript";
    }
}