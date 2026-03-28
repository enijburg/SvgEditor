using SvgEditor.Web.Features.IO.ImportSvg;

namespace SvgEditor.Web.Tests.Features.IO.ImportSvg;

[TestClass]
public sealed class ImportSvgHandlerTests
{
    private static readonly ImportSvgHandler Handler = new();

    [TestMethod]
    public async Task Handle_ParsesRect()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
                <rect x="10" y="20" width="30" height="40" fill="red" />
            </svg>
            """;

        var doc = await Handler.Handle(new ImportSvgCommand(svg));

        Assert.HasCount(1, doc.Elements);
        var rect = doc.Elements[0] as SvgEditor.Web.Features.Canvas.Models.SvgRect;
        Assert.IsNotNull(rect);
        Assert.AreEqual(10, rect.X);
        Assert.AreEqual(20, rect.Y);
        Assert.AreEqual(30, rect.Width);
        Assert.AreEqual(40, rect.Height);
        Assert.AreEqual("red", rect.Attributes["fill"]);
    }

    [TestMethod]
    public async Task Handle_ParsesCircle()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg">
                <circle cx="50" cy="60" r="25" />
            </svg>
            """;

        var doc = await Handler.Handle(new ImportSvgCommand(svg));

        var circle = doc.Elements[0] as SvgEditor.Web.Features.Canvas.Models.SvgCircle;
        Assert.IsNotNull(circle);
        Assert.AreEqual(50, circle.Cx);
        Assert.AreEqual(60, circle.Cy);
        Assert.AreEqual(25, circle.R);
    }

    [TestMethod]
    public async Task Handle_ParsesPath()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg">
                <path d="M 0 0 L 100 100" stroke="blue" />
            </svg>
            """;

        var doc = await Handler.Handle(new ImportSvgCommand(svg));

        var path = doc.Elements[0] as SvgEditor.Web.Features.Canvas.Models.SvgPath;
        Assert.IsNotNull(path);
        Assert.AreEqual("M 0 0 L 100 100", path.D);
    }

    [TestMethod]
    public async Task Handle_ParsesNestedGroups()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg">
                <g id="outer">
                    <rect x="5" y="5" width="10" height="10" />
                    <circle cx="20" cy="20" r="5" />
                </g>
            </svg>
            """;

        var doc = await Handler.Handle(new ImportSvgCommand(svg));

        Assert.HasCount(1, doc.Elements);
        var group = doc.Elements[0] as SvgEditor.Web.Features.Canvas.Models.SvgGroup;
        Assert.IsNotNull(group);
        Assert.HasCount(2, group.Children);
    }

    [TestMethod]
    public async Task Handle_PreservesViewBox()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 800 600" width="800" height="600">
            </svg>
            """;

        var doc = await Handler.Handle(new ImportSvgCommand(svg));

        Assert.AreEqual("0 0 800 600", doc.ViewBox);
        Assert.AreEqual(800, doc.Width);
        Assert.AreEqual(600, doc.Height);
    }

    [TestMethod]
    public async Task Handle_AssignsStableIds()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg">
                <rect x="0" y="0" width="10" height="10" />
                <circle cx="50" cy="50" r="10" />
            </svg>
            """;

        var doc = await Handler.Handle(new ImportSvgCommand(svg));

        var id1 = doc.Elements[0].Id;
        var id2 = doc.Elements[1].Id;
        Assert.IsNotNull(id1);
        Assert.IsNotNull(id2);
        Assert.AreNotEqual(id1, id2);
        Assert.IsTrue(Guid.TryParse(id1, out _));
    }

    [TestMethod]
    public async Task Handle_ThrowsOnMalformedXml()
    {
        const string svg = "<svg><rect not-closed";

        await Assert.ThrowsExactlyAsync<System.Xml.XmlException>(
            () => Handler.Handle(new ImportSvgCommand(svg)));
    }

    [TestMethod]
    public async Task Handle_InlineComment_ParsedAsSvgComment()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
                <!-- layer: background -->
                <rect x="0" y="0" width="100" height="100" />
            </svg>
            """;

        var doc = await Handler.Handle(new ImportSvgCommand(svg));

        var comments = doc.Elements.OfType<SvgEditor.Web.Features.Canvas.Models.SvgComment>().ToList();
        Assert.HasCount(1, comments);
        Assert.Contains(" layer: background ", comments[0].Text);
    }

    [TestMethod]
    public async Task Handle_PrologComment_CapturedInPrologComments()
    {
        const string svg = """
            <?xml version="1.0" encoding="UTF-8"?>
            <!-- Copyright 2024 Example Corp. -->
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
                <rect x="0" y="0" width="100" height="100" />
            </svg>
            """;

        var doc = await Handler.Handle(new ImportSvgCommand(svg));

        Assert.HasCount(1, doc.PrologComments);
        Assert.Contains("Copyright 2024 Example Corp.", doc.PrologComments[0]);
    }

    [TestMethod]
    public async Task Handle_NoComments_ElementsUnaffected()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
                <rect x="0" y="0" width="100" height="100" />
                <circle cx="50" cy="50" r="25" />
            </svg>
            """;

        var doc = await Handler.Handle(new ImportSvgCommand(svg));

        Assert.IsEmpty(doc.PrologComments);
        Assert.IsEmpty(doc.Elements.OfType<SvgEditor.Web.Features.Canvas.Models.SvgComment>());
        Assert.HasCount(2, doc.Elements);
    }
}
