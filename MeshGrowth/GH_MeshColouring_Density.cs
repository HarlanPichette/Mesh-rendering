using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;

namespace MeshGrowth
{
    public class GH_MeshColouring_Density : GH_Component
    {

        public GH_MeshColouring_Density()
            : base("MeshColouring", "MeshColouring", "MeshColouring", "McMuffin", "MeshColouring")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("StartingMeshes", "StartingMeshes", "StartingMeshes", GH_ParamAccess.item);
            pManager.AddColourParameter("MinColor", "MinColor", "MinColor", GH_ParamAccess.item);
            pManager.AddColourParameter("MaxColor", "MaxColor", "MaxColor", GH_ParamAccess.item);
            pManager.AddNumberParameter("Range", "Range", "Range", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "Meshes", "Meshes", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Mesh iStartingMesh = new Mesh();
            Color iMinColor = new Color();
            Color iMaxColor = new Color();
            double iRange = 0.0;

            RTree rTree = new RTree();
            List<int> DensityCount = new List<int>();
            int MinDensity = 0;
            int MaxDensity = 0;



            DA.GetData("StartingMeshes", ref iStartingMesh);
            DA.GetData("MinColor", ref iMinColor);
            DA.GetData("MaxColor", ref iMaxColor);
            DA.GetData("Range", ref iRange);

            if (iRange == 0) { for (int i = 0; i < iStartingMesh.Vertices.Count; i++) iStartingMesh.VertexColors.SetColor(i, iMinColor); }
            else
            {
                for (int i = 0; i < iStartingMesh.Vertices.Count; i++) { rTree.Insert((Point3d)iStartingMesh.Vertices[i], i); }

                for (int i = 0; i < iStartingMesh.Vertices.Count; i++)
                {
                    List<int> collisionIndices = new List<int>();
                    Sphere searchSphere = new Sphere((Point3d)iStartingMesh.Vertices[i], iRange);
                    rTree.Search(searchSphere, (sender, args) => { collisionIndices.Add(args.Id); });
                    DensityCount.Add(collisionIndices.Count);
                }

                MinDensity = DensityCount.Min();
                MaxDensity = DensityCount.Max();

                if (MinDensity != MaxDensity)
                {
                    for (int i = 0; i < iStartingMesh.Vertices.Count; i++)
                    {
                        int newR = (int)Remap(DensityCount[i], MinDensity, MaxDensity, iMinColor.R, iMaxColor.R);
                        int newG = (int)Remap(DensityCount[i], MinDensity, MaxDensity, iMinColor.G, iMaxColor.G);
                        int newB = (int)Remap(DensityCount[i], MinDensity, MaxDensity, iMinColor.B, iMaxColor.B);
                        iStartingMesh.VertexColors.SetColor(i, newR, newG, newB);
                    }
                }

            }
            //=============================================================================================


            DA.SetData("Meshes", iStartingMesh);

        }

        protected override System.Drawing.Bitmap Icon { get { return null; } }

        public override Guid ComponentGuid { get { return new Guid("4f8083bc-df49-4828-92df-5d4ee7701160"); } }

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