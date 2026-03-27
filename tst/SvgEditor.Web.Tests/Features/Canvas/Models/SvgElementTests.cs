using SvgEditor.Web.Features.Canvas.Models;

namespace SvgEditor.Web.Tests.Features.Canvas.Models;

[TestClass]
public sealed class SvgElementTests
{
    [TestMethod]
    public void SvgRect_DefaultId_IsGuid()
    {
        var rect = new SvgRect();

        Assert.IsTrue(Guid.TryParse(rect.Id, out _));
    }

    [TestMethod]
    public void SvgRect_WithOffset_UpdatesXAndY()
    {
        var rect = new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };

        var moved = (SvgRect)rect.WithOffset(5, -3);

        Assert.AreEqual(15, moved.X);
        Assert.AreEqual(17, moved.Y);
    }

    [TestMethod]
    public void SvgCircle_WithOffset_UpdatesCxCy()
    {
        var circle = new SvgCircle { Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "60", ["r"] = "10" } };

        var moved = (SvgCircle)circle.WithOffset(10, 10);

        Assert.AreEqual(60, moved.Cx);
        Assert.AreEqual(70, moved.Cy);
    }

    [TestMethod]
    public void SvgLine_WithOffset_UpdatesAllCoordinates()
    {
        var line = new SvgLine { Attributes = new Dictionary<string, string> { ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100" } };

        var moved = (SvgLine)line.WithOffset(5, 10);

        Assert.AreEqual(5, moved.X1);
        Assert.AreEqual(10, moved.Y1);
        Assert.AreEqual(105, moved.X2);
        Assert.AreEqual(110, moved.Y2);
    }

    [TestMethod]
    public void SvgPath_WithOffset_AddsTransformAttribute()
    {
        var path = new SvgPath { Attributes = new Dictionary<string, string> { ["d"] = "M 0 0 L 100 100" } };

        var moved = (SvgPath)path.WithOffset(20, 30);

        Assert.IsTrue(moved.Attributes.ContainsKey("transform"));
        Assert.AreEqual("translate(20,30)", moved.Attributes["transform"]);
    }

    [TestMethod]
    public void SvgPath_WithOffset_AccumulatesExistingTranslation()
    {
        var path = new SvgPath { Attributes = new Dictionary<string, string> { ["d"] = "M 0 0", ["transform"] = "translate(10,20)" } };

        var moved = (SvgPath)path.WithOffset(5, 5);

        Assert.AreEqual("translate(15,25)", moved.Attributes["transform"]);
    }

    [TestMethod]
    public void SvgRect_DeepClone_IsIndependent()
    {
        var rect = new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "10", ["fill"] = "red" } };

        var clone = (SvgRect)rect.DeepClone();
        clone.Attributes["x"] = "999";

        Assert.AreEqual("10", rect.Attributes["x"]);
    }

    [TestMethod]
    public void SvgGroup_WithOffset_MovesAllChildren()
    {
        var group = new SvgGroup
        {
            Attributes = [],
            Children =
            [
                new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "0", ["y"] = "0" } },
                new SvgCircle { Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "50", ["r"] = "10" } }
            ]
        };

        var moved = (SvgGroup)group.WithOffset(10, 10);

        var movedRect = (SvgRect)moved.Children[0];
        var movedCircle = (SvgCircle)moved.Children[1];
        Assert.AreEqual(10, movedRect.X);
        Assert.AreEqual(60, movedCircle.Cx);
    }

    [TestMethod]
    public void SvgImage_WithOffset_UpdatesXAndY()
    {
        var image = new SvgImage { Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20", ["width"] = "100", ["height"] = "80" } };

        var moved = (SvgImage)image.WithOffset(5, -3);

        Assert.AreEqual(15, moved.X);
        Assert.AreEqual(17, moved.Y);
        Assert.AreEqual(100, moved.Width);
        Assert.AreEqual(80, moved.Height);
    }

    [TestMethod]
    public void SvgImage_GetBoundingBox_ReturnsCorrectBox()
    {
        var image = new SvgImage { Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20", ["width"] = "100", ["height"] = "80" } };

        var bb = image.GetBoundingBox();

        Assert.IsNotNull(bb);
        Assert.AreEqual(10, bb.X);
        Assert.AreEqual(20, bb.Y);
        Assert.AreEqual(100, bb.Width);
        Assert.AreEqual(80, bb.Height);
    }

    [TestMethod]
    public void SvgImage_DeepClone_IsIndependent()
    {
        var image = new SvgImage { Attributes = new Dictionary<string, string> { ["x"] = "10", ["href"] = "data:image/png;base64,abc" } };

        var clone = (SvgImage)image.DeepClone();
        clone.Attributes["x"] = "999";

        Assert.AreEqual("10", image.Attributes["x"]);
    }
}
