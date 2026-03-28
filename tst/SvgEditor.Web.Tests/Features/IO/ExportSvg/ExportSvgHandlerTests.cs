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

    [TestMethod]
    public async Task Handle_InlineComment_IsPreservedInExport()
    {
        const string original = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
                <!-- layer: background -->
                <rect x="0" y="0" width="100" height="100" fill="white" />
            </svg>
            """;

        var imported = await ImportHandler.Handle(new ImportSvgCommand(original));
        var exported = await Handler.Handle(new ExportSvgCommand(imported));

        Assert.Contains("<!-- layer: background -->", exported);
    }

    [TestMethod]
    public async Task Handle_PrologComment_IsPreservedInExport()
    {
        const string original = """
            <?xml version="1.0" encoding="UTF-8"?>
            <!-- Copyright 2024 Example Corp. All rights reserved. -->
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
                <rect x="0" y="0" width="100" height="100" />
            </svg>
            """;

        var imported = await ImportHandler.Handle(new ImportSvgCommand(original));
        var exported = await Handler.Handle(new ExportSvgCommand(imported));

        Assert.Contains("Copyright 2024 Example Corp", exported);
    }

    [TestMethod]
    public async Task Handle_MultipleComments_AllPreservedInExport()
    {
        const string original = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200">
                <!-- first comment -->
                <rect x="0" y="0" width="50" height="50" />
                <!-- second comment -->
                <circle cx="100" cy="100" r="40" />
            </svg>
            """;

        var imported = await ImportHandler.Handle(new ImportSvgCommand(original));
        var exported = await Handler.Handle(new ExportSvgCommand(imported));

        Assert.Contains("first comment", exported);
        Assert.Contains("second comment", exported);
    }

    [TestMethod]
    public async Task Handle_Comments_DoNotAffectElementRoundtrip()
    {
        const string original = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200">
                <!-- this comment should not affect element parsing -->
                <rect x="5" y="10" width="30" height="40" fill="red" />
                <circle cx="100" cy="100" r="50" />
            </svg>
            """;

        var imported = await ImportHandler.Handle(new ImportSvgCommand(original));
        var exported = await Handler.Handle(new ExportSvgCommand(imported));
        var reimported = await ImportHandler.Handle(new ImportSvgCommand(exported));

        var svgElements = reimported.Elements.Where(e => e is not SvgComment).ToList();
        Assert.HasCount(2, svgElements);
        Assert.IsInstanceOfType<SvgRect>(svgElements[0]);
        Assert.IsInstanceOfType<SvgCircle>(svgElements[1]);
    }

    [TestMethod]
    public async Task Handle_ProducesIndentedXml()
    {
        var doc = new SvgDocument
        {
            Attributes = new Dictionary<string, string> { ["viewBox"] = "0 0 100 100" },
            Elements =
            [
                new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "0", ["y"] = "0", ["width"] = "100", ["height"] = "100" } }
            ]
        };

        var svg = await Handler.Handle(new ExportSvgCommand(doc));
        var lines = svg.Split('\n');

        // The child element must be indented relative to the root
        var rectLine = lines.FirstOrDefault(l => l.Contains("<rect"));
        Assert.IsNotNull(rectLine, "Expected a <rect> line in the output");
        Assert.IsTrue(rectLine.StartsWith("  ", StringComparison.Ordinal), "Child elements should be indented by 2 spaces");
    }
}
