using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.IO.ExportSvg;
using SvgEditor.Web.Features.IO.ImportSvg;

namespace SvgEditor.Web.Tests.Features.IO.ExportSvg;

[TestClass]
public sealed class ExportSvgHandlerTests
{
    private static readonly ExportSvgHandler Handler = new();
    private static readonly ImportSvgHandler ImportHandler = new();

    [TestMethod]
    public async Task Handle_ProducesWellFormedXml()
    {
        var doc = new SvgDocument
        {
            ViewBox = "0 0 100 100",
            Width = 100,
            Height = 100,
            Attributes = new Dictionary<string, string> { ["viewBox"] = "0 0 100 100", ["width"] = "100", ["height"] = "100" },
            Elements = [new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "10", ["width"] = "50", ["height"] = "50" } }]
        };

        var svg = await Handler.Handle(new ExportSvgCommand(doc));

        var xDoc = System.Xml.Linq.XDocument.Parse(svg);
        Assert.IsNotNull(xDoc.Root);
        Assert.AreEqual("svg", xDoc.Root.Name.LocalName);
    }

    [TestMethod]
    public async Task Handle_RoundTrip_PreservesElements()
    {
        const string original = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" width="200" height="200">
                <rect x="10" y="20" width="30" height="40" fill="blue" />
                <circle cx="100" cy="100" r="50" />
            </svg>
            """;

        var imported = await ImportHandler.Handle(new ImportSvgCommand(original));
        var exported = await Handler.Handle(new ExportSvgCommand(imported));
        var reimported = await ImportHandler.Handle(new ImportSvgCommand(exported));

        Assert.HasCount(2, reimported.Elements);
        Assert.IsInstanceOfType<SvgRect>(reimported.Elements[0]);
        Assert.IsInstanceOfType<SvgCircle>(reimported.Elements[1]);
    }

    [TestMethod]
    public async Task Handle_PreservesAttributes()
    {
        var doc = new SvgDocument
        {
            Attributes = new Dictionary<string, string> { ["viewBox"] = "0 0 50 50" },
            Elements = [new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "5", ["y"] = "5", ["width"] = "10", ["height"] = "10", ["fill"] = "green" } }]
        };

        var svg = await Handler.Handle(new ExportSvgCommand(doc));

        Assert.Contains("fill=\"green\"", svg);
    }

    [TestMethod]
    public async Task Handle_ExportsGroupWithChildren()
    {
        var doc = new SvgDocument
        {
            Attributes = [],
            Elements =
            [
                new SvgGroup
                {
                    Attributes = [],
                    Children =
                    [
                        new SvgCircle { Attributes = new Dictionary<string, string> { ["cx"] = "10", ["cy"] = "10", ["r"] = "5" } }
                    ]
                }
            ]
        };

        var svg = await Handler.Handle(new ExportSvgCommand(doc));

        Assert.Contains("<g", svg);
        Assert.Contains("<circle", svg);
    }
}
