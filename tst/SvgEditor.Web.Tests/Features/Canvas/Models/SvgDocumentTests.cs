using SvgEditor.Web.Features.Canvas.Models;

namespace SvgEditor.Web.Tests.Features.Canvas.Models;

[TestClass]
public sealed class SvgDocumentTests
{
    private static SvgDocument CreateDocument() => new SvgDocument
    {
        ViewBox = "0 0 100 100",
        Width = 100,
        Height = 100,
        Elements =
        [
            new SvgRect { Id = "rect1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20", ["width"] = "30", ["height"] = "40" } },
            new SvgCircle { Id = "circle1", Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "50", ["r"] = "25" } },
            new SvgGroup
            {
                Id = "group1",
                Attributes = [],
                Children =
                [
                    new SvgRect { Id = "nested-rect", Attributes = new Dictionary<string, string> { ["x"] = "5", ["y"] = "5", ["width"] = "10", ["height"] = "10" } }
                ]
            }
        ]
    };

    [TestMethod]
    public void FindById_ExistingElement_ReturnsElement()
    {
        var doc = CreateDocument();

        var result = doc.FindById("rect1");

        Assert.IsNotNull(result);
        Assert.AreEqual("rect1", result.Id);
    }

    [TestMethod]
    public void FindById_NestedElement_ReturnsElement()
    {
        var doc = CreateDocument();

        var result = doc.FindById("nested-rect");

        Assert.IsNotNull(result);
        Assert.AreEqual("nested-rect", result.Id);
    }

    [TestMethod]
    public void FindById_MissingElement_ReturnsNull()
    {
        var doc = CreateDocument();

        var result = doc.FindById("does-not-exist");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ReplaceElement_ReplacesExistingElement()
    {
        var doc = CreateDocument();
        var updated = new SvgRect { Id = "rect1", Attributes = new Dictionary<string, string> { ["x"] = "99", ["y"] = "99", ["width"] = "1", ["height"] = "1" } };

        var newDoc = doc.ReplaceElement(updated);

        var found = newDoc.FindById("rect1") as SvgRect;
        Assert.IsNotNull(found);
        Assert.AreEqual(99, found.X);
    }

    [TestMethod]
    public void ReplaceElement_PreservesOtherElements()
    {
        var doc = CreateDocument();
        var updated = new SvgRect { Id = "rect1", Attributes = new Dictionary<string, string> { ["x"] = "0", ["y"] = "0", ["width"] = "5", ["height"] = "5" } };

        var newDoc = doc.ReplaceElement(updated);

        Assert.HasCount(3, newDoc.Elements);
        Assert.IsNotNull(newDoc.FindById("circle1"));
    }

    [TestMethod]
    public void DeepClone_CreatesIndependentCopy()
    {
        var doc = CreateDocument();

        var clone = doc.DeepClone();
        clone.Elements[0].Attributes["x"] = "999";

        var original = doc.FindById("rect1") as SvgRect;
        Assert.AreNotEqual(999, original!.X);
    }
}
