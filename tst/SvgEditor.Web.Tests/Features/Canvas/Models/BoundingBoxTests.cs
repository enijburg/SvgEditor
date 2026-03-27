using SvgEditor.Web.Features.Canvas.Models;

namespace SvgEditor.Web.Tests.Features.Canvas.Models;

[TestClass]
public sealed class BoundingBoxTests
{
    [TestMethod]
    public void Rect_ReturnsBoundingBox()
    {
        var rect = new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20", ["width"] = "100", ["height"] = "50" } };
        var bb = rect.GetBoundingBox();
        Assert.IsNotNull(bb);
        Assert.AreEqual(10, bb.X);
        Assert.AreEqual(20, bb.Y);
        Assert.AreEqual(100, bb.Width);
        Assert.AreEqual(50, bb.Height);
    }

    [TestMethod]
    public void Circle_ReturnsBoundingBox()
    {
        var circle = new SvgCircle { Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "60", ["r"] = "10" } };
        var bb = circle.GetBoundingBox();
        Assert.IsNotNull(bb);
        Assert.AreEqual(40, bb.X);
        Assert.AreEqual(50, bb.Y);
        Assert.AreEqual(20, bb.Width);
        Assert.AreEqual(20, bb.Height);
    }

    [TestMethod]
    public void Ellipse_ReturnsBoundingBox()
    {
        var ellipse = new SvgEllipse { Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "60", ["rx"] = "20", ["ry"] = "10" } };
        var bb = ellipse.GetBoundingBox();
        Assert.IsNotNull(bb);
        Assert.AreEqual(30, bb.X);
        Assert.AreEqual(50, bb.Y);
        Assert.AreEqual(40, bb.Width);
        Assert.AreEqual(20, bb.Height);
    }

    [TestMethod]
    public void Line_ReturnsBoundingBox()
    {
        var line = new SvgLine { Attributes = new Dictionary<string, string> { ["x1"] = "10", ["y1"] = "20", ["x2"] = "100", ["y2"] = "80" } };
        var bb = line.GetBoundingBox();
        Assert.IsNotNull(bb);
        Assert.AreEqual(10, bb.X);
        Assert.AreEqual(20, bb.Y);
        Assert.AreEqual(90, bb.Width);
        Assert.AreEqual(60, bb.Height);
    }

    [TestMethod]
    public void Polygon_ReturnsBoundingBox()
    {
        var polygon = new SvgPolygon { Attributes = new Dictionary<string, string> { ["points"] = "10,20 100,80 50,100" } };
        var bb = polygon.GetBoundingBox();
        Assert.IsNotNull(bb);
        Assert.AreEqual(10, bb.X);
        Assert.AreEqual(20, bb.Y);
        Assert.AreEqual(90, bb.Width);
        Assert.AreEqual(80, bb.Height);
    }

    [TestMethod]
    public void Polyline_ReturnsBoundingBox()
    {
        var polyline = new SvgPolyline { Attributes = new Dictionary<string, string> { ["points"] = "0,0 50,50 100,0" } };
        var bb = polyline.GetBoundingBox();
        Assert.IsNotNull(bb);
        Assert.AreEqual(0, bb.X);
        Assert.AreEqual(0, bb.Y);
        Assert.AreEqual(100, bb.Width);
        Assert.AreEqual(50, bb.Height);
    }

    [TestMethod]
    public void Group_ReturnsCombinedBoundingBox()
    {
        var group = new SvgGroup
        {
            Children =
            [
                new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "10", ["width"] = "20", ["height"] = "20" } },
                new SvgRect { Attributes = new Dictionary<string, string> { ["x"] = "50", ["y"] = "50", ["width"] = "30", ["height"] = "30" } }
            ]
        };
        var bb = group.GetBoundingBox();
        Assert.IsNotNull(bb);
        Assert.AreEqual(10, bb.X);
        Assert.AreEqual(10, bb.Y);
        Assert.AreEqual(70, bb.Width);
        Assert.AreEqual(70, bb.Height);
    }

    [TestMethod]
    public void EmptyGroup_ReturnsNull()
    {
        var group = new SvgGroup { Children = [] };
        Assert.IsNull(group.GetBoundingBox());
    }

    [TestMethod]
    public void Unknown_ReturnsNull()
    {
        var unknown = new SvgUnknown("defs");
        Assert.IsNull(unknown.GetBoundingBox());
    }

    [TestMethod]
    public void Path_ReturnsBoundingBox()
    {
        var path = new SvgPath { Attributes = new Dictionary<string, string> { ["d"] = "M 10 20 L 100 80 L 50 100" } };
        var bb = path.GetBoundingBox();
        Assert.IsNotNull(bb);
        Assert.AreEqual(10, bb.X);
        Assert.AreEqual(20, bb.Y);
        Assert.AreEqual(90, bb.Width);
        Assert.AreEqual(80, bb.Height);
    }

    [TestMethod]
    public void Path_EmptyD_ReturnsNull()
    {
        var path = new SvgPath { Attributes = new Dictionary<string, string> { ["d"] = "" } };
        Assert.IsNull(path.GetBoundingBox());
    }
}
