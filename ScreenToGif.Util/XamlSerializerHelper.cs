using System.IO;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

namespace ScreenToGif.Util;

public static class XamlSerializerHelper
{
    public static async Task<string> ToXamlStringAsync(this Brush brush, bool indent = false)
    {
        if (brush == null)
            return null;

        var settings = new XmlWriterSettings
        {
            Async = true,
            Indent = indent,
            IndentChars = "\t",
            OmitXmlDeclaration = true,
            CheckCharacters = true,
            CloseOutput = true,
            ConformanceLevel = ConformanceLevel.Fragment,
            Encoding = Encoding.UTF8
        };

        await using var stream = new StringWriter();
        await using var writer = XmlWriter.Create(stream, settings);
        XamlWriter.Save(brush, writer);

        return stream.ToString();
    }

    //ToBrush
}