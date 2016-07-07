using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TerrainEditor.Utilities;
using Urho;

namespace TerrainEditor.Core
{

    public class GeometryBuilder
    {   
        private const string AllCurvesShouldHaveTheSameNumberOfPoints = "All curves should have the same number of points";
        private const string SourceMeshNormalsShouldNotBeNull = "Source mesh normal vectors should not be null.";
        private const string SourceMeshTextureCoordinatesShouldNotBeNull = "Source mesh texture coordinates should not be null.";
        private const string WrongNumberOfDiameters = "Wrong number of diameters.";
        private const string WrongNumberOfAngles = "Wrong number of angles.";
        private const string WrongNumberOfPositions = "Wrong number of positions.";
        private const string WrongNumberOfNormals = "Wrong number of normal vectors.";
        private const string WrongNumberOfTextureCoordinates = "Wrong number of texture coordinates.";
        
        private static readonly ThreadLocal<Dictionary<int, IList<Vector2>>> CircleCache = new ThreadLocal<Dictionary<int, IList<Vector2>>>(() => new Dictionary<int, IList<Vector2>>());


        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// Normal and texture coordinate generation are included.
        /// </remarks>
        public GeometryBuilder()
            : this(true, true)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryBuilder"/> class.
        /// </summary>
        /// <param name="generateNormals">
        /// Generate normal vectors.
        /// </param>
        /// <param name="generateTextureCoordinates">
        /// Generate texture coordinates.
        /// </param>
        public GeometryBuilder(bool generateNormals, bool generateTextureCoordinates)
        {
            Positions = new List<Vector3>();
            TriangleIndices = new List<int>();

            if (generateNormals)
                Normals = new List<Vector3>();

            if (generateTextureCoordinates)
                TextureCoordinates = new List<Vector2>();
        }


        /// <summary>
        /// Gets the normal vectors of the mesh.
        /// </summary>
        /// <value>The normal vectors.</value>
        public IList<Vector3> Normals { get; }
        /// <summary>
        /// Gets the positions collection of the mesh.
        /// </summary>
        /// <value> The positions. </value>
        public IList<Vector3> Positions { get; }
        /// <summary>
        /// Gets the texture coordinates of the mesh.
        /// </summary>
        /// <value>The texture coordinates.</value>
        public IList<Vector2> TextureCoordinates { get; }
        /// <summary>
        /// Gets the triangle indices.
        /// </summary>
        /// <value>The triangle indices.</value>
        public IList<int> TriangleIndices { get; }
        /// <summary>
        /// Gets or sets a value indicating whether to create normal vectors.
        /// </summary>
        /// <value>
        /// <c>true</c> if normal vectors should be created; otherwise, <c>false</c>.
        /// </value>
        public bool CreateNormals => Normals != null;
        /// <summary>
        /// Gets or sets a value indicating whether to create texture coordinates.
        /// </summary>
        /// <value>
        /// <c>true</c> if texture coordinates should be created; otherwise, <c>false</c>.
        /// </value>
        public bool CreateTextureCoordinates => TextureCoordinates != null;

        /// <summary>
        /// Clears the data
        /// </summary>
        public void Clear()
        {
            Positions.Clear();
            TriangleIndices.Clear();

            if (CreateTextureCoordinates)
                TextureCoordinates.Clear();

            if (CreateNormals)
                Normals.Clear();
        }
        /// <summary>
        /// Adds an arrow to the mesh.
        /// </summary>
        /// <param name="point1">
        /// The start point.
        /// </param>
        /// <param name="point2">
        /// The end point.
        /// </param>
        /// <param name="diameter">
        /// The diameter of the arrow cylinder.
        /// </param>
        /// <param name="headLength">
        /// Length of the head (relative to diameter).
        /// </param>
        /// <param name="thetaDiv">
        /// The number of divisions around the arrow.
        /// </param>
        public void AddArrow(Vector3 point1, Vector3 point2, float diameter, float headLength = 3, int thetaDiv = 18)
        {
            var dir = point2 - point1;
            float length = dir.Length;
            float r = diameter/2;

            var pc = new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(0, r),
                new Vector2(length - diameter*headLength, r),
                new Vector2(length - diameter*headLength, r*2),
                new Vector2(length, 0)
            };

            AddRevolvedGeometry(pc, null, point1, dir, thetaDiv);
        }
        /// <summary>
        /// Adds the edges of a bounding box as cylinders.
        /// </summary>
        /// <param name="boundingBox">
        /// The bounding box.
        /// </param>
        /// <param name="diameter">
        /// The diameter of the cylinders.
        /// </param>
        public void AddBoundingBox(System.Windows.Media.Media3D.Rect3D boundingBox, float diameter)
        {
            var p0 = new Vector3((float) boundingBox.X, (float) boundingBox.Y, (float) boundingBox.Z);
            var p1 = new Vector3((float) boundingBox.X, (float) (boundingBox.Y + boundingBox.SizeY), (float) boundingBox.Z);
            var p2 = new Vector3((float) (boundingBox.X + boundingBox.SizeX), (float) (boundingBox.Y + boundingBox.SizeY), (float) boundingBox.Z);
            var p3 = new Vector3((float) (boundingBox.X + boundingBox.SizeX), (float) boundingBox.Y, (float) boundingBox.Z);
            var p4 = new Vector3((float) boundingBox.X, (float) boundingBox.Y, (float) (boundingBox.Z + boundingBox.SizeZ));
            var p5 = new Vector3((float) boundingBox.X, (float) (boundingBox.Y + boundingBox.SizeY), (float) (boundingBox.Z + boundingBox.SizeZ));
            var p6 = new Vector3((float) (boundingBox.X + boundingBox.SizeX), (float) (boundingBox.Y + boundingBox.SizeY),
                (float) (boundingBox.Z + boundingBox.SizeZ));
            var p7 = new Vector3((float) (boundingBox.X + boundingBox.SizeX), (float) boundingBox.Y, (float) (boundingBox.Z + boundingBox.SizeZ));

            Action<Vector3, Vector3> addEdge = (c1, c2) => AddCylinder(c1, c2, diameter, 10);

            addEdge(p0, p1);
            addEdge(p1, p2);
            addEdge(p2, p3);
            addEdge(p3, p0);

            addEdge(p4, p5);
            addEdge(p5, p6);
            addEdge(p6, p7);
            addEdge(p7, p4);

            addEdge(p0, p4);
            addEdge(p1, p5);
            addEdge(p2, p6);
            addEdge(p3, p7);
        }
        /// <summary>
        /// Adds a box aligned with the X, Y and Z axes.
        /// </summary>
        /// <param name="center">
        /// The center point of the box.
        /// </param>
        /// <param name="xlength">
        /// The length of the box along the X axis.
        /// </param>
        /// <param name="ylength">
        /// The length of the box along the Y axis.
        /// </param>
        /// <param name="zlength">
        /// The length of the box along the Z axis.
        /// </param>
        public void AddBox(Vector3 center, float xlength, float ylength, float zlength)
        {
            AddBox(center, xlength, ylength, zlength, BoxFaces.All);
        }
        /// <summary>
        /// Adds a box aligned with the X, Y and Z axes.
        /// </summary>
        /// <param name="rectangle">
        /// The 3-D "rectangle".
        /// </param>
        public void AddBox(System.Windows.Media.Media3D.Rect3D rectangle)
        {
            AddBox(
                new Vector3((float) (rectangle.X + rectangle.SizeX*0.5), (float) (rectangle.Y + rectangle.SizeY*0.5),
                    (float) (rectangle.Z + rectangle.SizeZ*0.5)),
                (float) rectangle.SizeX,
                (float) rectangle.SizeY,
                (float) rectangle.SizeZ,
                BoxFaces.All);
        }
        /// <summary>
        /// Adds a box with the specified faces, aligned with the X, Y and Z axes.
        /// </summary>
        /// <param name="center">
        /// The center point of the box.
        /// </param>
        /// <param name="xlength">
        /// The length of the box along the X axis.
        /// </param>
        /// <param name="ylength">
        /// The length of the box along the Y axis.
        /// </param>
        /// <param name="zlength">
        /// The length of the box along the Z axis.
        /// </param>
        /// <param name="faces">
        /// The faces to include.
        /// </param>
        public void AddBox(Vector3 center, float xlength, float ylength, float zlength, BoxFaces faces)
        {
            AddBox(center, new Vector3(1, 0, 0), new Vector3(0, 1, 0), xlength, ylength, zlength, faces);
        }
        /// <summary>
        /// Adds a box with the specified faces, aligned with the specified axes.
        /// </summary>
        /// <param name="center">The center point of the box.</param>
        /// <param name="x">The x axis.</param>
        /// <param name="y">The y axis.</param>
        /// <param name="xlength">The length of the box along the X axis.</param>
        /// <param name="ylength">The length of the box along the Y axis.</param>
        /// <param name="zlength">The length of the box along the Z axis.</param>
        /// <param name="faces">The faces to include.</param>
        public void AddBox(Vector3 center, Vector3 x, Vector3 y, float xlength, float ylength, float zlength, BoxFaces faces = BoxFaces.All)
        {
            var z = Vector3.Cross(x, y);
            if ((faces & BoxFaces.Front) == BoxFaces.Front)
            {
                AddCubeFace(center, x, z, xlength, ylength, zlength);
            }

            if ((faces & BoxFaces.Back) == BoxFaces.Back)
            {
                AddCubeFace(center, -x, z, xlength, ylength, zlength);
            }

            if ((faces & BoxFaces.Left) == BoxFaces.Left)
            {
                AddCubeFace(center, -y, z, ylength, xlength, zlength);
            }

            if ((faces & BoxFaces.Right) == BoxFaces.Right)
            {
                AddCubeFace(center, y, z, ylength, xlength, zlength);
            }

            if ((faces & BoxFaces.Top) == BoxFaces.Top)
            {
                AddCubeFace(center, z, y, zlength, xlength, ylength);
            }

            if ((faces & BoxFaces.Bottom) == BoxFaces.Bottom)
            {
                AddCubeFace(center, -z, y, zlength, xlength, ylength);
            }
        }
        /// <summary>
        /// Adds a (possibly truncated) cone.
        /// </summary>
        /// <param name="origin">
        /// The origin.
        /// </param>
        /// <param name="direction">
        /// The direction (normalization not required).
        /// </param>
        /// <param name="baseRadius">
        /// The base radius.
        /// </param>
        /// <param name="topRadius">
        /// The top radius.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="baseCap">
        /// Include a base cap if set to <c>true</c> .
        /// </param>
        /// <param name="topCap">
        /// Include the top cap if set to <c>true</c> .
        /// </param>
        /// <param name="thetaDiv">
        /// The number of divisions around the cone.
        /// </param>
        /// <remarks>
        /// See http://en.wikipedia.org/wiki/Cone_(geometry).
        /// </remarks>
        public void AddCone(Vector3 origin, Vector3 direction, float baseRadius, float topRadius, float height, bool baseCap, bool topCap, int thetaDiv)
        {
            var pc = new List<Vector2>();
            var tc = new List<float>();
            if (baseCap)
            {
                pc.Add(new Vector2(0, 0));
                tc.Add(0);
            }

            pc.Add(new Vector2(0, baseRadius));
            tc.Add(1);
            pc.Add(new Vector2(height, topRadius));
            tc.Add(0);
            if (topCap)
            {
                pc.Add(new Vector2(height, 0));
                tc.Add(1);
            }

            AddRevolvedGeometry(pc, tc, origin, direction, thetaDiv);
        }
        /// <summary>
        /// Adds a cone.
        /// </summary>
        /// <param name="origin">The origin point.</param>
        /// <param name="apex">The apex point.</param>
        /// <param name="baseRadius">The base radius.</param>
        /// <param name="baseCap">
        /// Include a base cap if set to <c>true</c> .
        /// </param>
        /// <param name="thetaDiv">The theta div.</param>
        public void AddCone(Vector3 origin, Vector3 apex, float baseRadius, bool baseCap, int thetaDiv)
        {
            var dir = apex - origin;
            AddCone(origin, dir, baseRadius, 0, dir.Length, baseCap, false, thetaDiv);
        }
        /// <summary>
        /// Adds a cube face.
        /// </summary>
        /// <param name="center">
        /// The center of the cube.
        /// </param>
        /// <param name="normal">
        /// The normal vector for the face.
        /// </param>
        /// <param name="up">
        /// The up vector for the face.
        /// </param>
        /// <param name="dist">
        /// The distance from the center of the cube to the face.
        /// </param>
        /// <param name="width">
        /// The width of the face.
        /// </param>
        /// <param name="height">
        /// The height of the face.
        /// </param>
        public void AddCubeFace(Vector3 center, Vector3 normal, Vector3 up, float dist, float width, float height)
        {
            var right = Vector3.Cross(normal, up);
            var n = normal*dist/2;
            up *= height/2;
            right *= width/2;
            var p1 = center + n - up - right;
            var p2 = center + n - up + right;
            var p3 = center + n + up + right;
            var p4 = center + n + up - right;

            int i0 = Positions.Count;
            Positions.Add(p1);
            Positions.Add(p2);
            Positions.Add(p3);
            Positions.Add(p4);
            if (Normals != null)
            {
                Normals.Add(normal);
                Normals.Add(normal);
                Normals.Add(normal);
                Normals.Add(normal);
            }

            if (TextureCoordinates != null)
            {
                TextureCoordinates.Add(new Vector2(1, 1));
                TextureCoordinates.Add(new Vector2(0, 1));
                TextureCoordinates.Add(new Vector2(0, 0));
                TextureCoordinates.Add(new Vector2(1, 0));
            }

            TriangleIndices.Add(i0 + 2);
            TriangleIndices.Add(i0 + 1);
            TriangleIndices.Add(i0 + 0);
            TriangleIndices.Add(i0 + 0);
            TriangleIndices.Add(i0 + 3);
            TriangleIndices.Add(i0 + 2);
        }
        /// <summary>
        /// Adds a cylinder to the mesh.
        /// </summary>
        /// <param name="p1">
        /// The first point.
        /// </param>
        /// <param name="p2">
        /// The second point.
        /// </param>
        /// <param name="diameter">
        /// The diameters.
        /// </param>
        /// <param name="thetaDiv">
        /// The number of divisions around the cylinder.
        /// </param>
        /// <remarks>
        /// See http://en.wikipedia.org/wiki/Cylinder_(geometry).
        /// </remarks>
        public void AddCylinder(Vector3 p1, Vector3 p2, float diameter, int thetaDiv)
        {
            Vector3 n = p2 - p1;
            float l = n.Length;
            n.Normalize();
            AddCone(p1, n, diameter/2, diameter/2, l, false, false, thetaDiv);
        }
        /// <summary>
        /// Adds a collection of edges as cylinders.
        /// </summary>
        /// <param name="points">
        /// The points.
        /// </param>
        /// <param name="edges">
        /// The edge indices.
        /// </param>
        /// <param name="diameter">
        /// The diameter of the cylinders.
        /// </param>
        /// <param name="thetaDiv">
        /// The number of divisions around the cylinders.
        /// </param>
        public void AddEdges(IList<Vector3> points, IList<int> edges, float diameter, int thetaDiv)
        {
            for (int i = 0; i < edges.Count - 1; i += 2)
            {
                AddCylinder(points[edges[i]], points[edges[i + 1]], diameter, thetaDiv);
            }
        }
        /// <summary>
        /// Adds an extruded surface of the specified curve.
        /// </summary>
        /// <param name="points">
        /// The 2D points describing the curve to extrude.
        /// </param>
        /// <param name="xaxis">
        /// The x-axis.
        /// </param>
        /// <param name="p0">
        /// The start origin of the extruded surface.
        /// </param>
        /// <param name="p1">
        /// The end origin of the extruded surface.
        /// </param>
        /// <remarks>
        /// The y-axis is determined by the cross product between the specified x-axis and the p1-origin vector.
        /// </remarks>
        public void AddExtrudedGeometry(IList<Vector2> points, Vector3 xaxis, Vector3 p0, Vector3 p1)
        {
            var ydirection = Vector3.Cross(xaxis, p1 - p0);
            ydirection.Normalize();
            xaxis.Normalize();

            int index0 = Positions.Count;
            int np = 2*points.Count;
            foreach (var p in points)
            {
                var v = xaxis*p.X + ydirection*p.Y;
                Positions.Add(p0 + v);
                Positions.Add(p1 + v);
                v.Normalize();
                if (Normals != null)
                {
                    Normals.Add(v);
                    Normals.Add(v);
                }

                if (TextureCoordinates != null)
                {
                    TextureCoordinates.Add(new Vector2(0, 0));
                    TextureCoordinates.Add(new Vector2(1, 0));
                }

                int i1 = index0 + 1;
                int i2 = (index0 + 2)%np;
                int i3 = (index0 + 2)%np + 1;

                TriangleIndices.Add(i1);
                TriangleIndices.Add(i2);
                TriangleIndices.Add(index0);

                TriangleIndices.Add(i1);
                TriangleIndices.Add(i3);
                TriangleIndices.Add(i2);
            }
        }
        /// <summary>
        /// Adds an extruded surface of the specified line segments.
        /// </summary>
        /// <param name="points">The 2D points describing the line segments to extrude. The number of points must be even.</param>
        /// <param name="axisX">The x-axis.</param>
        /// <param name="p0">The start origin of the extruded surface.</param>
        /// <param name="p1">The end origin of the extruded surface.</param>
        /// <remarks>The y-axis is determined by the cross product between the specified x-axis and the p1-origin vector.</remarks>
        public void AddExtrudedSegments(IList<Vector2> points, Vector3 axisX, Vector3 p0, Vector3 p1)
        {
            if (points.Count%2 != 0)
            {
                throw new InvalidOperationException("The number of points should be even.");
            }

            var axisY = Vector3.Cross(axisX, p1 - p0);
            axisY.Normalize();
            axisX.Normalize();
            int index0 = Positions.Count;

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var d = axisX*p.X + axisY*p.Y;
                Positions.Add(p0 + d);
                Positions.Add(p1 + d);

                if (Normals != null)
                {
                    d.Normalize();
                    Normals.Add(d);
                    Normals.Add(d);
                }

                if (TextureCoordinates != null)
                {
                    var v = i/(points.Count - 1f);
                    TextureCoordinates.Add(new Vector2(0, v));
                    TextureCoordinates.Add(new Vector2(1, v));
                }
            }

            int n = points.Count - 1;
            for (int i = 0; i < n; i++)
            {
                int i0 = index0 + i*2;
                int i1 = i0 + 1;
                int i2 = i0 + 3;
                int i3 = i0 + 2;

                TriangleIndices.Add(i0);
                TriangleIndices.Add(i1);
                TriangleIndices.Add(i2);

                TriangleIndices.Add(i2);
                TriangleIndices.Add(i3);
                TriangleIndices.Add(i0);
            }
        }
        /// <summary>
        /// Adds a lofted surface.
        /// </summary>
        /// <param name="positionsList">
        /// List of lofting sections.
        /// </param>
        /// <param name="normalList">
        /// The normal list.
        /// </param>
        /// <param name="textureCoordinateList">
        /// The texture coordinate list.
        /// </param>
        /// <remarks>
        /// See http://en.wikipedia.org/wiki/Loft_(3D).
        /// </remarks>
        public void AddLoftedGeometry( IList<IList<Vector3>> positionsList, IList<IList<Vector3>> normalList, IList<IList<Vector2>> textureCoordinateList)
        {
            int index0 = Positions.Count;
            int n = -1;
            for (int i = 0; i < positionsList.Count; i++)
            {
                var pc = positionsList[i];

                // check that all curves have same number of points
                if (n == -1)
                {
                    n = pc.Count;
                }

                if (pc.Count != n)
                {
                    throw new InvalidOperationException(AllCurvesShouldHaveTheSameNumberOfPoints);
                }

                // add the points
                foreach (var p in pc)
                {
                    Positions.Add(p);
                }

                // add normals
                if (Normals != null && normalList != null)
                {
                    var nc = normalList[i];
                    foreach (var normal in nc)
                    {
                        Normals.Add(normal);
                    }
                }

                // add texcoords
                if (TextureCoordinates != null && textureCoordinateList != null)
                {
                    var tc = textureCoordinateList[i];
                    foreach (var t in tc)
                    {
                        TextureCoordinates.Add(t);
                    }
                }
            }

            for (int i = 0; i + 1 < positionsList.Count; i++)
            {
                for (int j = 0; j + 1 < n; j++)
                {
                    int i0 = index0 + i*n + j;
                    int i1 = i0 + n;
                    int i2 = i1 + 1;
                    int i3 = i0 + 1;
                    TriangleIndices.Add(i0);
                    TriangleIndices.Add(i1);
                    TriangleIndices.Add(i2);

                    TriangleIndices.Add(i2);
                    TriangleIndices.Add(i3);
                    TriangleIndices.Add(i0);
                }
            }
        }
        /// <summary>
        /// Adds a single node.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="normal">
        /// The normal.
        /// </param>
        /// <param name="textureCoordinate">
        /// The texture coordinate.
        /// </param>
        public void AddNode(Vector3 position, Vector3 normal, Vector2 textureCoordinate)
        {
            Positions.Add(position);

            Normals?.Add(normal);

            TextureCoordinates?.Add(textureCoordinate);
        }
        /// <summary>
        /// Adds a (possibly hollow) pipe.
        /// </summary>
        /// <param name="point1">
        /// The start point.
        /// </param>
        /// <param name="point2">
        /// The end point.
        /// </param>
        /// <param name="innerDiameter">
        /// The inner diameter.
        /// </param>
        /// <param name="diameter">
        /// The outer diameter.
        /// </param>
        /// <param name="thetaDiv">
        /// The number of divisions around the pipe.
        /// </param>
        public void AddPipe(Vector3 point1, Vector3 point2, float innerDiameter, float diameter, int thetaDiv)
        {
            var dir = point2 - point1;

            float height = dir.Length;
            dir.Normalize();

            var pc = new List<Vector2>
            {
                new Vector2(0, innerDiameter/2),
                new Vector2(0, diameter/2),
                new Vector2(height, diameter/2),
                new Vector2(height, innerDiameter/2)
            };

            var tc = new List<float> {1, 0, 1, 0};

            if (innerDiameter > 0)
            {
                // Add the inner surface
                pc.Add(new Vector2(0, innerDiameter/2));
                tc.Add(1);
            }

            AddRevolvedGeometry(pc, tc, point1, dir, thetaDiv);
        }
        /// <summary>
        /// Adds a polygon.
        /// </summary>
        /// <param name="points">
        /// The points of the polygon.
        /// </param>
        /// <remarks>
        /// If the number of points is greater than 4, a triangle fan is used.
        /// </remarks>
        public void AddPolygon(IList<Vector3> points)
        {
            switch (points.Count)
            {
            case 3:
                AddTriangle(points[0], points[1], points[2]);
                break;
            case 4:
                AddQuad(points[0], points[1], points[2], points[3]);
                break;
            default:
                AddTriangleFan(points);
                break;
            }
        }
        /// <summary>
        /// Adds a pyramid.
        /// </summary>
        /// <param name="center">
        /// The center.
        /// </param>
        /// <param name="sideLength">
        /// Length of the sides of the pyramid.
        /// </param>
        /// <param name="height">
        /// The height of the pyramid.
        /// </param>
        /// <remarks>
        /// See http://en.wikipedia.org/wiki/Pyramid_(geometry).
        /// </remarks>
        public void AddPyramid(Vector3 center, float sideLength, float height)
        {
            AddPyramid(center, Vector3.UnitX, Vector3.UnitZ, sideLength, height);
        }
        /// <summary>
        /// Adds a pyramid.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="normal">The normal vector (normalized).</param>
        /// <param name="up">The 'up' vector (normalized).</param>
        /// <param name="sideLength">Length of the sides of the pyramid.</param>
        /// <param name="height">The height of the pyramid.</param>
        public void AddPyramid(Vector3 center, Vector3 normal, Vector3 up, float sideLength, float height)
        {
            var right = Vector3.Cross(normal, up);
            var n = normal*sideLength/2;
            up *= height;
            right *= sideLength/2;

            var p1 = center - n - right;
            var p2 = center - n + right;
            var p3 = center + n + right;
            var p4 = center + n - right;
            var p5 = center + up;

            AddTriangle(p1, p2, p5);
            AddTriangle(p2, p3, p5);
            AddTriangle(p3, p4, p5);
            AddTriangle(p4, p1, p5);
        }
        /// <summary>
        /// Adds an octahedron.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="normal">The normal vector.</param>
        /// <param name="up">The up vector.</param>
        /// <param name="sideLength">Length of the side.</param>
        /// <param name="height">The half height of the octahedron.</param>
        /// <remarks>See <a href="http://en.wikipedia.org/wiki/Octahedron">Octahedron</a>.</remarks>
        public void AddOctahedron(Vector3 center, Vector3 normal, Vector3 up, float sideLength, float height)
        {
            var right = Vector3.Cross(normal, up);
            var n = normal*sideLength/2;
            up *= height/2;
            right *= sideLength/2;

            var p1 = center - n - up - right;
            var p2 = center - n - up + right;
            var p3 = center + n - up + right;
            var p4 = center + n - up - right;
            var p5 = center + up;
            var p6 = center - up;

            AddTriangle(p1, p2, p5);
            AddTriangle(p2, p3, p5);
            AddTriangle(p3, p4, p5);
            AddTriangle(p4, p1, p5);

            AddTriangle(p2, p1, p6);
            AddTriangle(p3, p2, p6);
            AddTriangle(p4, p3, p6);
            AddTriangle(p1, p4, p6);
        }
        /// <summary>
        /// Adds a quadrilateral polygon.
        /// </summary>
        /// <param name="p0">
        /// The first point.
        /// </param>
        /// <param name="p1">
        /// The second point.
        /// </param>
        /// <param name="p2">
        /// The third point.
        /// </param>
        /// <param name="p3">
        /// The fourth point.
        /// </param>
        /// <remarks>
        /// See http://en.wikipedia.org/wiki/Quadrilateral.
        /// </remarks>
        public void AddQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            //// The nodes are arranged in counter-clockwise order
            //// p3               p2
            //// +---------------+
            //// |               |
            //// |               |
            //// +---------------+
            //// origin               p1
            var uv0 = new Vector2(0, 0);
            var uv1 = new Vector2(1, 0);
            var uv2 = new Vector2(1, 1);
            var uv3 = new Vector2(0, 1);
            AddQuad(p0, p1, p2, p3, uv0, uv1, uv2, uv3);
        }
        /// <summary>
        /// Adds a quadrilateral polygon.
        /// </summary>
        /// <param name="p0">
        /// The first point.
        /// </param>
        /// <param name="p1">
        /// The second point.
        /// </param>
        /// <param name="p2">
        /// The third point.
        /// </param>
        /// <param name="p3">
        /// The fourth point.
        /// </param>
        /// <param name="uv0">
        /// The first texture coordinate.
        /// </param>
        /// <param name="uv1">
        /// The second texture coordinate.
        /// </param>
        /// <param name="uv2">
        /// The third texture coordinate.
        /// </param>
        /// <param name="uv3">
        /// The fourth texture coordinate.
        /// </param>
        /// <remarks>
        /// See http://en.wikipedia.org/wiki/Quadrilateral.
        /// </remarks>
        public void AddQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            //// The nodes are arranged in counter-clockwise order
            //// p3               p2
            //// +---------------+
            //// |               |
            //// |               |
            //// +---------------+
            //// origin               p1
            int i0 = Positions.Count;

            Positions.Add(p0);
            Positions.Add(p1);
            Positions.Add(p2);
            Positions.Add(p3);

            if (TextureCoordinates != null)
            {
                TextureCoordinates.Add(uv0);
                TextureCoordinates.Add(uv1);
                TextureCoordinates.Add(uv2);
                TextureCoordinates.Add(uv3);
            }

            if (Normals != null)
            {
                var w = Vector3.Cross(p1 - p0, p3 - p0);
                w.Normalize();
                Normals.Add(w);
                Normals.Add(w);
                Normals.Add(w);
                Normals.Add(w);
            }

            TriangleIndices.Add(i0 + 0);
            TriangleIndices.Add(i0 + 1);
            TriangleIndices.Add(i0 + 2);

            TriangleIndices.Add(i0 + 2);
            TriangleIndices.Add(i0 + 3);
            TriangleIndices.Add(i0 + 0);
        }
        /// <summary>
        /// Adds a list of quadrilateral polygons.
        /// </summary>
        /// <param name="quadPositions">
        /// The points.
        /// </param>
        /// <param name="quadNormals">
        /// The normal vectors.
        /// </param>
        /// <param name="quadTextureCoordinates">
        /// The texture coordinates.
        /// </param>
        public void AddQuads( IList<Vector3> quadPositions, IList<Vector3> quadNormals, IList<Vector2> quadTextureCoordinates)
        {
            if (quadPositions == null)
            {
                throw new ArgumentNullException(nameof(quadPositions));
            }

            if (Normals != null && quadNormals == null)
            {
                throw new ArgumentNullException(nameof(quadNormals));
            }

            if (TextureCoordinates != null && quadTextureCoordinates == null)
            {
                throw new ArgumentNullException(nameof(quadTextureCoordinates));
            }

            if (quadNormals != null && quadNormals.Count != quadPositions.Count)
            {
                throw new InvalidOperationException(WrongNumberOfNormals);
            }

            if (quadTextureCoordinates != null && quadTextureCoordinates.Count != quadPositions.Count)
            {
                throw new InvalidOperationException(WrongNumberOfTextureCoordinates);
            }

            Debug.Assert(quadPositions.Count > 0 && quadPositions.Count%4 == 0, "Wrong number of positions.");

            int index0 = Positions.Count;
            foreach (var p in quadPositions)
            {
                Positions.Add(p);
            }

            if (TextureCoordinates != null && quadTextureCoordinates != null)
            {
                foreach (var tc in quadTextureCoordinates)
                {
                    TextureCoordinates.Add(tc);
                }
            }

            if (Normals != null && quadNormals != null)
            {
                foreach (var n in quadNormals)
                {
                    Normals.Add(n);
                }
            }

            int indexEnd = Positions.Count;
            for (int i = index0; i + 3 < indexEnd; i++)
            {
                TriangleIndices.Add(i);
                TriangleIndices.Add(i + 1);
                TriangleIndices.Add(i + 2);

                TriangleIndices.Add(i + 2);
                TriangleIndices.Add(i + 3);
                TriangleIndices.Add(i);
            }
        }
        /// <summary>
        /// Adds a rectangular mesh (m x n points).
        /// </summary>
        /// <param name="points">
        /// The one-dimensional array of points. The points are stored row-by-row.
        /// </param>
        /// <param name="columns">
        /// The number of columns in the rectangular mesh.
        /// </param>
        public void AddRectangularMesh(IList<Vector3> points, int columns)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            int index0 = Positions.Count;

            foreach (var pt in points)
            {
                Positions.Add(pt);
            }

            int rows = points.Count/columns;

            AddRectangularMeshTriangleIndices(index0, rows, columns);
            if (Normals != null)
            {
                AddRectangularMeshNormals(index0, rows, columns);
            }

            if (TextureCoordinates != null)
            {
                AddRectangularMeshTextureCoordinates(rows, columns);
            }
        }
        /// <summary>
        /// Adds a rectangular mesh defined by a two-dimensional array of points.
        /// </summary>
        /// <param name="points">
        /// The points.
        /// </param>
        /// <param name="texCoords">
        /// The texture coordinates (optional).
        /// </param>
        /// <param name="closed0">
        /// set to <c>true</c> if the mesh is closed in the first dimension.
        /// </param>
        /// <param name="closed1">
        /// set to <c>true</c> if the mesh is closed in the second dimension.
        /// </param>
        public void AddRectangularMesh(Vector3[,] points, Vector2[,] texCoords = null, bool closed0 = false, bool closed1 = false)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            int rows = points.GetUpperBound(0) + 1;
            int columns = points.GetUpperBound(1) + 1;
            int index0 = Positions.Count;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    Positions.Add(points[i, j]);
                }
            }

            AddRectangularMeshTriangleIndices(index0, rows, columns, closed0, closed1);

            if (Normals != null)
            {
                AddRectangularMeshNormals(index0, rows, columns);
            }

            if (TextureCoordinates != null)
            {
                if (texCoords != null)
                {
                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < columns; j++)
                        {
                            TextureCoordinates.Add(texCoords[i, j]);
                        }
                    }
                }
                else
                {
                    AddRectangularMeshTextureCoordinates(rows, columns);
                }
            }
        }
        /// <summary>
        /// Adds a regular icosahedron.
        /// </summary>
        /// <param name="center">
        /// The center.
        /// </param>
        /// <param name="radius">
        /// The radius.
        /// </param>
        /// <param name="shareVertices">
        /// Share vertices if set to <c>true</c> .
        /// </param>
        /// <remarks>
        /// See <a href="http://en.wikipedia.org/wiki/Icosahedron">Wikipedia</a> and <a href="http://www.gamedev.net/community/forums/topic.asp?topic_id=283350">link</a>.
        /// </remarks>
        public void AddRegularIcosahedron(Vector3 center, float radius, bool shareVertices)
        {
            float a = (float) Math.Sqrt(2.0/(5.0 + Math.Sqrt(5.0)));

            float b = (float) Math.Sqrt(2.0/(5.0 - Math.Sqrt(5.0)));

            var icosahedronIndices = new[]
            {
                1, 4, 0, 4, 9, 0, 4, 5, 9, 8, 5, 4, 1, 8, 4, 1, 10, 8, 10, 3, 8, 8, 3, 5, 3, 2, 5, 3, 7, 2, 3, 10, 7,
                10, 6, 7, 6, 11, 7, 6, 0, 11, 6, 1, 0, 10, 1, 6, 11, 0, 9, 2, 11, 9, 5, 2, 9, 11, 2, 7
            };

            var icosahedronVertices = new[]
            {
                new Vector3(-a, 0, b), new Vector3(a, 0, b), new Vector3(-a, 0, -b), new Vector3(a, 0, -b),
                new Vector3(0, b, a), new Vector3(0, b, -a), new Vector3(0, -b, a), new Vector3(0, -b, -a),
                new Vector3(b, a, 0), new Vector3(-b, a, 0), new Vector3(b, -a, 0), new Vector3(-b, -a, 0)
            };

            if (shareVertices)
            {
                int index0 = Positions.Count;
                foreach (var v in icosahedronVertices)
                {
                    Positions.Add(center + v*radius);
                }

                foreach (int i in icosahedronIndices)
                {
                    TriangleIndices.Add(index0 + i);
                }
            }
            else
            {
                for (int i = 0; i + 2 < icosahedronIndices.Length; i += 3)
                {
                    AddTriangle(
                        center + icosahedronVertices[icosahedronIndices[i]]*radius,
                        center + icosahedronVertices[icosahedronIndices[i + 1]]*radius,
                        center + icosahedronVertices[icosahedronIndices[i + 2]]*radius);
                }
            }
        }
        /// <summary>
        /// Adds a surface of revolution.
        /// </summary>
        /// <param name="points">The points (x coordinates are distance from the origin along the axis of revolution, y coordinates are radius, )</param>
        /// <param name="textureValues">The v texture coordinates, one for each point in the <paramref name="points" /> list.</param>
        /// <param name="origin">The origin of the revolution axis.</param>
        /// <param name="direction">The direction of the revolution axis.</param>
        /// <param name="thetaDiv">The number of divisions around the mesh.</param>
        /// <remarks>
        /// See http://en.wikipedia.org/wiki/Surface_of_revolution.
        /// </remarks>
        public void AddRevolvedGeometry(IList<Vector2> points, IList<float> textureValues, Vector3 origin, Vector3 direction, int thetaDiv)
        {
            direction.Normalize();

            // Find two unit vectors orthogonal to the specified direction
            var u = direction.FindAnyPerpendicular();
            var v = Vector3.Cross(direction, u);

            u.Normalize();
            v.Normalize();

            var circle = GetCircle(thetaDiv);

            int index0 = Positions.Count;
            int n = points.Count;

            int totalNodes = (points.Count - 1)*2*thetaDiv;
            int rowNodes = (points.Count - 1)*2;

            for (int i = 0; i < thetaDiv; i++)
            {
                var w = v*circle[i].X + u*circle[i].Y;

                for (int j = 0; j + 1 < n; j++)
                {
                    // Add segment
                    var q1 = origin + direction*points[j].X + w*points[j].Y;
                    var q2 = origin + direction*points[j + 1].X + w*points[j + 1].Y;

                    // TODO: should not add segment if q1==q2 (corner point)
                    // const float eps = 1e-6;
                    // if (Vector3.Subtract(q1, q2).LengthSquared < eps)
                    // continue;
                    Positions.Add(q1);
                    Positions.Add(q2);

                    if (Normals != null)
                    {
                        float tx = points[j + 1].X - points[j].X;
                        float ty = points[j + 1].Y - points[j].Y;
                        var normal = -direction*ty + w*tx;
                        normal.Normalize();

                        Normals.Add(normal);
                        Normals.Add(normal);
                    }

                    if (TextureCoordinates != null)
                    {
                        TextureCoordinates.Add(new Vector2((float) i/(thetaDiv - 1),
                            textureValues?[j] ?? (float) j/(n - 1)));
                        TextureCoordinates.Add(new Vector2((float) i/(thetaDiv - 1),
                            textureValues?[j + 1] ?? (float) (j + 1)/(n - 1)));
                    }

                    int i0 = index0 + i*rowNodes + j*2;
                    int i1 = i0 + 1;
                    int i2 = index0 + ((i + 1)*rowNodes + j*2)%totalNodes;
                    int i3 = i2 + 1;

                    TriangleIndices.Add(i1);
                    TriangleIndices.Add(i0);
                    TriangleIndices.Add(i2);

                    TriangleIndices.Add(i1);
                    TriangleIndices.Add(i2);
                    TriangleIndices.Add(i3);
                }
            }
        }
        /// <summary>
        /// Adds a sphere.
        /// </summary>
        /// <param name="center">
        /// The center of the sphere.
        /// </param>
        /// <param name="radius">
        /// The radius of the sphere.
        /// </param>
        /// <param name="thetaDiv">
        /// The number of divisions around the sphere.
        /// </param>
        /// <param name="phiDiv">
        /// The number of divisions from top to bottom of the sphere.
        /// </param>
        public void AddSphere(Vector3 center, float radius, int thetaDiv = 20, int phiDiv = 10)
        {
            AddEllipsoid(center, radius, radius, radius, thetaDiv, phiDiv);
        }
        /// <summary>
        /// Adds an ellipsoid.
        /// </summary>
        /// <param name="center">
        /// The center of the ellipsoid.
        /// </param>
        /// <param name="radiusx">
        /// The x radius of the ellipsoid.
        /// </param>
        /// <param name="radiusy">
        /// The y radius of the ellipsoid.
        /// </param>
        /// <param name="radiusz">
        /// The z radius of the ellipsoid.
        /// </param>
        /// <param name="thetaDiv">
        /// The number of divisions around the ellipsoid.
        /// </param>
        /// <param name="phiDiv">
        /// The number of divisions from top to bottom of the ellipsoid.
        /// </param>
        public void AddEllipsoid(Vector3 center, float radiusx, float radiusy, float radiusz, int thetaDiv = 20, int phiDiv = 10)
        {
            int index0 = Positions.Count;
            float dt = (float) (2*Math.PI/thetaDiv);
            float dp = (float) (Math.PI/phiDiv);

            for (int pi = 0; pi <= phiDiv; pi++)
            {
                float phi = pi*dp;

                for (int ti = 0; ti <= thetaDiv; ti++)
                {
                    // we want to start the mesh on the x axis
                    float theta = ti*dt;

                    // Spherical coordinates
                    // http://mathworld.wolfram.com/SphericalCoordinates.html
                    float x = (float) (Math.Cos(theta)*Math.Sin(phi));
                    float y = (float) (Math.Sin(theta)*Math.Sin(phi));
                    float z = (float) Math.Cos(phi);

                    var p = new Vector3(center.X + radiusx*x, center.Y + radiusy*y, center.Z + radiusz*z);
                    Positions.Add(p);

                    if (Normals != null)
                    {
                        var n = new Vector3(x, y, z);
                        Normals.Add(n);
                    }

                    if (TextureCoordinates != null)
                    {
                        var uv = new Vector2((float) (theta/(2*Math.PI)), (float) (phi/Math.PI));
                        TextureCoordinates.Add(uv);
                    }
                }
            }

            AddRectangularMeshTriangleIndices(index0, phiDiv + 1, thetaDiv + 1, true);
        }
        /// <summary>
        /// Adds a triangle.
        /// </summary>
        /// <param name="p0">
        /// The first point.
        /// </param>
        /// <param name="p1">
        /// The second point.
        /// </param>
        /// <param name="p2">
        /// The third point.
        /// </param>
        public void AddTriangle(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var uv0 = new Vector2(0, 0);
            var uv1 = new Vector2(1, 0);
            var uv2 = new Vector2(0, 1);
            AddTriangle(p0, p1, p2, uv0, uv1, uv2);
        }
        /// <summary>
        /// Adds a triangle.
        /// </summary>
        /// <param name="p0">
        /// The first point.
        /// </param>
        /// <param name="p1">
        /// The second point.
        /// </param>
        /// <param name="p2">
        /// The third point.
        /// </param>
        /// <param name="uv0">
        /// The first texture coordinate.
        /// </param>
        /// <param name="uv1">
        /// The second texture coordinate.
        /// </param>
        /// <param name="uv2">
        /// The third texture coordinate.
        /// </param>
        public void AddTriangle(Vector3 p0, Vector3 p1, Vector3 p2, Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            int i0 = Positions.Count;

            Positions.Add(p0);
            Positions.Add(p1);
            Positions.Add(p2);

            if (TextureCoordinates != null)
            {
                TextureCoordinates.Add(uv0);
                TextureCoordinates.Add(uv1);
                TextureCoordinates.Add(uv2);
            }

            if (Normals != null)
            {
                var w = Vector3.Cross(p1 - p0, p2 - p0);
                w.Normalize();
                Normals.Add(w);
                Normals.Add(w);
                Normals.Add(w);
            }

            TriangleIndices.Add(i0 + 0);
            TriangleIndices.Add(i0 + 1);
            TriangleIndices.Add(i0 + 2);
        }
        /// <summary>
        /// Adds a triangle fan.
        /// </summary>
        /// <param name="vertices">
        /// The vertex indices of the triangle fan.
        /// </param>
        public void AddTriangleFan(IList<int> vertices)
        {
            for (int i = 0; i + 2 < vertices.Count; i++)
            {
                TriangleIndices.Add(vertices[0]);
                TriangleIndices.Add(vertices[i + 1]);
                TriangleIndices.Add(vertices[i + 2]);
            }
        }
        /// <summary>
        /// Adds a triangle fan to the mesh
        /// </summary>
        /// <param name="fanPositions">
        /// The points of the triangle fan.
        /// </param>
        /// <param name="fanNormals">
        /// The normal vectors of the triangle fan.
        /// </param>
        /// <param name="fanTextureCoordinates">
        /// The texture coordinates of the triangle fan.
        /// </param>
        public void AddTriangleFan(IList<Vector3> fanPositions, IList<Vector3> fanNormals = null, IList<Vector2> fanTextureCoordinates = null)
        {
            if (Positions == null)
            {
                throw new ArgumentNullException(nameof(fanPositions));
            }

            if (Normals != null && fanNormals == null)
            {
                throw new ArgumentNullException(nameof(fanNormals));
            }

            if (TextureCoordinates != null && fanTextureCoordinates == null)
            {
                throw new ArgumentNullException(nameof(fanTextureCoordinates));
            }

            int index0 = Positions.Count;
            foreach (var p in fanPositions)
            {
                Positions.Add(p);
            }

            if (TextureCoordinates != null && fanTextureCoordinates != null)
            {
                foreach (var tc in fanTextureCoordinates)
                {
                    TextureCoordinates.Add(tc);
                }
            }

            if (Normals != null && fanNormals != null)
            {
                foreach (var n in fanNormals)
                {
                    Normals.Add(n);
                }
            }

            int indexEnd = Positions.Count;
            for (int i = index0; i + 2 < indexEnd; i++)
            {
                TriangleIndices.Add(index0);
                TriangleIndices.Add(i + 1);
                TriangleIndices.Add(i + 2);
            }
        }
        /// <summary>
        /// Adds a triangle strip to the mesh.
        /// </summary>
        /// <param name="stripPositions">
        /// The points of the triangle strip.
        /// </param>
        /// <param name="stripNormals">
        /// The normal vectors of the triangle strip.
        /// </param>
        /// <param name="stripTextureCoordinates">
        /// The texture coordinates of the triangle strip.
        /// </param>
        /// <remarks>
        /// See http://en.wikipedia.org/wiki/Triangle_strip.
        /// </remarks>
        public void AddTriangleStrip(IList<Vector3> stripPositions, IList<Vector3> stripNormals = null, IList<Vector2> stripTextureCoordinates = null)
        {
            if (stripPositions == null)
            {
                throw new ArgumentNullException(nameof(stripPositions));
            }

            if (Normals != null && stripNormals == null)
            {
                throw new ArgumentNullException(nameof(stripNormals));
            }

            if (TextureCoordinates != null && stripTextureCoordinates == null)
            {
                throw new ArgumentNullException(nameof(stripTextureCoordinates));
            }

            if (stripNormals != null && stripNormals.Count != stripPositions.Count)
            {
                throw new InvalidOperationException(WrongNumberOfNormals);
            }

            if (stripTextureCoordinates != null && stripTextureCoordinates.Count != stripPositions.Count)
            {
                throw new InvalidOperationException(WrongNumberOfTextureCoordinates);
            }

            int index0 = Positions.Count;
            for (int i = 0; i < stripPositions.Count; i++)
            {
                Positions.Add(stripPositions[i]);
                if (Normals != null && stripNormals != null)
                {
                    Normals.Add(stripNormals[i]);
                }

                if (TextureCoordinates != null && stripTextureCoordinates != null)
                {
                    TextureCoordinates.Add(stripTextureCoordinates[i]);
                }
            }

            int indexEnd = Positions.Count;
            for (int i = index0; i + 2 < indexEnd; i += 2)
            {
                TriangleIndices.Add(i);
                TriangleIndices.Add(i + 1);
                TriangleIndices.Add(i + 2);

                if (i + 3 < indexEnd)
                {
                    TriangleIndices.Add(i + 1);
                    TriangleIndices.Add(i + 3);
                    TriangleIndices.Add(i + 2);
                }
            }
        }
        /// <summary>
        /// Adds a polygon specified by vertex index (uses a triangle fan).
        /// </summary>
        /// <param name="vertexIndices">The vertex indices.</param>
        public void AddPolygon(IList<int> vertexIndices)
        {
            int n = vertexIndices.Count;
            for (int i = 0; i + 2 < n; i++)
            {
                TriangleIndices.Add(vertexIndices[0]);
                TriangleIndices.Add(vertexIndices[i + 1]);
                TriangleIndices.Add(vertexIndices[i + 2]);
            }
        }
        /// <summary>
        /// Adds a list of triangles.
        /// </summary>
        /// <param name="trianglePositions">
        /// The points (the number of points must be a multiple of 3).
        /// </param>
        /// <param name="triangleNormals">
        /// The normal vectors (corresponding to the points).
        /// </param>
        /// <param name="triangleTextureCoordinates">
        /// The texture coordinates (corresponding to the points).
        /// </param>
        public void AddTriangles(IList<Vector3> trianglePositions, IList<Vector3> triangleNormals = null, IList<Vector2> triangleTextureCoordinates = null)
        {
            if (trianglePositions == null)
            {
                throw new ArgumentNullException(nameof(trianglePositions));
            }

            if (Normals != null && triangleNormals == null)
            {
                throw new ArgumentNullException(nameof(triangleNormals));
            }

            if (TextureCoordinates != null && triangleTextureCoordinates == null)
            {
                throw new ArgumentNullException(nameof(triangleTextureCoordinates));
            }

            if (trianglePositions.Count%3 != 0)
            {
                throw new InvalidOperationException(WrongNumberOfPositions);
            }

            if (triangleNormals != null && triangleNormals.Count != trianglePositions.Count)
            {
                throw new InvalidOperationException(WrongNumberOfNormals);
            }

            if (triangleTextureCoordinates != null && triangleTextureCoordinates.Count != trianglePositions.Count)
            {
                throw new InvalidOperationException(WrongNumberOfTextureCoordinates);
            }

            int index0 = Positions.Count;
            foreach (var p in trianglePositions)
            {
                Positions.Add(p);
            }

            if (TextureCoordinates != null && triangleTextureCoordinates != null)
            {
                foreach (var tc in triangleTextureCoordinates)
                {
                    TextureCoordinates.Add(tc);
                }
            }

            if (Normals != null && triangleNormals != null)
            {
                foreach (var n in triangleNormals)
                {
                    Normals.Add(n);
                }
            }

            int indexEnd = Positions.Count;
            for (int i = index0; i < indexEnd; i++)
            {
                TriangleIndices.Add(i);
            }
        }
        /// <summary>
        /// Adds a tube with a custom section.
        /// </summary>
        /// <param name="path">A list of points defining the centers of the tube.</param>
        /// <param name="angles">The rotation of the section as it moves along the path</param>
        /// <param name="values">The texture coordinate X values (optional).</param>
        /// <param name="diameters">The diameters (optional).</param>
        /// <param name="section">The section to extrude along the tube path.</param>
        /// <param name="sectionXAxis">The initial alignment of the x-axis of the section into the
        /// 3D viewport</param>
        /// <param name="isTubeClosed">If the tube is closed set to <c>true</c> .</param>
        /// <param name="isSectionClosed">if set to <c>true</c> [is section closed].</param>
        public void AddTube( IList<Vector3> path, IList<float> angles, IList<float> values, IList<float> diameters, IList<Vector2> section, Vector3 sectionXAxis, bool isTubeClosed, bool isSectionClosed)
        {
            if (values != null && values.Count == 0)
            {
                throw new InvalidOperationException(WrongNumberOfTextureCoordinates);
            }

            if (diameters != null && diameters.Count == 0)
            {
                throw new InvalidOperationException(WrongNumberOfDiameters);
            }

            if (angles != null && angles.Count == 0)
            {
                throw new InvalidOperationException(WrongNumberOfAngles);
            }

            int index0 = Positions.Count;
            int pathLength = path.Count;
            int sectionLength = section.Count;
            if (pathLength < 2 || sectionLength < 2)
            {
                return;
            }

            var forward = path[1] - path[0];
            var right = sectionXAxis;
            var up = Vector3.Cross(forward, right);
            up.Normalize();
            right.Normalize();

            int diametersCount = diameters?.Count ?? 0;
            int valuesCount = values?.Count ?? 0;
            int anglesCount = angles?.Count ?? 0;

            for (int i = 0; i < pathLength; i++)
            {
                float radius = diameters?[i%diametersCount]/2 ?? 1;
                float theta = angles?[i%anglesCount] ?? 0.0f;

                float ct = (float) Math.Cos(theta);
                float st = (float) Math.Sin(theta);

                int i0 = i > 0 ? i - 1 : i;
                int i1 = i + 1 < pathLength ? i + 1 : i;

                forward = path[i1] - path[i0];
                right = Vector3.Cross(up, forward);
                if (right.LengthSquared > 1e-6)
                {
                    up = Vector3.Cross(forward, right);
                }

                up.Normalize();
                right.Normalize();
                for (int j = 0; j < sectionLength; j++)
                {
                    var x = section[j].X*ct - section[j].Y*st;
                    var y = section[j].X*st + section[j].Y*ct;

                    var w = x*right*radius + y*up*radius;
                    var q = path[i] + w;
                    Positions.Add(q);
                    if (Normals != null)
                    {
                        w.Normalize();
                        Normals.Add(w);
                    }

                    TextureCoordinates?.Add(
                        values != null
                            ? new Vector2(values[i%valuesCount], (float) j/(sectionLength - 1))
                            : new Vector2());
                }
            }

            AddRectangularMeshTriangleIndices(index0, pathLength, sectionLength, isSectionClosed, isTubeClosed);
        }
        /// <summary>
        /// Appends the specified mesh.
        /// </summary>
        /// <param name="builder">
        /// The mesh.
        /// </param>
        public void Append(GeometryBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            Append(builder.Positions, builder.TriangleIndices, builder.Normals, builder.TextureCoordinates);
        }
        /// <summary>
        /// Appends the specified points and triangles.
        /// </summary>
        /// <param name="positionsToAppend">
        /// The points to append.
        /// </param>
        /// <param name="triangleIndicesToAppend">
        /// The triangle indices to append.
        /// </param>
        /// <param name="normalsToAppend">
        /// The normal vectors to append.
        /// </param>
        /// <param name="textureCoordinatesToAppend">
        /// The texture coordinates to append.
        /// </param>
        public void Append(IList<Vector3> positionsToAppend, IList<int> triangleIndicesToAppend, IList<Vector3> normalsToAppend = null, IList<Vector2> textureCoordinatesToAppend = null)
        {
            if (positionsToAppend == null)
            {
                throw new ArgumentNullException(nameof(positionsToAppend));
            }

            if (Normals != null && normalsToAppend == null)
            {
                throw new InvalidOperationException(SourceMeshNormalsShouldNotBeNull);
            }

            if (TextureCoordinates != null && textureCoordinatesToAppend == null)
            {
                throw new InvalidOperationException(SourceMeshTextureCoordinatesShouldNotBeNull);
            }

            if (normalsToAppend != null && normalsToAppend.Count != positionsToAppend.Count)
            {
                throw new InvalidOperationException(WrongNumberOfNormals);
            }

            if (textureCoordinatesToAppend != null && textureCoordinatesToAppend.Count != positionsToAppend.Count)
            {
                throw new InvalidOperationException(WrongNumberOfTextureCoordinates);
            }

            int index0 = Positions.Count;
            foreach (var p in positionsToAppend)
            {
                Positions.Add(p);
            }

            if (Normals != null && normalsToAppend != null)
            {
                foreach (var n in normalsToAppend)
                {
                    Normals.Add(n);
                }
            }

            if (TextureCoordinates != null && textureCoordinatesToAppend != null)
            {
                foreach (var t in textureCoordinatesToAppend)
                {
                    TextureCoordinates.Add(t);
                }
            }

            foreach (int i in triangleIndicesToAppend)
            {
                TriangleIndices.Add(index0 + i);
            }
        }
        /// <summary>
        /// Scales the positions (and normal vectors).
        /// </summary>
        /// <param name="scaleX">
        /// The X scale factor.
        /// </param>
        /// <param name="scaleY">
        /// The Y scale factor.
        /// </param>
        /// <param name="scaleZ">
        /// The Z scale factor.
        /// </param>
        public void Scale(float scaleX, float scaleY, float scaleZ)
        {
            for (int i = 0; i < Positions.Count; i++)
            {
                Positions[i] = new Vector3(
                    Positions[i].X*scaleX, Positions[i].Y*scaleY, Positions[i].Z*scaleZ);
            }

            if (Normals != null)
            {
                for (int i = 0; i < Normals.Count; i++)
                {
                    Normals[i] = new Vector3(
                        Normals[i].X*scaleX, Normals[i].Y*scaleY, Normals[i].Z*scaleZ);
                    Normals[i].Normalize();
                }
            }
        }
        /// <summary>
        /// Performs a linear subdivision of the mesh.
        /// </summary>
        /// <param name="barycentric">
        /// Add a vertex in the center if set to <c>true</c> .
        /// </param>
        public void SubdivideLinear(bool barycentric = false)
        {
            if (barycentric)
            {
                SubdivideBarycentric();
            }
            else
            {
                Subdivide4();
            }
        }
        /// <summary>
        /// Converts the geometry to a <see cref="Geometry"/> .
        /// </summary>
        /// <returns>
        /// A mesh geometry.
        /// </returns>
        public Geometry ToGeometry(out BoundingBox box, bool dynamic = false, Context ctx = null)
        {
            ctx = ctx ?? Application.CurrentContext;

            var geom = new Geometry(ctx);

            if (TriangleIndices.Count == 0)
            {
                box = new BoundingBox();
                return geom;
            }

            if (Normals != null && Positions.Count != Normals.Count)
                throw new InvalidOperationException(WrongNumberOfNormals);

            if (TextureCoordinates != null && Positions.Count != TextureCoordinates.Count)
                throw new InvalidOperationException(WrongNumberOfTextureCoordinates);

            var vb = new VertexBuffer(ctx, false);
            var ib = new IndexBuffer(ctx, false);

            var mask = ElementMask.Position;
            if (CreateNormals) mask |= ElementMask.Normal;
            if (CreateTextureCoordinates) mask |= ElementMask.TexCoord1;

            vb.SetSize((uint)Positions.Count, mask, dynamic);
            ib.SetSize((uint)TriangleIndices.Count, false, dynamic);

            var data = Positions.Select(d => new[] { d.X, d.Y, d.Z });

            if (CreateNormals)
                data = data
                    .Zip(Normals, (floats, d) => floats.Concat(new[] { d.X, d.Y, d.Z }))
                    .Select(floats => floats.ToArray());

            if (CreateTextureCoordinates)
                data = data
                    .Zip(TextureCoordinates, (floats, d) => floats.Concat(new[] { d.X, d.Y }))
                    .Select(floats => floats.ToArray());

            vb.SetData(data.SelectMany(floats => floats).ToArray());
            ib.SetData(TriangleIndices.Select(i => (short)i).ToArray());

            geom.SetVertexBuffer(0, vb);
            geom.IndexBuffer = ib;
            geom.SetDrawRange(PrimitiveType.TriangleList, 0, (uint)TriangleIndices.Count, false);

            box = new BoundingBox();
            foreach (var pos in Positions)
                Utils.Merge(ref box, new Vector3(pos.X, pos.Y, pos.Z));

            return geom;
        }
        /// <summary>
        /// Adds normal vectors for a rectangular mesh.
        /// </summary>
        /// <param name="index0">
        /// The index 0.
        /// </param>
        /// <param name="rows">
        /// The number of rows.
        /// </param>
        /// <param name="columns">
        /// The number of columns.
        /// </param>
        private void AddRectangularMeshNormals(int index0, int rows, int columns)
        {
            for (int i = 0; i < rows; i++)
            {
                int i1 = i + 1;
                if (i1 == rows)
                {
                    i1--;
                }

                int i0 = i1 - 1;
                for (int j = 0; j < columns; j++)
                {
                    int j1 = j + 1;
                    if (j1 == columns)
                    {
                        j1--;
                    }

                    int j0 = j1 - 1;
                    var u = Vector3.Subtract(
                        Positions[index0 + i1*columns + j0], Positions[index0 + i0*columns + j0]);
                    var v = Vector3.Subtract(
                        Positions[index0 + i0*columns + j1], Positions[index0 + i0*columns + j0]);
                    var normal = Vector3.Cross(u, v);
                    normal.Normalize();
                    Normals.Add(normal);
                }
            }
        }
        /// <summary>
        /// Adds texture coordinates for a rectangular mesh.
        /// </summary>
        /// <param name="rows">
        /// The number of rows.
        /// </param>
        /// <param name="columns">
        /// The number of columns.
        /// </param>
        private void AddRectangularMeshTextureCoordinates(int rows, int columns)
        {
            for (int i = 0; i < rows; i++)
            {
                float v = (float) i/(rows - 1);
                for (int j = 0; j < columns; j++)
                {
                    float u = (float) j/(columns - 1);
                    TextureCoordinates.Add(new Vector2(u, v));
                }
            }
        }
        /// <summary>
        /// Add triangle indices for a rectangular mesh.
        /// </summary>
        /// <param name="index0">
        /// The index offset.
        /// </param>
        /// <param name="rows">
        /// The number of rows.
        /// </param>
        /// <param name="columns">
        /// The number of columns.
        /// </param>
        /// <param name="isSpherical">
        /// set the flag to true to create a sphere mesh (triangles at top and bottom).
        /// </param>
        private void AddRectangularMeshTriangleIndices(int index0, int rows, int columns, bool isSpherical = false)
        {
            for (int i = 0; i < rows - 1; i++)
            {
                for (int j = 0; j < columns - 1; j++)
                {
                    int ij = i*columns + j;
                    if (!isSpherical || i > 0)
                    {
                        TriangleIndices.Add(index0 + ij);
                        TriangleIndices.Add(index0 + ij + 1 + columns);
                        TriangleIndices.Add(index0 + ij + 1);
                    }

                    if (!isSpherical || i < rows - 2)
                    {
                        TriangleIndices.Add(index0 + ij + 1 + columns);
                        TriangleIndices.Add(index0 + ij);
                        TriangleIndices.Add(index0 + ij + columns);
                    }
                }
            }
        }
        /// <summary>
        /// Adds triangular indices for a rectangular mesh.
        /// </summary>
        /// <param name="index0">
        /// The index 0.
        /// </param>
        /// <param name="rows">
        /// The rows.
        /// </param>
        /// <param name="columns">
        /// The columns.
        /// </param>
        /// <param name="rowsClosed">
        /// True if rows are closed.
        /// </param>
        /// <param name="columnsClosed">
        /// True if columns are closed.
        /// </param>
        private void AddRectangularMeshTriangleIndices(int index0, int rows, int columns, bool rowsClosed, bool columnsClosed)
        {
            int m2 = rows - 1;
            int n2 = columns - 1;
            if (columnsClosed)
            {
                m2++;
            }

            if (rowsClosed)
            {
                n2++;
            }

            for (int i = 0; i < m2; i++)
            {
                for (int j = 0; j < n2; j++)
                {
                    int i00 = index0 + i*columns + j;
                    int i01 = index0 + i*columns + (j + 1)%columns;
                    int i10 = index0 + (i + 1)%rows*columns + j;
                    int i11 = index0 + (i + 1)%rows*columns + (j + 1)%columns;
                    TriangleIndices.Add(i00);
                    TriangleIndices.Add(i11);
                    TriangleIndices.Add(i01);

                    TriangleIndices.Add(i11);
                    TriangleIndices.Add(i00);
                    TriangleIndices.Add(i10);
                }
            }
        }
        /// <summary>
        /// Subdivides each triangle into four sub-triangles.
        /// </summary>
        private void Subdivide4()
        {
            // Each triangle is divided into four subtriangles, adding new vertices in the middle of each edge.
            int ip = Positions.Count;
            int ntri = TriangleIndices.Count;
            for (int i = 0; i < ntri; i += 3)
            {
                int i0 = TriangleIndices[i];
                int i1 = TriangleIndices[i + 1];
                int i2 = TriangleIndices[i + 2];
                var p0 = Positions[i0];
                var p1 = Positions[i1];
                var p2 = Positions[i2];
                var v01 = p1 - p0;
                var v12 = p2 - p1;
                var v20 = p0 - p2;
                var p01 = p0 + v01*0.5f;
                var p12 = p1 + v12*0.5f;
                var p20 = p2 + v20*0.5f;

                int i01 = ip++;
                int i12 = ip++;
                int i20 = ip++;

                Positions.Add(p01);
                Positions.Add(p12);
                Positions.Add(p20);

                if (Normals != null)
                {
                    var n = Normals[i0];
                    Normals.Add(n);
                    Normals.Add(n);
                    Normals.Add(n);
                }

                if (TextureCoordinates != null)
                {
                    var uv0 = TextureCoordinates[i0];
                    var uv1 = TextureCoordinates[i0 + 1];
                    var uv2 = TextureCoordinates[i0 + 2];
                    var t01 = uv1 - uv0;
                    var t12 = uv2 - uv1;
                    var t20 = uv0 - uv2;
                    var u01 = uv0 + t01*0.5f;
                    var u12 = uv1 + t12*0.5f;
                    var u20 = uv2 + t20*0.5f;
                    TextureCoordinates.Add(u01);
                    TextureCoordinates.Add(u12);
                    TextureCoordinates.Add(u20);
                }

                // TriangleIndices[i ] = i0;
                TriangleIndices[i + 1] = i01;
                TriangleIndices[i + 2] = i20;

                TriangleIndices.Add(i01);
                TriangleIndices.Add(i1);
                TriangleIndices.Add(i12);

                TriangleIndices.Add(i12);
                TriangleIndices.Add(i2);
                TriangleIndices.Add(i20);

                TriangleIndices.Add(i01);
                TriangleIndices.Add(i12);
                TriangleIndices.Add(i20);
            }
        }
        /// <summary>
        /// Subdivides each triangle into six triangles. Adds a vertex at the midpoint of each triangle.
        /// </summary>
        /// <remarks>
        /// See <a href="http://en.wikipedia.org/wiki/Barycentric_subdivision">wikipedia</a>.
        /// </remarks>
        private void SubdivideBarycentric()
        {
            // The BCS of a triangle S divides it into six triangles; each part has one vertex v2 at the
            // barycenter of S, another one v1 at the midpoint of some side, and the last one v0 at one
            // of the original vertices.
            int im = Positions.Count;
            int ntri = TriangleIndices.Count;
            for (int i = 0; i < ntri; i += 3)
            {
                int i0 = TriangleIndices[i];
                int i1 = TriangleIndices[i + 1];
                int i2 = TriangleIndices[i + 2];
                var p0 = Positions[i0];
                var p1 = Positions[i1];
                var p2 = Positions[i2];
                var v01 = p1 - p0;
                var v12 = p2 - p1;
                var v20 = p0 - p2;
                var p01 = p0 + v01*0.5f;
                var p12 = p1 + v12*0.5f;
                var p20 = p2 + v20*0.5f;
                var m = new Vector3((p0.X + p1.X + p2.X)/3, (p0.Y + p1.Y + p2.Y)/3, (p0.Z + p1.Z + p2.Z)/3);

                int i01 = im + 1;
                int i12 = im + 2;
                int i20 = im + 3;

                Positions.Add(m);
                Positions.Add(p01);
                Positions.Add(p12);
                Positions.Add(p20);

                if (Normals != null)
                {
                    var n = Normals[i0];
                    Normals.Add(n);
                    Normals.Add(n);
                    Normals.Add(n);
                    Normals.Add(n);
                }

                if (TextureCoordinates != null)
                {
                    var uv0 = TextureCoordinates[i0];
                    var uv1 = TextureCoordinates[i0 + 1];
                    var uv2 = TextureCoordinates[i0 + 2];
                    var t01 = uv1 - uv0;
                    var t12 = uv2 - uv1;
                    var t20 = uv0 - uv2;
                    var u01 = uv0 + t01*0.5f;
                    var u12 = uv1 + t12*0.5f;
                    var u20 = uv2 + t20*0.5f;
                    var uvm = new Vector2((uv0.X + uv1.X)*0.5f, (uv0.Y + uv1.Y)*0.5f);
                    TextureCoordinates.Add(uvm);
                    TextureCoordinates.Add(u01);
                    TextureCoordinates.Add(u12);
                    TextureCoordinates.Add(u20);
                }

                // TriangleIndices[i ] = i0;
                TriangleIndices[i + 1] = i01;
                TriangleIndices[i + 2] = im;

                TriangleIndices.Add(i01);
                TriangleIndices.Add(i1);
                TriangleIndices.Add(im);

                TriangleIndices.Add(i1);
                TriangleIndices.Add(i12);
                TriangleIndices.Add(im);

                TriangleIndices.Add(i12);
                TriangleIndices.Add(i2);
                TriangleIndices.Add(im);

                TriangleIndices.Add(i2);
                TriangleIndices.Add(i20);
                TriangleIndices.Add(im);

                TriangleIndices.Add(i20);
                TriangleIndices.Add(i0);
                TriangleIndices.Add(im);

                im += 4;
            }
        }
        private static IList<Vector2> GetCircle(int thetaDiv)
        {
            IList<Vector2> circle;
            if (!CircleCache.Value.TryGetValue(thetaDiv, out circle))
            {
                circle = new List<Vector2>();
                CircleCache.Value.Add(thetaDiv, circle);
                for (int i = 0; i < thetaDiv; i++)
                {
                    float theta = (float) (Math.PI * 2 * ((float)i / (thetaDiv - 1)));
                    circle.Add(new Vector2((float) Math.Cos(theta), (float) -Math.Sin(theta)));
                }
            }
            return circle;
        }
        /// <summary>
        /// Box face enumeration.
        /// </summary>
        [Flags]
        public enum BoxFaces
        {
            /// <summary>
            /// The top.
            /// </summary>
            Top = 0x1,

            /// <summary>
            /// The bottom.
            /// </summary>
            Bottom = 0x2,

            /// <summary>
            /// The left side.
            /// </summary>
            Left = 0x4,

            /// <summary>
            /// The right side.
            /// </summary>
            Right = 0x8,

            /// <summary>
            /// The front side.
            /// </summary>
            Front = 0x10,

            /// <summary>
            /// The back side.
            /// </summary>
            Back = 0x20,

            /// <summary>
            /// All sides.
            /// </summary>
            All = Top | Bottom | Left | Right | Front | Back
        }
    }
}