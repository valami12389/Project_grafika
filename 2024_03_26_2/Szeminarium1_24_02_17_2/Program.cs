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
        private static GlCube glCubeCentered;
        private static GlCube glCubeRotating;

        // Anyagtulajdonságok
        private static float Shininess = 50;
        private static Vector3 ambientStrength = new Vector3(0.2f, 0.2f, 0.2f);
        private static Vector3 diffuseStrength = new Vector3(0.5f, 0.5f, 0.5f);
        private static Vector3 specularStrength = new Vector3(1.0f, 1.0f, 1.0f);
        private static Vector3 backgroundColor = new Vector3(1f, 1f, 1f);

        // Kocka színek
        private static int selectedFaceColorIndex = 0;
        private static string selectedFaceColorName = "Red";
        private static bool cubeFaceColorChanged = false;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

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

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        
        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;
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
            // Ambient komponens
            vec3 ambient = ambientStrength * lightColor;

            // Diffúz komponens
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * diffuseStrength * lightColor;

            // Spekuláris komponens
            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess);
            vec3 specular = spec * specularStrength * lightColor;

            // Kombinált eredmény
            vec3 result = (ambient + diffuse + specular) * outCol.xyz;
            FragColor = vec4(result, outCol.w);
        }
        ";

        private static readonly Vector4[] predefinedColors = new Vector4[]
        {
            new Vector4(1f, 0f, 0f, 1f), // Red
            new Vector4(0f, 1f, 0f, 1f), // Green
            new Vector4(0f, 0f, 1f, 1f), // Blue
            new Vector4(1f, 1f, 0f, 1f), // Yellow
            new Vector4(1f, 0f, 1f, 1f), // Magenta
            new Vector4(0f, 1f, 1f, 1f), // Cyan
        };

        private static readonly string[] predefinedColorNames = new string[]
        {
            "Red",
            "Green",
            "Blue",
            "Yellow",
            "Magenta",
            "Cyan"
        };

        private static void UpdateCubeFaceColor()
        {
            selectedFaceColorName = predefinedColorNames[selectedFaceColorIndex];
            cubeFaceColorChanged = true;
        }

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Phong Modell Szeminárium";
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
                case Key.Left:
                    cameraDescriptor.DecreaseZYAngle();
                    break;
                case Key.Right:
                    cameraDescriptor.IncreaseZYAngle();
                    break;
                case Key.Down:
                    cameraDescriptor.IncreaseDistance();
                    break;
                case Key.Up:
                    cameraDescriptor.DecreaseDistance();
                    break;
                case Key.U:
                    cameraDescriptor.IncreaseZXAngle();
                    break;
                case Key.D:
                    cameraDescriptor.DecreaseZXAngle();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            controller.Update((float)deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            if (cubeFaceColorChanged)
            {
                RegenerateCube();
                cubeFaceColorChanged = false;
            }

            Gl.ClearColor(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();
            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();
            SetLightProperties();

            DrawPulsingCenterCube();
            DrawRevolvingCube();

            RenderUI();

            controller.Render();
        }

        private static void RegenerateCube()
        {
            Vector4 selectedColor = predefinedColors[selectedFaceColorIndex];

            if (glCubeRotating != null)
                glCubeRotating.ReleaseGlCube();

            glCubeRotating = GlCube.CreateCubeWithFaceColors(
                Gl,
                new float[] { selectedColor.X, selectedColor.Y, selectedColor.Z, selectedColor.W },
                new float[] { selectedColor.X, selectedColor.Y, selectedColor.Z, selectedColor.W },
                new float[] { selectedColor.X, selectedColor.Y, selectedColor.Z, selectedColor.W },
                new float[] { selectedColor.X, selectedColor.Y, selectedColor.Z, selectedColor.W },
                new float[] { selectedColor.X, selectedColor.Y, selectedColor.Z, selectedColor.W },
                new float[] { selectedColor.X, selectedColor.Y, selectedColor.Z, selectedColor.W }
            );
        }

        private static void RenderUI()
        {
            ImGui.Begin("Anyagtulajdonságok", ImGuiWindowFlags.AlwaysAutoResize);

            // Fénytulajdonságok
            ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);
            ImGui.SliderFloat3("Ambient Strength", ref ambientStrength, 0.0f, 1.0f);
            ImGui.SliderFloat3("Diffuse Strength", ref diffuseStrength, 0.0f, 1.0f);
            ImGui.SliderFloat3("Specular Strength", ref specularStrength, 0.0f, 1.0f);

            // Háttérszín
            ImGui.ColorEdit3("Background Color", ref backgroundColor);

            // Kocka szín választó
            if (ImGui.BeginCombo("Face Color", selectedFaceColorName))
            {
                for (int i = 0; i < predefinedColors.Length; i++)
                {
                    bool isSelected = (selectedFaceColorIndex == i);
                    if (ImGui.Selectable(predefinedColorNames[i], isSelected))
                    {
                        selectedFaceColorIndex = i;
                        UpdateCubeFaceColor();
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            // Anyag beállítások a link alapján
            if (ImGui.Button("Rubber"))
            {
                ambientStrength = new Vector3(0.05f, 0.05f, 0.05f);
                diffuseStrength = new Vector3(0.5f, 0.5f, 0.5f);
                specularStrength = new Vector3(0.7f, 0.7f, 0.7f);
                Shininess = 32;
            }
            ImGui.SameLine();
            if (ImGui.Button("Metal"))
            {
                ambientStrength = new Vector3(0.25f, 0.25f, 0.25f);
                diffuseStrength = new Vector3(0.4f, 0.4f, 0.4f);
                specularStrength = new Vector3(0.774597f, 0.774597f, 0.774597f);
                Shininess = 76.8f;
            }

            ImGui.End();
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

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);
            if (location == -1)
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            Gl.Uniform3(location, 1f, 1f, 1f);
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);
            if (location == -1)
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            Gl.Uniform3(location, 0f, 2f, 0f);
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

        private static unsafe void DrawRevolvingCube()
        {
            Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(0.25f);
            Matrix4X4<float> rotx = Matrix4X4.CreateRotationX((float)Math.PI / 4f);
            Matrix4X4<float> rotz = Matrix4X4.CreateRotationZ((float)Math.PI / 4f);
            Matrix4X4<float> rotLocY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleOwnRevolution);
            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(1f, 1f, 0f);
            Matrix4X4<float> rotGlobY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleRevolutionOnGlobalY);
            Matrix4X4<float> modelMatrix = diamondScale * rotx * rotz * rotLocY * trans * rotGlobY;

            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(glCubeRotating.Vao);
            Gl.DrawElements(GLEnum.Triangles, glCubeRotating.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawPulsingCenterCube()
        {
            var modelMatrixForCenterCube = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            SetModelMatrix(modelMatrixForCenterCube);
            Gl.BindVertexArray(glCubeCentered.Vao);
            Gl.DrawElements(GLEnum.Triangles, glCubeCentered.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
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
            float[] face1Color = [1.0f, 0.0f, 0.0f, 1.0f];
            float[] face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 1.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 1.0f, 1.0f];
            float[] face6Color = [1.0f, 1.0f, 0.0f, 1.0f];

            glCubeCentered = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);
            glCubeRotating = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face1Color, face1Color, face1Color, face1Color, face1Color);
        }

        private static void Window_Closing()
        {
            glCubeCentered.ReleaseGlCube();
            glCubeRotating.ReleaseGlCube();
        }

        private static unsafe void SetProjectionMatrix()
        {
            float aspectRatio = (float)window.FramebufferSize.X / window.FramebufferSize.Y;
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>(
                (float)Math.PI / 4f, aspectRatio, 0.1f, 100.0f);

            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            if (location == -1)
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");
            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            if (location == -1)
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}