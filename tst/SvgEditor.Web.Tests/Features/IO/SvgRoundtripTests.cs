using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Canvas.UpdateElement;
using SvgEditor.Web.Features.IO.ExportSvg;
using SvgEditor.Web.Features.IO.ImportSvg;

namespace SvgEditor.Web.Tests.Features.IO;

[TestClass]
public sealed class SvgRoundtripTests
{
    private static readonly ImportSvgHandler ImportHandler = new();
    private static readonly ExportSvgHandler ExportHandler = new();

    [TestMethod]
    public async Task ImportMoveExportReimport_ElementPositionUpdated()
    {
        const string original = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200" width="200" height="200">
                <rect x="10" y="20" width="50" height="50" fill="blue" />
            </svg>
            """;

        var imported = await ImportHandler.Handle(new ImportSvgCommand(original));
        var state = new EditorState { Document = imported };
        var updateHandler = new UpdateElementHandler(state);
        var rectId = imported.Elements[0].Id;

        await updateHandler.Handle(new UpdateElementCommand(rectId, 15, 25));

        var exported = await ExportHandler.Handle(new ExportSvgCommand(state.Document!));
        var reimported = await ImportHandler.Handle(new ImportSvgCommand(exported));

        var movedRect = (SvgRect)reimported.Elements[0];
        Assert.AreEqual(25, movedRect.X);
        Assert.AreEqual(45, movedRect.Y);
    }

    [TestMethod]
    public async Task ImportExport_NoChanges_PreservesAllElements()
    {
        const string original = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 300">
                <rect x="0" y="0" width="100" height="100" />
                <circle cx="200" cy="150" r="75" />
                <ellipse cx="350" cy="100" rx="40" ry="30" />
            </svg>
            """;

        var imported = await ImportHandler.Handle(new ImportSvgCommand(original));
        var exported = await ExportHandler.Handle(new ExportSvgCommand(imported));
        var reimported = await ImportHandler.Handle(new ImportSvgCommand(exported));

        Assert.HasCount(3, reimported.Elements);
        Assert.IsInstanceOfType<SvgRect>(reimported.Elements[0]);
        Assert.IsInstanceOfType<SvgCircle>(reimported.Elements[1]);
        Assert.IsInstanceOfType<SvgEllipse>(reimported.Elements[2]);
    }
}
