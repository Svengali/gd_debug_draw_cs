// FILE: DebugDrawInternal/Geometry.cs
#if DEBUG
using Godot;
using System;
using System.Collections.Generic;

namespace DebugDrawInternalFunctionality
{
    /// <summary>
    /// Contains static helper methods for generating geometry and performing bounds checks.
    /// </summary>
    internal static class Geometry
    {
        public static float CubeDiagonalLengthForSphere = (Vector3.One * 0.5f).Length();

        public static Vector3[] CenteredCubeVertices = new Vector3[]{
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f)
        };
        public static Vector3[] CubeVertices = new Vector3[]{
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 1)
        };
        public static int[] CubeIndices = new int[] {
            0, 1,
            1, 2,
            2, 3,
            3, 0,

            4, 5,
            5, 6,
            6, 7,
            7, 4,

            0, 4,
            1, 5,
            2, 6,
            3, 7,
        };
        public static int[] CubeWithDiagonalsIndices = new int[] {
            0, 1,
            1, 2,
            2, 3,
            3, 0,

            4, 5,
            5, 6,
            6, 7,
            7, 4,

            0, 4,
            1, 5,
            2, 6,
            3, 7,

            // Diagonals
            1, 3,
            4, 6,
            1, 4,
            3, 6,
            3, 4,
            1, 6,
        };
        public static Vector3[] ArrowheadVertices = new Vector3[]
        {
            new Vector3(0, 0, -1),
            new Vector3(0, 0.25f, 0),
            new Vector3(0, -0.25f, 0),
            new Vector3(0.25f, 0, 0),
            new Vector3(-0.25f, 0, 0),
            // Cross to center
            new Vector3(0, 0, -0.2f),
        };
        public static int[] ArrowheadIndices = new int[]
        {
            0, 1,
            0, 2,
            0, 3,
            0, 4,
            // Cross
            //1, 2,
            //3, 4,
            // Or Cross to center
            5, 1,
            5, 2,
            5, 3,
            5, 4,
        };
        public static Vector3[] CenteredSquareVertices = new Vector3[]
        {
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
        };
        public static int[] SquareIndices = new int[]
        {
            0, 1, 2,
            2, 3, 0,
        };
        public static Vector3[] PositionVertices = new Vector3[]
        {
            new Vector3(0.5f, 0, 0),
            new Vector3(-0.5f, 0, 0),
            new Vector3(0, 0.5f, 0),
            new Vector3(0, -0.5f, 0),
            new Vector3(0, 0, 0.5f),
            new Vector3(0, 0, -0.5f),
        };
        public static int[] PositionIndices = new int[]
        {
            0, 1,
            2, 3,
            4, 5,
        };

        #region Geometry Generation

        public static Vector3[] CreateCameraFrustumLines(Plane[] frustum)
        {
            if (frustum.Length != 6)
                return Array.Empty<Vector3>();

            Vector3[] res = new Vector3[CubeIndices.Length];

            //  near, far, left, top, right, bottom
            //  0,    1,   2,    3,   4,     5
            var cube = new Vector3[]{
								frustum[0].Intersect3(frustum[3], frustum[2]).Value,
                frustum[0].Intersect3(frustum[3], frustum[4]).Value,
                frustum[0].Intersect3(frustum[5], frustum[4]).Value,
                frustum[0].Intersect3(frustum[5], frustum[2]).Value,

                frustum[1].Intersect3(frustum[3], frustum[2]).Value,
                frustum[1].Intersect3(frustum[3], frustum[4]).Value,
                frustum[1].Intersect3(frustum[5], frustum[4]).Value,
                frustum[1].Intersect3(frustum[5], frustum[2]).Value,
            };

            for (int i = 0; i < res.Length; i++) res[i] = cube[CubeIndices[i]];

            return res;
        }

        public static Vector3[] CreateCubeLines(Vector3 position, Quaternion rotation, Vector3 size, bool centeredBox = true, bool withDiagonals = false)
        {
            Vector3[] scaled = new Vector3[8];
            Vector3[] res = new Vector3[withDiagonals ? CubeWithDiagonalsIndices.Length : CubeIndices.Length];

            bool dont_rot = rotation == Quaternion.Identity;

            Func<int, Vector3> get;
            if (centeredBox)
            {
                if (dont_rot)
                    get = (idx) => CenteredCubeVertices[idx] * size + position;
                else
                    get = (idx) => rotation * (CenteredCubeVertices[idx] * size) + position;
            }
            else
            {
                if (dont_rot)
                    get = (idx) => CubeVertices[idx] * size + position;
                else
                    get = (idx) => rotation * (CubeVertices[idx] * size) + position;
            }

            for (int i = 0; i < 8; i++)
                scaled[i] = get(i);

            if (withDiagonals)
                for (int i = 0; i < res.Length; i++) res[i] = scaled[CubeWithDiagonalsIndices[i]];
            else
                for (int i = 0; i < res.Length; i++) res[i] = scaled[CubeIndices[i]];

            return res;
        }

        public static Vector3[] CreateSphereLines(int lats, int lons, float radius, Vector3 position)
        {
            if (lats < 2)
                lats = 2;
            if (lons < 4)
                lons = 4;

            Vector3[] res = new Vector3[lats * lons * 6];
            int total = 0;
            for (int i = 1; i <= lats; i++)
            {
                float lat0 = Mathf.Pi * (-0.5f + (float)(i - 1) / lats);
                float z0 = Mathf.Sin(lat0);
                float zr0 = Mathf.Cos(lat0);

                float lat1 = Mathf.Pi * (-0.5f + (float)i / lats);
                float z1 = Mathf.Sin(lat1);
                float zr1 = Mathf.Cos(lat1);

                for (int j = lons; j >= 1; j--)
                {
                    float lng0 = 2 * Mathf.Pi * (j - 1) / lons;
                    float x0 = Mathf.Cos(lng0);
                    float y0 = Mathf.Sin(lng0);

                    float lng1 = 2 * Mathf.Pi * j / lons;
                    float x1 = Mathf.Cos(lng1);
                    float y1 = Mathf.Sin(lng1);

                    Vector3[] v = new Vector3[]{
                        new Vector3(x1 * zr0, z0, y1 * zr0) * radius + position,
                        new Vector3(x1 * zr1, z1, y1 * zr1) * radius + position,
                        new Vector3(x0 * zr1, z1, y0 * zr1) * radius + position,
                        new Vector3(x0 * zr0, z0, y0 * zr0) * radius + position
                    };

                    res[total++] = v[0];
                    res[total++] = v[1];
                    res[total++] = v[2];

                    res[total++] = v[2];
                    res[total++] = v[3];
                    res[total++] = v[0];
                }
            }
            return res;
        }

        public static Vector3[] CreateCylinderLines(int edges, float radius, float height, Vector3 position, int drawEdgeEachNStep = 1)
        {
            var angle = 360f / edges;

            List<Vector3> points = new List<Vector3>();

            Vector3 d = new Vector3(0, height * 0.5f, 0);
            for (int i = 0; i < edges; i++)
            {
                float ra = Mathf.DegToRad(i * angle);
                float rb = Mathf.DegToRad((i + 1) * angle);
                Vector3 a = new Vector3(Mathf.Sin(ra), 0, Mathf.Cos(ra)) * radius + position;
                Vector3 b = new Vector3(Mathf.Sin(rb), 0, Mathf.Cos(rb)) * radius + position;

                // Top
                points.Add(a + d);
                points.Add(b + d);

                // Bottom
                points.Add(a - d);
                points.Add(b - d);

                // Edge
                if (i % drawEdgeEachNStep == 0)
                {
                    points.Add(a + d);
                    points.Add(a - d);
                }
            }

            return points.ToArray();
        }

        public static Vector3[] CreateLinesFromPath(IList<Vector3> path)
        {
            var res = new Vector3[(path.Count - 1) * 2];

            for (int i = 0; i < path.Count - 1; i++) // Corrected loop bound
            {
                res[i * 2] = path[i];
                res[i * 2 + 1] = path[i + 1];
            }
            return res;
        }

        #endregion // Geometry Generation

        public static void GetDiagonalVectors(Vector3 a, Vector3 b, out Vector3 bottom, out Vector3 top, out Vector3 diag)
        {
            bottom = Vector3.Zero;
            top = Vector3.Zero;

            if (a.X > b.X) { top.X = a.X; bottom.X = b.X; } else { top.X = b.X; bottom.X = a.X; }
            if (a.Y > b.Y) { top.Y = a.Y; bottom.Y = b.Y; } else { top.Y = b.Y; bottom.Y = a.Y; }
            if (a.Z > b.Z) { top.Z = a.Z; bottom.Z = b.Z; } else { top.Z = b.Z; bottom.Z = a.Z; }

            diag = top - bottom;
        }

        public static bool BoundsPartiallyInsideConvexShape(Aabb bounds, IList<Plane> planes)
        {
            var extent = bounds.Size * 0.5f;
            var center = bounds.Position + extent;
            foreach (var p in planes)
                if (new Vector3(
                        center.X - extent.X * Mathf.Sign(p.Normal.X),
                        center.Y - extent.Y * Mathf.Sign(p.Normal.Y),
                        center.Z - extent.Z * Mathf.Sign(p.Normal.Z)
                        ).Dot(p.Normal) > p.D)
                    return false;

            return true;
        }

        public static bool BoundsPartiallyInsideConvexShape(SphereBounds sphere, IList<Plane> planes)
        {
            foreach (var p in planes)
                if (p.DistanceTo(sphere.Position) >= sphere.Radius)
                    return false;

            return true;
        }

        public static float GetMaxValue(ref Vector3 value)
        {
            return Math.Max(Math.Abs(value.X), Math.Max(Math.Abs(value.Y), Math.Abs(value.Z)));
        }
    }
}
#endif
