using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  #if REVIT_2023
    using ARDB_Structure_AnalyticalSurfaceBase = ARDB.Structure.AnalyticalSurfaceBase;
  #else
    using ARDB_Structure_AnalyticalSurfaceBase = ARDB.Structure.AnalyticalModelSurface;
  #endif

  [ComponentVersion(introduced: "1.26")]
  public class AnalyticalSurface : GraphicalElement<Types.AnalyticalMember, ARDB_Structure_AnalyticalSurfaceBase>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("7576D430-37BD-4255-86B8-49495C15BEDF");

    public AnalyticalSurface() : base("Analytical Surface", "Analytical Surface", "Contains a collection of Revit analytical surfaces", "Params", "Revit Elements") { }
  }
}
