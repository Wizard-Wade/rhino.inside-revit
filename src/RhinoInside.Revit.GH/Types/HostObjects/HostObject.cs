using System;
using System.Linq;
using System.Collections.Generic;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Host")]
  public interface IGH_HostObject : IGH_GeometricElement { }

  [Kernel.Attributes.Name("Host")]
  public class HostObject : GeometricElement, IGH_HostObject
  {
    protected override Type ValueType => typeof(ARDB.HostObject);
    public new ARDB.HostObject Value => base.Value as ARDB.HostObject;

    public HostObject() { }
    protected internal HostObject(ARDB.HostObject host) : base(host) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.HostObject host && !(host.Location is ARDB.LocationPoint) && !(host.Location is ARDB.LocationCurve))
        {
          if (host.GetSketch() is ARDB.Sketch sketch)
          {
            var center = Point3d.Origin;
            var count = 0;
            foreach (var curveArray in sketch.Profile.Cast<ARDB.CurveArray>())
            {
              foreach (var curve in curveArray.Cast<ARDB.Curve>())
              {
                count++;
                center += curve.Evaluate(0.0, normalized: true).ToPoint3d();
                count++;
                center += curve.Evaluate(1.0, normalized: true).ToPoint3d();
              }
            }
            center /= count;

            var hostLevelId = host.LevelId;
            if (hostLevelId == ARDB.ElementId.InvalidElementId)
              hostLevelId = host.get_Parameter(ARDB.BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM)?.AsElementId() ?? hostLevelId;

            if (host.Document.GetElement(hostLevelId) is ARDB.Level level)
              center.Z = level.GetElevation() * Revit.ModelUnits;

            var plane = sketch.SketchPlane.GetPlane().ToPlane();
            var origin = center;
            var xAxis = plane.XAxis;
            var yAxis = plane.YAxis;

            if (host is ARDB.Wall)
            {
              xAxis = -plane.XAxis;
              yAxis = plane.ZAxis;
            }

            if (host is ARDB.FootPrintRoof)
              origin.Z += host.get_Parameter(ARDB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;

            if (host is ARDB.ExtrusionRoof)
            {
              origin.Z += host.get_Parameter(ARDB.BuiltInParameter.ROOF_CONSTRAINT_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;
              yAxis = -plane.ZAxis;
            }

            return new Plane(origin, xAxis, yAxis);
          }
        }

        return base.Location;
      }
    }

    internal bool SetSlabShape(IList<Point3d> points, IList<Line> creases, out IList<Point3d> skipedPoints, out IList<Line> skipedCreases)
    {
      if (Value is ARDB.HostObject host)
      {
        InvalidateGraphics();

        skipedPoints = new List<Point3d>();
        skipedCreases = new List<Line>();

        var shape = host.GetSlabShapeEditor();
        using (shape as IDisposable) // ARDB.SlabShapeEditor is IDisposable since Revit 2023
        {
          shape.ResetSlabShape();
          shape.Enable();
          host.Document.Regenerate();

          var bbox = BoundingBox;
          var elevation = GeometryEncoder.ToInternalLength(bbox.Max.Z);

          var vertices = new Dictionary<Point3d, ARDB.SlabShapeVertex>(shape.SlabShapeVertices.Size + points.Count);
          ARDB.SlabShapeVertex AddVertex(Point3d point)
          {
            var x = GeometryEncoder.ToInternalLength(point.X);
            var y = GeometryEncoder.ToInternalLength(point.Y);
            var z = GeometryEncoder.ToInternalLength(point.Z);

            var xyz = new Point3d(x, y, z);
            if (!vertices.TryGetValue(xyz, out var vertex))
            {
              try
              {
                if ((vertex = shape.AddPoint(new ARDB.XYZ(x, y, elevation))) is object)
                  vertices.Add(xyz, vertex);
              }
              catch { }
            }

            return vertex?.VertexType == ARDB.SlabShapeVertexType.Invalid ? null : vertex;
          }

          if (points is object)
          {
            foreach (var point in points)
            {
              if (AddVertex(point) is null)
                skipedPoints.Add(point);
            }
          }

          if (creases is object)
          {
            foreach (var crease in creases)
            {
              if (crease.IsValid)
              {
                try
                {
                  var from = AddVertex(crease.From);
                  var to = AddVertex(crease.To);
                  if (from is null || to is null || shape.AddSplitLine(from, to) is null)
                    skipedCreases.Add(crease);
                }
                catch { skipedCreases.Add(crease); }
              }
              else skipedCreases.Add(crease);
            }
          }

          var bottomUpVertices = vertices.OrderBy(x => x.Key.Z);
          foreach (var vertex in bottomUpVertices)
            shape.ModifySubElement(vertex.Value, vertex.Key.Z - elevation);
        }

        return true;
      }

      skipedPoints = default;
      skipedCreases = default;
      return false;
    }
  }

  [Kernel.Attributes.Name("Host Type")]
  public interface IGH_HostObjectType : IGH_ElementType { }

  [Kernel.Attributes.Name("Host Type")]
  public class HostObjectType : ElementType, IGH_HostObjectType
  {
    protected override Type ValueType => typeof(ARDB.HostObjAttributes);
    public new ARDB.HostObjAttributes Value => base.Value as ARDB.HostObjAttributes;

    public HostObjectType() { }
    protected internal HostObjectType(ARDB.HostObjAttributes type) : base(type) { }

    public CompoundStructure CompoundStructure
    {
      get => Value is ARDB.HostObjAttributes type ? new CompoundStructure(Document, type.GetCompoundStructure()) : default;
      set
      {
        if (value is object && Value is ARDB.HostObjAttributes type)
          type.SetCompoundStructure(value.Value);
      }
    }
  }
}
