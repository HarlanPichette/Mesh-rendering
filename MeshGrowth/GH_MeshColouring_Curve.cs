using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;

namespace MeshGrowth
{
    public class GH_MeshColouring_Curve : GH_Component
    {

        public GH_MeshColouring_Curve()
            : base("MeshColouring", "MeshColouring", "MeshColouring", "McMuffin", "MeshColouring")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "Curves", "Curves", GH_ParamAccess.list);
            pManager.AddMeshParameter("StartingMeshes", "StartingMeshes", "StartingMeshes", GH_ParamAccess.item);
            pManager.AddColourParameter("FarthestColour", "FarthestColour", "FarthestColour", GH_ParamAccess.item);
            pManager.AddColourParameter("ClosestColour", "ClosestColour", "ClosestColour", GH_ParamAccess.item);
            pManager.AddNumberParameter("Range", "Range", "Range", GH_ParamAccess.item);
            pManager.AddNumberParameter("Divide", "Divide", "Divide", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "Meshes", "Meshes", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> iCurves = new List<Curve>();
            Mesh iStartingMesh = new Mesh();
            Color iFarthestColour = new Color();
            Color iClosestColour = new Color();
            double iRange = 0.0;
            double iDivide = 0.0;
            RTree rTree = new RTree();
            List<Point3d> CurvePoints = new List<Point3d>();
            List<double> allDistances = new List<double>();

            //=============================================================================================

            DA.GetDataList("Curves", iCurves);
            DA.GetData("StartingMeshes", ref iStartingMesh);
            DA.GetData("FarthestColour", ref iFarthestColour);
            DA.GetData("ClosestColour", ref iClosestColour);
            DA.GetData("Range", ref iRange);
            DA.GetData("Divide", ref iDivide);

            if (iRange == 0 || iDivide == 0) { for (int i = 0; i < iStartingMesh.Vertices.Count; i++) iStartingMesh.VertexColors.SetColor(i, iFarthestColour); }


            else
            {
                for (int i = 0; i < iStartingMesh.Vertices.Count; i++)
                {
                    allDistances.Add(99999);
                    rTree.Insert((Point3d)iStartingMesh.Vertices[i], i);
                }


                foreach (Curve item in iCurves)
                {
                    List<double> tValues = item.DivideByLength(iDivide, true).ToList();
                    foreach (double value in tValues) CurvePoints.Add(item.PointAt(value));
                }

                for (int i = 0; i < CurvePoints.Count; i++)
                {
                    Sphere searchSphere = new Sphere(CurvePoints[i], iRange);
                    rTree.Search(searchSphere, (sender, args) =>
                    {
                        double distance = (iStartingMesh.Vertices[args.Id] - CurvePoints[i]).Length;
                        if (allDistances[args.Id] > distance) { allDistances[args.Id] = distance; }
                    });
                }


                List<double> trueDistances = new List<double>();

                for (int i = 0; i < iStartingMesh.Vertices.Count; i++)
                {
                    if (allDistances[i] != 99999) trueDistances.Add(allDistances[i]);

                }

                double minDistance = trueDistances.Min();
                double maxDistance = trueDistances.Max();

                for (int i = 0; i < iStartingMesh.Vertices.Count; i++)
                {
                    if (allDistances[i] == 99999) allDistances[i] = maxDistance;
                }

                if (minDistance != maxDistance)
                {
                    for (int i = 0; i < iStartingMesh.Vertices.Count; i++)
                    {

                        int newR = (int)Remap(allDistances[i], minDistance, maxDistance, iClosestColour.R, iFarthestColour.R);
                        int newG = (int)Remap(allDistances[i], minDistance, maxDistance, iClosestColour.G, iFarthestColour.G);
                        int newB = (int)Remap(allDistances[i], minDistance, maxDistance, iClosestColour.B, iFarthestColour.B);
                        iStartingMesh.VertexColors.SetColor(i, newR, newG, newB);
                    }
                }

            }
            DA.SetData("Meshes", iStartingMesh);
        }

        protected override System.Drawing.Bitmap Icon { get { return null; } }

        public override Guid ComponentGuid { get { return new Guid("ca062f30-9fe1-4282-9a0b-06ae14a4a98f"); } }

        public static double Remap(double from, double fromMin, double fromMax, double toMin, double toMax)
        {
            var fromAbs = from - fromMin;
            var fromMaxAbs = fromMax - fromMin;

            var normal = fromAbs / fromMaxAbs;

            var toMaxAbs = toMax - toMin;
            var toAbs = toMaxAbs * normal;

            var to = toAbs + toMin;

            return to;
        }




    }
}