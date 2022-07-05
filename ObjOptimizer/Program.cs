// KSoft OptiOBJ 1.0 (c) 2022 Kleadron, All Rights Reserved
// SOURCE CODE IS LICENSED UNDER THE MIT LICENSE, CHECK THE LICENSE FILE FOR MORE INFO!

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ObjOptimizer
{
    #region FNA code import
    // The following code is shamelessly "borrowed" from the FNA project.
    // I used the Microsoft XNA Framework originally, but requiring people to install that to use this software would be dumb.
    // I could just not credit it, but that's a bit dicky!
    // https://github.com/FNA-XNA/FNA
    public struct Vector3 : IEquatable<Vector3>
    {
        public float X;
        public float Y;
        public float Z;

        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is Vector3) && Equals((Vector3)obj);
        }

        public bool Equals(Vector3 other)
        {
            return (X == other.X &&
                    Y == other.Y &&
                    Z == other.Z);
        }

        public static bool operator ==(Vector3 value1, Vector3 value2)
        {
            return (value1.X == value2.X &&
                    value1.Y == value2.Y &&
                    value1.Z == value2.Z);
        }

        public static bool operator !=(Vector3 value1, Vector3 value2)
        {
            return !(value1 == value2);
        }
    }

    public struct Vector2 : IEquatable<Vector2>
    {
        public float X;
        public float Y;

        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is Vector2) && Equals((Vector2)obj);
        }

        public bool Equals(Vector2 other)
        {
            return (X == other.X &&
                    Y == other.Y);
        }

        public static bool operator ==(Vector2 value1, Vector2 value2)
        {
            return (value1.X == value2.X &&
                    value1.Y == value2.Y);
        }

        public static bool operator !=(Vector2 value1, Vector2 value2)
        {
            return !(value1 == value2);
        }
    }
    #endregion

    // OBJ Vertices are indices into the position/uv/normal arrays.
    public struct Vertex
    {
        public int posIndex;
        public int uvIndex;
        public int normIndex;
    }

    // OBJ Faces contain a list of vertices. A list is used for simplicity.
    public class Face
    {
        public List<Vertex> vertices = new List<Vertex>();
    }

    class Program
    {
        // Yes, all the code is in void Main. Cry about it =]
        static int Main(string[] args)
        {
            Console.WriteLine("KSoft OptiOBJ 1.0 (c) 2022 Kleadron, All Rights Reserved");

            if (args.Length == 0 || args.Contains("-?"))
            {
                Console.WriteLine("Usage Information:");
                Console.WriteLine("Specify or drag-and-drop an OBJ file onto this exe to optimize it.");
                Console.WriteLine("-? or no arguments shows this screen.");
                Console.WriteLine("-auto does not wait for any key presses after optimization.");
                //Console.WriteLine("-tri converts all faces to triangles, if they aren't already."); // NOT IMPLEMENTED
                Console.WriteLine("Options must go AFTER the OBJ file path!");
                Console.WriteLine("Press any key to close...");
                Console.ReadKey();
                return 0;
            }

            // the 
            bool automatic = args.Contains("-auto");

            string objPath = args[0];

            if (!File.Exists(objPath))
            {
                Console.WriteLine("OBJ file does not exist!");
                if (!automatic)
                {
                    Console.WriteLine("Press any key to close...");
                    Console.ReadKey();
                }
                return 1;
            }

            //bool noDedupePositions

            string materialLib = null;
            string currentMat = "";

            // I used hashsets and dictionaries to speed up de-duplication, I might mess with it later but it works fine for now.
            List<Vector3> positions = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            HashSet<Vector3> posHash = new HashSet<Vector3>();
            HashSet<Vector2> uvHash = new HashSet<Vector2>();
            HashSet<Vector3> normHash = new HashSet<Vector3>();

            Dictionary<Vector3, int> posToIndex = new Dictionary<Vector3, int>();
            Dictionary<Vector2, int> uvToIndex = new Dictionary<Vector2, int>();
            Dictionary<Vector3, int> normToIndex = new Dictionary<Vector3, int>();

            // These lists store the indices from a referenced face vertex to the correct de-duplicated 
            List<int> posIndices = new List<int>();
            List<int> uvIndices = new List<int>();
            List<int> normIndices = new List<int>();

            // Stores material names to groups of faces that go with the material
            Dictionary<string, List<Face>> materialFaceGroups = new Dictionary<string, List<Face>>();

            char[] splitChars = new char[] { ' ', '\t' };

            Console.WriteLine("Reading " + objPath);
            string[] lines = File.ReadAllLines(objPath);

            Console.WriteLine("Processing OBJ");

            // timer
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // go through every line of the OBJ file
            for (int i = 0; i < lines.Length; i++)
            {
                // trim to make sure there's no garbage
                string line = lines[i].Trim();

                // make sure the line is valid and not a comment
                if (line.Length == 0 || line[0] == '#')
                    continue;

                // these are checking the prefix of the line with a space at the end to make sure it's the entire prefixed token

                if (line.StartsWith("mtllib "))
                {
                    materialLib = line.Substring("mtllib ".Length);
                }

                if (line.StartsWith("v "))
                {
                    // split the line without the token
                    string[] split = line.Substring("v ".Length).Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                    Vector3 v = new Vector3();
                    v.X = float.Parse(split[0]);
                    v.Y = float.Parse(split[1]);
                    v.Z = float.Parse(split[2]);

                    // old slow ass code
                    //int index = positions.IndexOf(v);
                    //if (index == -1)
                    //{
                    //    posIndices.Add(positions.Count);
                    //    positions.Add(v);
                    //}
                    //else
                    //{
                    //    posIndices.Add(index);
                    //}

                    // check if the hashset contains the position already
                    if (posHash.Add(v))
                    {
                        // get the index for the new position, update posToIndex, add it to the indices list, add it to the positions array
                        int index = positions.Count;
                        posToIndex[v] = index;
                        posIndices.Add(index);
                        positions.Add(v);
                    }
                    else
                    {
                        // it already exists, so just add the index of it to the index redirection list
                        int index = posToIndex[v];
                        posIndices.Add(index);
                    }
                }

                // these are basically the same as the above so I'm not commenting them
                if (line.StartsWith("vt "))
                {
                    string[] split = line.Substring("vt ".Length).Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                    Vector2 v = new Vector2();
                    v.X = float.Parse(split[0]);
                    v.Y = float.Parse(split[1]);

                    if (uvHash.Add(v))
                    {
                        int index = uvs.Count;
                        uvToIndex[v] = index;
                        uvIndices.Add(index);
                        uvs.Add(v);
                    }
                    else
                    {
                        int index = uvs.IndexOf(v);
                        uvIndices.Add(index);
                    }
                }

                if (line.StartsWith("vn "))
                {
                    string[] split = line.Substring("vn ".Length).Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                    Vector3 v = new Vector3();
                    v.X = float.Parse(split[0]);
                    v.Y = float.Parse(split[1]);
                    v.Z = float.Parse(split[2]);

                    if (normHash.Add(v))
                    {
                        int index = normals.Count;
                        normToIndex[v] = index;
                        normIndices.Add(normals.Count);
                        normals.Add(v);
                    }
                    else
                    {
                        int index = normToIndex[v];
                        normIndices.Add(index);
                    }
                }

                // set the current material-face group
                if (line.StartsWith("usemtl "))
                {
                    currentMat = line.Substring("usemtl ".Length);
                }

                // process the 3 or more cornered face
                if (line.StartsWith("f "))
                {
                    string[] split = line.Substring("f ".Length).Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

                    Face f = new Face();
                    for (int j = 0; j < split.Length; j++)
                    {
                        // Split the vertex definition by / to get position/uv/normal indices
                        string[] vSplit = split[j].Split('/');

                        Vertex v = new Vertex();

                        // reading vertex indices from the file requires the index to be negated by 1 to get a zero-based index.

                        v.posIndex = int.Parse(vSplit[0])-1;

                        // indices that are not specified use a -1 internally.
                        if (vSplit.Length > 1 && vSplit[1].Length > 0)
                            v.uvIndex = int.Parse(vSplit[1])-1;
                        else
                            v.uvIndex = -1;

                        if (vSplit.Length > 2 && vSplit[2].Length > 0)
                            v.normIndex = int.Parse(vSplit[2])-1;
                        else
                            v.normIndex = -1;

                        f.vertices.Add(v);
                    }

                    // create a group for this material if it doesn't exist
                    if (!materialFaceGroups.ContainsKey(currentMat))
                    {
                        materialFaceGroups[currentMat] = new List<Face>();
                    }
                    // add the face to the current group
                    materialFaceGroups[currentMat].Add(f);
                }

            }

            stopwatch.Stop();

            // deduplication/optimization report
            Console.WriteLine("Done processing, took " + stopwatch.ElapsedMilliseconds + "ms");
            Console.WriteLine("== Optimization Report ==");
            Console.WriteLine("  Positions: " + posIndices.Count + " -> " + positions.Count + " (-" + (posIndices.Count - positions.Count) + ")");
            Console.WriteLine("  UVs      : " + uvIndices.Count + " -> " + uvs.Count + " (-" + (uvIndices.Count - uvs.Count) + ")");
            Console.WriteLine("  Normals  : " + normIndices.Count + " -> " + normals.Count + " (-" + (normIndices.Count - normals.Count) + ")");
            // after processsing

            string newFilePath = Path.GetFileNameWithoutExtension(objPath) + "_opti.obj";

            Console.WriteLine("Building optimized OBJ");

            // the list to store the lines of the new file in
            List<string> optiLines = new List<string>();

            // optimizer watermark, this is the only comment
            optiLines.Add("# Optimized by KSoft OptiOBJ 1.0");

            // if there was never a material library then it won't be added here
            if (materialLib != null)
            {
                optiLines.Add("mtllib " + materialLib);
            }

            // I was messing with format specifiers to try and compress OBJ files further, but I am keeping it default for now
            string floatFormat = "G";

            // add the following vertex data lists to the file
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 v = positions[i];
                optiLines.Add("v " + v.X.ToString(floatFormat) + " " + v.Y.ToString(floatFormat) + " " + v.Z.ToString(floatFormat));
            }

            for (int i = 0; i < uvs.Count; i++)
            {
                Vector2 v = uvs[i];
                optiLines.Add("vt " + v.X.ToString(floatFormat) + " " + v.Y.ToString(floatFormat));
            }

            for (int i = 0; i < normals.Count; i++)
            {
                Vector3 v = normals[i];
                optiLines.Add("vn " + v.X.ToString(floatFormat) + " " + v.Y.ToString(floatFormat) + " " + v.Z.ToString(floatFormat));
            }

            // add all of the material-face groups to the file
            foreach (KeyValuePair<string, List<Face>> pair in materialFaceGroups)
            {
                // if this is not the default material then the material name gets marked
                if (pair.Key.Length > 0)
                    optiLines.Add("usemtl " + pair.Key);

                List<Face> faces = pair.Value;
                for (int i = 0; i < faces.Count; i++)
                {
                    Face f = faces[i];
                    // start a face line with "f"
                    string faceLine = "f";

                    for (int j = 0; j < f.vertices.Count; j++)
                    {
                        // space out the vertex from the "f" token or previous vertex
                        faceLine += " ";

                        Vertex v = f.vertices[j];

                        // vertex indices have a 1 added to the value because of the OBJ format.

                        // case: any, vertices always have position
                        int posIndex = posIndices[v.posIndex]+1;
                        faceLine += posIndex.ToString();

                        // case: has normal but no UV
                        if (v.uvIndex == -1 && v.normIndex != -1)
                        {
                            int normIndex = normIndices[v.normIndex]+1;
                            faceLine += "//" + normIndex;
                        }
                        // case: has UV but unsure of normal
                        else if (v.uvIndex != -1)
                        {
                            int uvIndex = uvIndices[v.uvIndex]+1;
                            faceLine += "/" + uvIndex;

                            // case: has normal
                            if (v.normIndex != -1)
                            {
                                int normIndex = normIndices[v.normIndex]+1;
                                faceLine += "/" + normIndex;
                            }
                        }
                    }

                    optiLines.Add(faceLine);
                }
            }

            // save it
            Console.WriteLine("Writing " + newFilePath);
            File.WriteAllLines(newFilePath, optiLines);

            // :D
            Console.WriteLine("Done!");
            if (!automatic)
            {
                Console.WriteLine("Press any key to close...");
                Console.ReadKey();
            }

            return 0;
        }
    }
}
