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

    [TestMethod]
    public void GetForegroundColorAttribute_Line_ReturnsStroke()
    {
        var line = new SvgLine { Attributes = new Dictionary<string, string> { ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100", ["stroke"] = "red" } };

        Assert.AreEqual("stroke", line.GetForegroundColorAttribute());
    }

    [TestMethod]
    public void GetForegroundColorAttribute_Rect_ReturnsFill()
    {
        var rect = new SvgRect { Attributes = new Dictionary<string, string> { ["fill"] = "blue" } };

        Assert.AreEqual("fill", rect.GetForegroundColorAttribute());
    }

    [TestMethod]
    public void GetForegroundColorAttribute_PathWithFillNoneAndStroke_ReturnsStroke()
    {
        var path = new SvgPath { Attributes = new Dictionary<string, string> { ["d"] = "M0 0 L100 100", ["fill"] = "none", ["stroke"] = "#ff0000" } };

        Assert.AreEqual("stroke", path.GetForegroundColorAttribute());
    }

    [TestMethod]
    public void GetForegroundColorAttribute_PathWithFill_ReturnsFill()
    {
        var path = new SvgPath { Attributes = new Dictionary<string, string> { ["d"] = "M0 0 L100 100", ["fill"] = "#00ff00" } };

        Assert.AreEqual("fill", path.GetForegroundColorAttribute());
    }

    [TestMethod]
    public void GetForegroundColorAttribute_PolylineWithFillNoneAndStroke_ReturnsStroke()
    {
        var polyline = new SvgPolyline { Attributes = new Dictionary<string, string> { ["points"] = "0,0 100,100", ["fill"] = "none", ["stroke"] = "green" } };

        Assert.AreEqual("stroke", polyline.GetForegroundColorAttribute());
    }

    [TestMethod]
    public void GetForegroundColorAttribute_LineWithoutStrokeAttribute_ReturnsStroke()
    {
        var line = new SvgLine { Attributes = new Dictionary<string, string> { ["x1"] = "0", ["y1"] = "0", ["x2"] = "10", ["y2"] = "10" } };

        Assert.AreEqual("stroke", line.GetForegroundColorAttribute());
    }

    [TestMethod]
    public void SvgRect_WithResize_ScalesDimensions()
    {
        var rect = new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "10", ["width"] = "100", ["height"] = "50" } };

        var resized = (SvgRect)rect.WithResize(new BoundingBox(10, 10, 100, 50), new BoundingBox(10, 10, 200, 100));

        Assert.AreEqual(10, resized.X);
        Assert.AreEqual(10, resized.Y);
        Assert.AreEqual(200, resized.Width);
        Assert.AreEqual(100, resized.Height);
    }

    [TestMethod]
    public void SvgCircle_WithResize_ScalesRadius()
    {
        var circle = new SvgCircle { Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "50", ["r"] = "20" } };

        var resized = (SvgCircle)circle.WithResize(new BoundingBox(30, 30, 40, 40), new BoundingBox(30, 30, 80, 80));

        Assert.AreEqual(70, resized.Cx);
        Assert.AreEqual(70, resized.Cy);
        Assert.AreEqual(40, resized.R);
    }

    [TestMethod]
    public void SvgEllipse_WithResize_ScalesRadii()
    {
        var ellipse = new SvgEllipse { Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "50", ["rx"] = "30", ["ry"] = "20" } };

        var resized = (SvgEllipse)ellipse.WithResize(new BoundingBox(20, 30, 60, 40), new BoundingBox(20, 30, 120, 80));

        Assert.AreEqual(80, resized.Cx);
        Assert.AreEqual(70, resized.Cy);
        Assert.AreEqual(60, resized.Rx);
        Assert.AreEqual(40, resized.Ry);
    }

    [TestMethod]
    public void SvgLine_WithResize_ScalesEndpoints()
    {
        var line = new SvgLine { Attributes = new Dictionary<string, string> { ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100" } };

        var resized = (SvgLine)line.WithResize(new BoundingBox(0, 0, 100, 100), new BoundingBox(0, 0, 200, 200));

        Assert.AreEqual(0, resized.X1);
        Assert.AreEqual(0, resized.Y1);
        Assert.AreEqual(200, resized.X2);
        Assert.AreEqual(200, resized.Y2);
    }

    [TestMethod]
    public void SvgImage_WithResize_ScalesDimensions()
    {
        var image = new SvgImage { Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20", ["width"] = "100", ["height"] = "80" } };

        var resized = (SvgImage)image.WithResize(new BoundingBox(10, 20, 100, 80), new BoundingBox(10, 20, 200, 160));

        Assert.AreEqual(10, resized.X);
        Assert.AreEqual(20, resized.Y);
        Assert.AreEqual(200, resized.Width);
        Assert.AreEqual(160, resized.Height);
    }

    [TestMethod]
    public void SvgText_WithResize_ScalesFontSize()
    {
        var text = new SvgText { Attributes = new Dictionary<string, string> { ["x"] = "0", ["y"] = "16", ["font-size"] = "16" }, Content = "Hello" };

        var resized = (SvgText)text.WithResize(new BoundingBox(0, 0, 40, 16), new BoundingBox(0, 0, 80, 32));

        Assert.AreEqual("32", resized.Attributes["font-size"]);
    }

    [TestMethod]
    public void SvgPolygon_WithResize_ScalesPoints()
    {
        var polygon = new SvgPolygon { Attributes = new Dictionary<string, string> { ["points"] = "0,0 100,0 100,100 0,100" } };

        var resized = (SvgPolygon)polygon.WithResize(new BoundingBox(0, 0, 100, 100), new BoundingBox(0, 0, 200, 200));

        Assert.AreEqual("0,0 200,0 200,200 0,200", resized.Points);
    }

    [TestMethod]
    public void SvgPolyline_WithResize_ScalesPoints()
    {
        var polyline = new SvgPolyline { Attributes = new Dictionary<string, string> { ["points"] = "0,0 50,0 50,50" } };

        var resized = (SvgPolyline)polyline.WithResize(new BoundingBox(0, 0, 50, 50), new BoundingBox(0, 0, 100, 100));

        Assert.AreEqual("0,0 100,0 100,100", resized.Points);
    }

    [TestMethod]
    public void SvgGroup_WithResize_ResizesChildren()
    {
        var group = new SvgGroup
        {
            Attributes = [],
            Children =
            [
                new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "0", ["y"] = "0", ["width"] = "50", ["height"] = "50" } },
            ]
        };

        var resized = (SvgGroup)group.WithResize(new BoundingBox(0, 0, 50, 50), new BoundingBox(0, 0, 100, 100));

        var resizedRect = (SvgRect)resized.Children[0];
        Assert.AreEqual(0, resizedRect.X);
        Assert.AreEqual(0, resizedRect.Y);
        Assert.AreEqual(100, resizedRect.Width);
        Assert.AreEqual(100, resizedRect.Height);
    }
}
