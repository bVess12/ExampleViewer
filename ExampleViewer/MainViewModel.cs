using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Point3D = global::SharpDX.Vector3;
using HelixToolkit.Wpf.SharpDX;
using Microsoft.Win32;
using System.IO;

namespace ExampleViewer
{
    class MainViewModel : BaseViewModel
    {
        Point3D _RotationPoint;
        public Point3D RotationPoint { get { return _RotationPoint; } set { _RotationPoint = value; OnPropertyChanged(nameof(RotationPoint)); } }

        EffectsManager _EffectsManager;
        public EffectsManager EffectsManager { get { return _EffectsManager; } set { _EffectsManager = value; OnPropertyChanged(nameof(EffectsManager)); } }

        MeshGeometry3D _MeshModel;
        public MeshGeometry3D MeshModel { get { return _MeshModel; } set { _MeshModel = value; OnPropertyChanged(nameof(MeshModel)); } }

        Material _MeshMaterial;
        public Material MeshMaterial { get { return _MeshMaterial; } set { _MeshMaterial = value; OnPropertyChanged(nameof(MeshMaterial)); } }

        HelixToolkit.Wpf.SharpDX.Camera _MainCamera;
        public HelixToolkit.Wpf.SharpDX.Camera MainCamera { get { return _MainCamera; } set { _MainCamera = value; OnPropertyChanged(nameof(MainCamera)); } }


        public MainViewModel()
        {
            //Create the Effects Manager
            this.EffectsManager = new DefaultEffectsManager();

            //Create the Material
            this.MeshMaterial = DiffuseMaterials.Bisque;

            //Set the initial rotation point to the origin
            this.RotationPoint = Point3D.Zero;

            //Create the Camera
            this.MainCamera = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera
            {
                Position = new System.Windows.Media.Media3D.Point3D(0f, 40, 40),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0f, -40, -40),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1)
            };
        }

        #region File Loading
        public void LoadSRTFile()
        {
            OpenFileDialog shortHandDialog = new OpenFileDialog();
            shortHandDialog.AddExtension = true;
            shortHandDialog.Filter = "3D Model Shorthand (.srt)|*.srt";
            shortHandDialog.DefaultExt = ".srt";
            if (shortHandDialog.ShowDialog() == true)
                loadShortHand(shortHandDialog.FileName);
        }
        void loadShortHand(string fileName)
        {
            var lines = File.ReadLines(fileName).ToArray();
            int numLines = lines.Length;

            //width 
            //height
            //xSpacing
            //ySpacing
            //heights[0, 0] ... heights[0, width - 1]
            //heights[1, 0] ... heights[1, width - 1]
            //...

            float xSpacing = 0f, ySpacing = 0f;
            int width = 0, height = 0;
            float[,] heightArray = null;

            int x = 0, y = 0;
            for (int i = 0; i < numLines; i++)
            {
                if (i == 0)
                    width = Int32.Parse(lines[i]);
                else if (i == 1)
                    height = Int32.Parse(lines[i]);
                else if (i == 2)
                    xSpacing = float.Parse(lines[i]);
                else if (i == 3)
                {
                    ySpacing = float.Parse(lines[i]);
                    heightArray = new float[width, height];
                }
                else
                {
                    heightArray[x, y] = float.Parse(lines[i]);
                    x++;
                    if (x >= width)
                    {
                        x = 0;
                        y++;
                    }
                }
            }

            //Create the Model and load it into the scene
            CreateMeshFromPoints(heightArray, xSpacing, ySpacing);
        }
        #endregion File Loading

        #region Mesh Creation
        const float BASE_HEIGHT = -0.125f;
        void CreateMeshFromPoints(float[,] pointHeights, float xSpacing, float ySpacing)
        {
            //Create the Meshbuilder
            MeshBuilder mb = new MeshBuilder(true);


            //Iterate through the point cloud
            int width = pointHeights.GetLength(0), height = pointHeights.GetLength(1);
            for (int x = 0; x < width - 1; x++)
                for(int y = 0; y < height - 1; y++)
                {
                    //Create the top face, starting at the lower-left corner and rotating counter-clockwise
                    Point3D pointOne = new Point3D(x * xSpacing, y * ySpacing, pointHeights[x, y]);
                    Point3D pointTwo = new Point3D((x + 1) * xSpacing, y * ySpacing, pointHeights[x + 1, y]);
                    Point3D pointThree = new Point3D((x + 1) * xSpacing, (y + 1) * ySpacing, pointHeights[x + 1, y + 1]);
                    Point3D pointFour = new Point3D(x * xSpacing, (y + 1) * ySpacing, pointHeights[x, y + 1]);

                    //Add the quad to the mesh builder
                    mb.AddQuad(pointFour, pointThree, pointTwo, pointOne);

                    //Add the left side, if necessary
                    if (x == 0)
                    {
                        //Create the left face
                        pointOne = new Point3D(x * xSpacing, (y + 1) * ySpacing, BASE_HEIGHT);
                        pointTwo = new Point3D(x * xSpacing, y * ySpacing, BASE_HEIGHT);
                        pointThree = new Point3D(x * xSpacing, y * ySpacing, pointHeights[x, y]);
                        pointFour = new Point3D(x * xSpacing, (y + 1) * ySpacing, pointHeights[x, y + 1]);

                        //Add the quad to the mesh builder
                        mb.AddQuad(pointOne, pointTwo, pointThree, pointFour);
                    }
                    //Add the right side, if necessary
                    if(x == width - 1)
                    {
                        //Create the right face
                        pointOne = new Point3D((x + 1) * xSpacing, y * ySpacing, BASE_HEIGHT);
                        pointTwo = new Point3D((x + 1) * xSpacing, (y + 1) * ySpacing, BASE_HEIGHT);
                        pointThree = new Point3D((x + 1) * xSpacing, (y + 1) * ySpacing, pointHeights[x + 1, y + 1]);
                        pointFour = new Point3D((x + 1) * xSpacing, y * ySpacing, pointHeights[x + 1, y]);

                        //Add the quad to the mesh builder
                        mb.AddQuad(pointOne, pointTwo, pointThree, pointFour);
                    }
                    //Add the bottom side, if necessary
                    if(y == 0)
                    {
                        //Create the bottom face
                        pointOne = new Point3D(x * xSpacing, y * ySpacing, BASE_HEIGHT);
                        pointTwo = new Point3D((x + 1) * xSpacing, y * ySpacing, BASE_HEIGHT);
                        pointThree = new Point3D((x + 1) * xSpacing, y * ySpacing, pointHeights[x + 1, y]);
                        pointFour = new Point3D(x * xSpacing, y * ySpacing, pointHeights[x, y]);

                        //Add the quad to the mesh builder
                        mb.AddQuad(pointOne, pointTwo, pointThree, pointFour);
                    }
                    //Add the top side, if necessary
                    if(y == height - 1)
                    {
                        //Create the top face
                        pointOne = new Point3D((x + 1) * xSpacing, (y + 1) * ySpacing, BASE_HEIGHT);
                        pointTwo = new Point3D(x * xSpacing, (y + 1) * ySpacing, BASE_HEIGHT);
                        pointThree = new Point3D(x * xSpacing, (y + 1) * ySpacing, pointHeights[x, y + 1]);
                        pointFour = new Point3D((x + 1) * xSpacing, (y + 1) * ySpacing, pointHeights[x + 1, y + 1]);

                        //Add the quad to the mesh builder
                        mb.AddQuad(pointOne, pointTwo, pointThree, pointFour);
                    }

                    //Add the under side
                    pointOne = new Point3D((x + 1) * xSpacing, y * ySpacing, BASE_HEIGHT);
                    pointTwo = new Point3D(x * xSpacing, y * ySpacing, BASE_HEIGHT);
                    pointThree = new Point3D(x * xSpacing, (y + 1) * ySpacing, BASE_HEIGHT);
                    pointFour = new Point3D((x + 1) * xSpacing, (y + 1) * ySpacing, BASE_HEIGHT);

                    //Add the quad to the mesh builder
                    mb.AddQuad(pointOne, pointTwo, pointThree, pointFour);
                }

            //Update the loaded geometry
            this.MeshModel = mb.ToMeshGeometry3D();
        }
        #endregion Mesh Creation
    }
}
