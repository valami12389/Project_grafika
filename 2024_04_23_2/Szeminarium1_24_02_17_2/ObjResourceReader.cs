using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;
using System.Reflection;

namespace Szeminarium1_24_02_17_2
{
    internal class ObjResourceReader
    {

        public static unsafe GlObject CreateAsteroidWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<float[]> objNormals;
            List<int[][]> objFaces;
            ReadObjDataForAsteroid(out objVertices, out objNormals, out objFaces);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysForAsteroidObj(faceColor, objVertices, objNormals, objFaces, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe void ReadObjDataForAsteroid(
            out List<float[]> objVertices,
            out List<float[]> objNormals,
            out List<int[][]> objFaces)
        {
            objVertices = new List<float[]>();
            objNormals = new List<float[]>();
            objFaces = new List<int[][]>();

            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Szeminarium1_24_02_17_2.Resources.asteroid.obj";

            using (Stream objStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (objStream == null)
                    throw new FileNotFoundException($"Resource '{resourceName}' not found.");

                using (StreamReader objReader = new StreamReader(objStream))
                {
                    while (!objReader.EndOfStream)
                    {
                        var line = objReader.ReadLine();

                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        switch (parts[0])
                        {
                            case "v":
                                objVertices.Add(parts.Skip(1).Select(p => float.Parse(p, CultureInfo.InvariantCulture)).ToArray());
                                break;

                            case "vn":
                                objNormals.Add(parts.Skip(1).Select(p => float.Parse(p, CultureInfo.InvariantCulture)).ToArray());
                                break;

                            case "f":
                                int[][] face = parts.Skip(1).Select(part =>
                                {
                                    var indices = part.Split("//");
                                    return new int[]
                                    {
                                int.Parse(indices[0]), // vertex index
                                -1,                    // texture index (missing)
                                int.Parse(indices[1])  // normal index
                                    };
                                }).ToArray();
                                objFaces.Add(face);
                                break;
                        }
                    }
                }
            }
        }

        private static void CreateGlArraysForAsteroidObj(float[] faceColor,
            List<float[]> objVertices,
            List<float[]> objNormals,
            List<int[][]> objFaces,
            List<float> glVertices,
            List<float> glColors,
            List<uint> glIndices)
        {
            Dictionary<string, uint> vertexCache = new Dictionary<string, uint>();

            foreach (var face in objFaces)
            {
                foreach (var v in face)
                {
                    int vIdx = v[0] - 1;
                    int vnIdx = v[2] - 1;

                    string key = $"{vIdx}//{vnIdx}";

                    if (!vertexCache.ContainsKey(key))
                    {
                        // pozíció
                        glVertices.AddRange(objVertices[vIdx]);

                        // normál
                        glVertices.AddRange(objNormals[vnIdx]);

                        // dummy UV
                        glVertices.AddRange(new float[] { 0f, 0f });

                        // szín
                        glColors.AddRange(faceColor);

                        vertexCache[key] = (uint)vertexCache.Count;
                    }

                    glIndices.Add(vertexCache[key]);
                }
            }
        }

        public static unsafe GlObject CreateSpaceshipWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<float[]> objNormals;
            List<float[]> objTextures;
            List<int[][]> objFaces;

            ReadObjDataForSpaceship(out objVertices, out objNormals, out objTextures, out objFaces);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objNormals, objTextures, objFaces, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint offsetTexCoord = offsetNormal + (3 * sizeof(float));
            uint vertexSize = offsetTexCoord + (2 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);

            // Position attribute
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            // Normal attribute
            Gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);
            Gl.EnableVertexAttribArray(1);

            // Texture coordinate attribute
            Gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetTexCoord);
            Gl.EnableVertexAttribArray(2);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(3);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);

            // release array buffer
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)glIndices.Count;

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
        }

        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor,
            List<float[]> objVertices, List<float[]> objNormals, List<float[]> objTextures,
            List<int[][]> objFaces, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, uint> vertexCache = new Dictionary<string, uint>();

            foreach (var face in objFaces)
            {
                for (int i = 0; i < face.Length; i++)
                {
                    var vertexIndices = face[i];
                    int vIdx = vertexIndices[0] - 1;
                    int vtIdx = vertexIndices[1] - 1;
                    int vnIdx = vertexIndices[2] - 1;

                    var vertexKey = $"{vIdx}/{vtIdx}/{vnIdx}";

                    if (!vertexCache.ContainsKey(vertexKey))
                    {
                        // Add vertex position
                        glVertices.AddRange(objVertices[vIdx]);

                        // Add normal
                        if (vnIdx >= 0 && vnIdx < objNormals.Count)
                            glVertices.AddRange(objNormals[vnIdx]);
                        else
                            glVertices.AddRange(new float[] { 0f, 0f, 0f });

                        // Add texture coordinate
                        if (vtIdx >= 0 && vtIdx < objTextures.Count)
                            glVertices.AddRange(objTextures[vtIdx]);
                        else
                            glVertices.AddRange(new float[] { 0f, 0f });

                        // Add color
                        glColors.AddRange(faceColor);

                        vertexCache.Add(vertexKey, (uint)vertexCache.Count);
                    }

                    glIndices.Add(vertexCache[vertexKey]);
                }
            }
        }

        private static unsafe void ReadObjDataForSpaceship(
            out List<float[]> objVertices,
            out List<float[]> objNormals,
            out List<float[]> objTextures,
            out List<int[][]> objFaces)
        {
            objVertices = new List<float[]>();
            objNormals = new List<float[]>();
            objTextures = new List<float[]>();
            objFaces = new List<int[][]>();

            // Get the assembly where the resources are embedded
            var assembly = Assembly.GetExecutingAssembly();
            // Get the resource name (case sensitive!)
            string resourceName = "Szeminarium1_24_02_17_2.Resources.spaceship.obj";

            using (Stream objStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (objStream == null)
                {
                    // List all available resources to help debug
                    string[] resources = assembly.GetManifestResourceNames();
                    throw new FileNotFoundException($"Resource '{resourceName}' not found. Available resources: {string.Join(", ", resources)}");
                }

                using (StreamReader objReader = new StreamReader(objStream))
                {
                    while (!objReader.EndOfStream)
                    {
                        var line = objReader.ReadLine();

                        if (String.IsNullOrEmpty(line) || line.Trim().StartsWith("#"))
                            continue;

                        var lineParts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lineParts.Length == 0)
                            continue;

                        var lineClassifier = lineParts[0];
                        var lineData = lineParts.Skip(1).ToArray();

                        switch (lineClassifier)
                        {
                            case "v":
                                float[] vertex = new float[3];
                                for (int i = 0; i < 3 && i < lineData.Length; i++)
                                    vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                                objVertices.Add(vertex);
                                break;
                            case "vn":
                                float[] normal = new float[3];
                                for (int i = 0; i < 3 && i < lineData.Length; i++)
                                    normal[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                                objNormals.Add(normal);
                                break;
                            case "vt":
                                float[] texture = new float[2];
                                for (int i = 0; i < 2 && i < lineData.Length; i++)
                                    texture[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                                objTextures.Add(texture);
                                break;
                            case "f":
                                int[][] face = new int[lineData.Length][];
                                for (int i = 0; i < lineData.Length; i++)
                                {
                                    var indices = lineData[i].Split('/');
                                    face[i] = new int[3];
                                    face[i][0] = indices.Length > 0 && !string.IsNullOrEmpty(indices[0]) ? int.Parse(indices[0]) : 0;
                                    face[i][1] = indices.Length > 1 && !string.IsNullOrEmpty(indices[1]) ? int.Parse(indices[1]) : 0;
                                    face[i][2] = indices.Length > 2 && !string.IsNullOrEmpty(indices[2]) ? int.Parse(indices[2]) : 0;
                                }
                                objFaces.Add(face);
                                break;
                        }
                    }
                }
            }
        }
    }
}