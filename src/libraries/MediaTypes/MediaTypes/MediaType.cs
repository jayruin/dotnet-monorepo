namespace MediaTypes;

public static class MediaType
{
    public static class Application
    {
        public const string Epub_Zip = "application/epub+zip";
        public const string OctetStream = "application/octet-stream";
        public const string OebpsPackage_Xml = "application/oebps-package+xml";
        public const string Pdf = "application/pdf";
        public const string XDtbncx_Xml = "application/x-dtbncx+xml";
        public const string Xhtml_Xml = "application/xhtml+xml";
    }

    public static class Audio
    {
        public const string Mpeg = "audio/mpeg";
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
        public const string Svg_Xml = "image/svg+xml";
        public const string Webp = "image/webp";
    }

    public static class Text
    {
        public const string Css = "text/css";
        public const string Html = "text/html";
        public const string Javascript = "text/javascript";
        public const string Markdown = "text/markdown";
        public const string Plain = "text/plain";
    }
}
