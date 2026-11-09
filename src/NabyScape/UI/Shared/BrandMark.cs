using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
namespace NabyScape;
// Approved proposal 03 with smooth curves. One vector supplies every size/theme.
internal static class BrandMark
{
    private static readonly Geometry Artwork=Load();
    private static Geometry Load()
    {
        using var stream=typeof(BrandMark).Assembly.GetManifestResourceStream("NabyScape.BrandMark.svg")??throw new InvalidOperationException("Missing brand artwork.");
        var svg=XDocument.Load(stream);var shapes=new GeometryGroup{FillRule=FillRule.Nonzero};
        foreach(var path in svg.Descendants().Where(e=>e.Name.LocalName=="path"))shapes.Children.Add(Geometry.Parse((string)path.Attribute("d")!));
        shapes.Freeze();return shapes;
    }
    internal static Path Create(double width=30)
    {
        var mark=new Path{Data=Artwork,Width=width,Height=width*Artwork.Bounds.Height/Artwork.Bounds.Width,Stretch=Stretch.Uniform,VerticalAlignment=VerticalAlignment.Center};
        mark.SetResourceReference(Shape.FillProperty,"ControlText");return mark;
    }
}
