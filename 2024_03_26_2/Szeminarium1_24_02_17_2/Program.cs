using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Numerics;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;
        private static IInputContext inputContext;
        private static GL Gl;
        private static ImGuiController controller;
        private static uint program;

        private static GlCube[,,] smallCubes = new GlCube[3, 3, 3];

        private static GlCube glCubeRotating;

        private static float targetRotation = 0f;

        private static bool isRotating = false;
        private static float currentRotationAngle = 0;
        private static float rotationSpeed = (float)(Math.PI / 100f);
        private static bool rotationDirection;

        private static float Shininess = 50;
        private static Vector3 ambientStrength = new Vector3(0.2f, 0.2f, 0.2f);
        private static Vector3 diffuseStrength = new Vector3(0.5f, 0.5f, 0.5f);
        private static Vector3 specularStrength = new Vector3(1.0f, 1.0f, 1.0f);
        private static Vector3 backgroundColor = new Vector3(1f, 1f, 1f);

        private static Vector3 lightColor = new Vector3(1f, 1f, 1f);
        private static Vector3 lightDirection = new Vector3(-1f, -1f, -1f);
        private static float lightIntensity = 1.0f;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNorm;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
            outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
            outNormal = normalize(uNormal*vNorm);
            outWorldPosition = vec3(uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0));
        }
        ";

        private static readonly string FragmentShaderSource = @"
       #version 330 core
    
            uniform vec3 lightColor;
            uniform vec3 lightDir;  
            uniform vec3 viewPos;
            uniform float lightIntensity;
            uniform float shininess;
            uniform vec3 ambientStrength;
            uniform vec3 diffuseStrength;
            uniform vec3 specularStrength;

            out vec4 FragColor;

            in vec4 outCol;
            in vec3 outNormal;
            in vec3 outWorldPosition;

            void main()
            {
                vec3 norm = normalize(outNormal);
                vec3 viewDir = normalize(viewPos - outWorldPosition);
                vec3 lightDirNorm = normalize(-lightDir); 

                vec3 ambient = ambientStrength * lightColor * 0.2;

                float diff = max(dot(norm, lightDirNorm), 0.0);
                vec3 diffuse = diff * diffuseStrength * lightColor * lightIntensity;

                vec3 reflectDir = reflect(-lightDirNorm, norm);
                float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess);
                vec3 specular = spec * specularStrength * lightColor * lightIntensity;

                vec3 result = (ambient + diffuse + specular) * outCol.xyz;
                result = pow(result, vec3(1.0/2.2));
                FragColor = vec4(result, outCol.w);
            }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(800, 600);
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();
            controller = new ImGuiController(Gl, window, inputContext);

            window.FramebufferResize += s =>
            {
                Gl.Viewport(s);
            };

            Gl.ClearColor(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f);

            SetUpObjects();

            LinkProgram();

            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.W:
                    cameraDescriptor.MoveForward();
                    break;
                case Key.S:
                    cameraDescriptor.MoveBackward();
                    break;
                case Key.A:
                    cameraDescriptor.MoveLeft();
                    break;
                case Key.D:
                    cameraDescriptor.MoveRight();
                    break;
                case Key.Space:
                    cameraDescriptor.MoveUp();
                    break;
                case Key.Backspace:
                    cameraDescriptor.MoveDown();
                    break;
                case Key.Left:
                    cameraDescriptor.TurnLeft();
                    break;
                case Key.Right:
                    cameraDescriptor.TurnRight();
                    break;
                case Key.R:
                    Rotation(true);
                    break;
                case Key.L:
                    Rotation(false);
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            if (isRotating)
            {
                float step = rotationDirection ? rotationSpeed : -rotationSpeed;
                currentRotationAngle += step;

                if (Math.Abs(currentRotationAngle - targetRotation) < 0.01f)
                {
                    currentRotationAngle = targetRotation;
                    isRotating = false;
                    ApplyFinalRotation();
                }
            }

            controller.Update((float)deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.ClearColor(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();
            SetLightColor();
            SetLightDirection();
            SetLightIntensity();
            SetViewerPosition();
            SetShininess();
            SetLightProperties();

            DrawRubikCube();

            RenderUI();

            controller.Render();
        }

        private static void RenderUI()
        {
            ImGui.Begin("Lighting Controls", ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.Text("Light Direction");
        ImGui.DragFloat("Dir X", ref lightDirection.X, 0.01f, -1f, 1f);
        ImGui.DragFloat("Dir Y", ref lightDirection.Y, 0.01f, -1f, 1f);
        ImGui.DragFloat("Dir Z", ref lightDirection.Z, 0.01f, -1f, 1f);
        
        ImGui.SliderFloat("Intensity", ref lightIntensity, 0.1f, 2.0f);
        
        ImGui.ColorEdit3("Light Color", ref lightColor);

        ImGui.Separator();
        ImGui.Text("Material Properties");
        ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);
        ImGui.SliderFloat3("Ambient", ref ambientStrength, 0.0f, 0.5f); // Reduced max
        ImGui.SliderFloat3("Diffuse", ref diffuseStrength, 0.0f, 1.0f);
        ImGui.SliderFloat3("Specular", ref specularStrength, 0.0f, 1.0f);

        if (ImGui.Button("Plastic"))
        {
            ambientStrength = new Vector3(0.1f, 0.1f, 0.1f);
            diffuseStrength = new Vector3(0.6f, 0.6f, 0.6f);
            specularStrength = new Vector3(0.8f, 0.8f, 0.8f);
            Shininess = 32;
        }
        ImGui.SameLine();
        if (ImGui.Button("Metal"))
        {
            ambientStrength = new Vector3(0.25f, 0.25f, 0.25f);
            diffuseStrength = new Vector3(0.4f, 0.4f, 0.4f);
            specularStrength = new Vector3(0.9f, 0.9f, 0.9f);
            Shininess = 128;
        }

            ImGui.Separator();
            ImGui.Text("Rotation Controls");
            if (ImGui.Button("Rotate Right (R)"))
            {
                Rotation(true);
            }
            ImGui.SameLine();
            if (ImGui.Button("Rotate Left (L)"))
            {
                Rotation(false);
            }

            ImGui.End();
        }

        private static unsafe void DrawRubikCube()
        {
            float offset = 1.1f;

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        Matrix4X4<float> translation = Matrix4X4.CreateTranslation(
                                (x - 1) * offset,
                                (y - 1) * offset,
                                (z - 1) * offset
                            );

                        if (z == 2)
                        {
                            Matrix4X4<float> rotation = Matrix4X4.CreateRotationZ(currentRotationAngle);
                            translation = translation * rotation;
                        }
                        SetModelMatrix(translation);
                        Gl.BindVertexArray(smallCubes[x, y, z].Vao);
                        Gl.DrawElements(GLEnum.Triangles, smallCubes[x, y, z].IndexArrayLength, GLEnum.UnsignedInt, null);
                        Gl.BindVertexArray(0);
                    }
                }
            }
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            Matrix4X4.Invert(modelMatrix, out var modelInverse);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInverse));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {
            float[][] colors = new float[][]
            {
                new float[] {0.0f, 0.0f, 0.0f, 1.0f},
                new float[] {1.0f, 0.0f, 0.0f, 1.0f},
                new float[] {0.0f, 1.0f, 0.0f, 1.0f},
                new float[] {0.0f, 0.0f, 1.0f, 1.0f},
                new float[] {1.0f, 0.0f, 1.0f, 1.0f},
                new float[] {0.0f, 1.0f, 1.0f, 1.0f},
                new float[] {1.0f, 1.0f, 0.0f, 1.0f}
            };

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        float[] f = colors[0], b = colors[0], u = colors[0], d = colors[0], r = colors[0], l = colors[0];
                        if (z == 2) f = colors[1];
                        if (z == 0) b = colors[5];
                        if (y == 2) u = colors[2];
                        if (y == 0) d = colors[6];
                        if (x == 2) r = colors[3];
                        if (x == 0) l = colors[4];
                        smallCubes[x, y, z] = GlCube.CreateCubeWithFaceColors(Gl, u, f, l, d, b, r, new Vector3D<float>(x, y, z));
                    }
                }
            }
        }

        private static void Window_Closing()
        {
            foreach (var cube in smallCubes)
            {
                cube?.ReleaseGlCube();
            }
        }

        private static unsafe void SetProjectionMatrix()
        {
            float aspectRatio = (float)window.FramebufferSize.X / window.FramebufferSize.Y;
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, aspectRatio, 0.1f, 100);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetLightIntensity()
        {
            int location = Gl.GetUniformLocation(program, "lightIntensity");
            if (location == -1) return;
            Gl.Uniform1(location, lightIntensity);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);
            if (location == -1)
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            Gl.Uniform3(location, lightColor.X, lightColor.Y, lightColor.Z);
            CheckError();
        }

        private static unsafe void SetLightDirection()
        {
            int location = Gl.GetUniformLocation(program, "lightDir");
            if (location == -1)
            {
                Console.WriteLine("ERROR: lightDir uniform not found!");
                return;
            }
            Gl.Uniform3(location, lightDirection.X, lightDirection.Y, lightDirection.Z);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);
            if (location == -1)
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);
            if (location == -1)
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");
            Gl.Uniform1(location, Shininess);
            CheckError();
        }

        private static unsafe void SetLightProperties()
        {
            int ambientLoc = Gl.GetUniformLocation(program, "ambientStrength");
            int diffuseLoc = Gl.GetUniformLocation(program, "diffuseStrength");
            int specularLoc = Gl.GetUniformLocation(program, "specularStrength");

            if (ambientLoc == -1 || diffuseLoc == -1 || specularLoc == -1)
                throw new Exception("One of the light property uniforms was not found.");

            Gl.Uniform3(ambientLoc, ambientStrength.X, ambientStrength.Y, ambientStrength.Z);
            Gl.Uniform3(diffuseLoc, diffuseStrength.X, diffuseStrength.Y, diffuseStrength.Z);
            Gl.Uniform3(specularLoc, specularStrength.X, specularStrength.Y, specularStrength.Z);
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }

        public static void Rotation(bool dir)
        {
            if (!isRotating)
            {
                if (dir)
                {
                    targetRotation += (float)Math.PI / 2f;
                }
                else
                {
                    targetRotation -= (float)Math.PI / 2f;
                }
                isRotating = true;
                rotationDirection = dir;
                rotationSpeed = (float)(Math.PI / 20);
            }
        }

        public static void ApplyFinalRotation()
        {
            foreach (var cube in smallCubes)
            {
                if (cube.Position.Z == 2)
                {
                    float newX = cube.Position.Y;
                    float newY = 2 - cube.Position.X;

                    cube.Position = new Vector3D<float>(newX, newY, cube.Position.Z);
                }
            }
        }
    }
}