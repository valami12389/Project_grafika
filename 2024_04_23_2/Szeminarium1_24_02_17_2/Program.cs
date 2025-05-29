using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

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

        private static GlObject spaceshipmodell;

        private static GlObject table;

        private static GlCube glCubeRotating;

        private static GlCube skyBox;

        private static Spaceship spaceship = new Spaceship();

        private static float Shininess = 50;
        private static List<Asteroid> asteroids = new List<Asteroid>();

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string TextureUniformVariableName = "uTexture";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        static void Main(string[] args)
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(1000, 1000);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
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
            cameraDescriptor = new CameraDescriptor(spaceship);
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
                keyboard.KeyUp += Keyboard_KeyUp;
            }

            Gl = window.CreateOpenGL();

            controller = new ImGuiController(Gl, window, inputContext);

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                Gl.Viewport(s);
            };


            Gl.ClearColor(System.Drawing.Color.Black);

            SetUpObjects();

            LinkProgram();


            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, ReadShader("VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, ReadShader("FragmentShader.frag"));
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

        private static string ReadShader(string shaderFileName)
        {
            using (Stream shaderStream = typeof(Program).Assembly.GetManifestResourceStream("Szeminarium1_24_02_17_2.Shaders." + shaderFileName))
            using (StreamReader shaderReader = new StreamReader(shaderStream))
                return shaderReader.ReadToEnd();
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    spaceship.isMovingLeft = true;
                    break;
                case Key.Right:
                    spaceship.isMovingRight = true;
                    break;
                case Key.Up:
                    spaceship.isMovingForward = true;
                    break;
                case Key.Down:
                    spaceship.isMovingBackward = true;
                    break;
                case Key.U:
                    spaceship.isMovingUp = true;
                    break;
                case Key.D:
                    spaceship.isMovingDown = true;
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                    break;
                case Key.C:
                    cameraDescriptor.ToggleCameraMode();
                    break;
            }
        }

        private static void Keyboard_KeyUp(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    spaceship.isMovingLeft = false;
                    spaceship.StopMovingLeftRight();
                    break;
                case Key.Right:
                    spaceship.isMovingRight = false;
                    spaceship.StopMovingLeftRight();
                    break;
                case Key.Up:
                    spaceship.isMovingForward = false;
                    break;
                case Key.Down:
                    spaceship.isMovingBackward = false;
                    break;
                case Key.U:
                    spaceship.isMovingUp = false;
                    spaceship.StopMovingUpDown();
                    break;
                case Key.D:
                    spaceship.isMovingDown = false;
                    spaceship.StopMovingUpDown();
                    break;
            }
        }
        private static unsafe void DrawAsteroids()
        {


            foreach (var asteroid in asteroids)
            {
                int shininessLoc = Gl.GetUniformLocation(program, ShininessVariableName);
                Gl.Uniform1(shininessLoc, 100.0f);

                // Create transformation matrix
                Matrix4X4<float> scale = Matrix4X4.CreateScale(asteroid.Scale);
                Matrix4X4<float> rotation = Matrix4X4.CreateFromAxisAngle(asteroid.RotationAxis, asteroid.RotationAngle);
                Matrix4X4<float> translation = Matrix4X4.CreateTranslation(asteroid.Position);

                Matrix4X4<float> modelMatrix = scale * rotation * translation;
                SetModelMatrix(modelMatrix);

                Gl.BindVertexArray(asteroid.GlObject.Vao);
                Gl.DrawElements(GLEnum.Triangles, asteroid.GlObject.IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);

                Gl.Uniform1(shininessLoc, Shininess);
            }
        }

        private static void Window_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            spaceship.Update((float)deltaTime);


            Console.WriteLine($"Frame update - Health: {spaceship.CurrentHealth}, Invuln: {spaceship.invulnerabilityTimer}");

            List<Asteroid> asteroidsToRemove = new List<Asteroid>();
            foreach (var asteroid in asteroids)
            {

                asteroid.Direction = Vector3D.Normalize(spaceship.Position - asteroid.Position);
                asteroid.Position += asteroid.Direction * asteroid.Speed * (float)deltaTime;
                asteroid.RotationAngle += 0.01f;

                if (!asteroid.HasCausedDamage && CheckCollisionAdjustable(spaceship, asteroid))
                {
                    asteroid.HasCausedDamage = true;
                    spaceship.TakeDamage(10f);
                    Vector3D<float> knockbackDirection = Vector3D.Normalize(spaceship.Position - asteroid.Position);
                    float knockbackForce = 200.0f;
                    spaceship.ApplyKnockback(knockbackDirection, knockbackForce);
                    asteroidsToRemove.Add(asteroid);
                }
            }

            foreach (var asteroid in asteroidsToRemove)
            {
                asteroid.GlObject.ReleaseGlObject();
                asteroids.Remove(asteroid);
                AddNewAsteroid();
            }

            for (int i = asteroids.Count - 1; i >= 0; i--)
            {
                if (asteroids[i].Position.Z > 200f)
                {
                    asteroids[i].GlObject.ReleaseGlObject();
                    asteroids.RemoveAt(i);
                    AddNewAsteroid();
                }
            }

            controller.Update((float)deltaTime);
        }

        private static void AddNewAsteroid()
        {
            float[] asteroidColor = [0.85f, 0.85f, 0.85f, 1.0f];

            Vector3D<float> playerPosition = cameraDescriptor.Position;

            var position = new Vector3D<float>(
                (float)(Random.Shared.NextDouble() * 400),
                (float)(Random.Shared.NextDouble() * 400),
                spaceship.Position.Z - 300
            );

            float scale = (float)(Random.Shared.NextDouble() * 2.5 + 0.5);

            float speed = (float)(Random.Shared.NextDouble() * 10 + 1);

            var asteroidObj = ObjResourceReader.CreateAsteroidWithColor(Gl, asteroidColor);
            asteroids.Add(new Asteroid(asteroidObj, position, scale, speed, spaceship.Position));
        }

        private static unsafe void Window_Render(double deltaTime)
        {

            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();
            DrawAsteroids();

            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();

            DrawPulsingSpaceShip();

            DrawSkyBox();

            ImGuiNET.ImGui.Begin("Health Status", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);
            ImGuiNET.ImGui.SetWindowPos(new System.Numerics.Vector2(10, 10));
            ImGuiNET.ImGui.SetWindowSize(new System.Numerics.Vector2(200, 50));

            float healthPercentage = spaceship.CurrentHealth / spaceship.MaxHealth;
            System.Numerics.Vector4 healthColor = healthPercentage > 0.6f ?
                new System.Numerics.Vector4(0, 1, 0, 1) :
                healthPercentage > 0.3f ?
                new System.Numerics.Vector4(1, 1, 0, 1) :
                new System.Numerics.Vector4(1, 0, 0, 1);

            ImGuiNET.ImGui.Text("Spaceship Health:");
            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.PlotHistogram, healthColor);
            ImGuiNET.ImGui.ProgressBar(healthPercentage,
                new System.Numerics.Vector2(180, 20),
                $"{spaceship.CurrentHealth}/{spaceship.MaxHealth}");
            ImGuiNET.ImGui.PopStyleColor();

            ImGuiNET.ImGui.Text("Spaceship Health:");
            ImGuiNET.ImGui.PushStyleColor(ImGuiCol.PlotHistogram, healthColor);
            ImGuiNET.ImGui.ProgressBar(healthPercentage,
                new System.Numerics.Vector2(180, 20),
                $"{spaceship.CurrentHealth}/{spaceship.MaxHealth}");
            ImGuiNET.ImGui.PopStyleColor();


            controller.Render();
        }

        private static unsafe void DrawSkyBox()
        {

            int isSkyboxLocation = Gl.GetUniformLocation(program, "isSkybox");
            if (isSkyboxLocation != -1)
            {
                Gl.Uniform1(isSkyboxLocation, 1);
            }

            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(400f);
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(skyBox.Vao);

            int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureUniformVariableName} uniform not found on shader.");
            }
            // set texture 0
            Gl.Uniform1(textureLocation, 0);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
            Gl.BindTexture(TextureTarget.Texture2D, skyBox.Texture.Value);

            Gl.DrawElements(GLEnum.Triangles, skyBox.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            CheckError();
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            CheckError();

            if (isSkyboxLocation != -1)
            {
                Gl.Uniform1(isSkyboxLocation, 0); 
            }
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 1.2f, 1.2f, 1.0f);
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 0f, 10f, 0f);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);

            if (location == -1)
            {
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");
            }

            Gl.Uniform1(location, Shininess);
            CheckError();
        }
        private static unsafe void DrawPulsingSpaceShip()
        {
            if (spaceship.invulnerabilityTimer > 0 && (int)(spaceship.invulnerabilityTimer * 10) % 2 == 0)
            {
                Gl.Uniform4(Gl.GetUniformLocation(program, "uColor"), 1f, 0.5f, 0.5f, 1f);
            }

            var scale = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            var rotationX = Matrix4X4.CreateRotationX(spaceship.RollAngle);
            var rotationZ = Matrix4X4.CreateRotationZ(spaceship.PitchAngle);
            var translation = Matrix4X4.CreateTranslation(spaceship.Position);
            var modelMatrix = scale * rotationX * rotationZ * translation;
            SetModelMatrix(modelMatrix);

            Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(GLEnum.Back);

            Gl.Uniform4(Gl.GetUniformLocation(program, "uMaterial"), 1f, 1f, 1f, 1f);
            Gl.BindVertexArray(spaceshipmodell.Vao);
            Gl.DrawElements(GLEnum.Triangles, spaceshipmodell.IndexArrayLength, GLEnum.UnsignedInt, null);

            Gl.Disable(EnableCap.CullFace);
            Gl.BindVertexArray(0);
            Gl.Uniform4(Gl.GetUniformLocation(program, "uColor"), 1f, 1f, 1f, 1f);
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

            Matrix4X4<float> modelMatrixWithoutTranslation = new Matrix4X4<float>(
                 modelMatrix.M11, modelMatrix.M12, modelMatrix.M13, 0,
                 modelMatrix.M21, modelMatrix.M22, modelMatrix.M23, 0,
                 modelMatrix.M31, modelMatrix.M32, modelMatrix.M33, 0,
                 0, 0, 0, 1
             );

            Matrix4X4.Invert(modelMatrixWithoutTranslation, out var modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {
            float[] asteroidColor = [0.85f, 0.85f, 0.85f, 1.0f];
            float[] face1Color = [1f, 0f, 0f, 1.0f];
            float[] face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 1.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 1.0f, 1.0f];
            float[] face6Color = [1.0f, 1.0f, 0.0f, 1.0f];

            Vector3D<float> initialCameraPosition = cameraDescriptor.Position;

            for (int i = 0; i < 100; i++)
            {
                var position = new Vector3D<float>(
                    (float)(Random.Shared.NextDouble() * 400),
                    (float)(Random.Shared.NextDouble() * 400),
                    (float)(Random.Shared.NextDouble() * 400)
                );

                float scale = (float)(Random.Shared.NextDouble() * 3.0 + 1.0);
                float speed = (float)(Random.Shared.NextDouble() * 10 + 1);

                var asteroidObj = ObjResourceReader.CreateAsteroidWithColor(Gl, asteroidColor);
                asteroids.Add(new Asteroid(asteroidObj, position, scale, speed, initialCameraPosition));
            }

            spaceshipmodell = ObjResourceReader.CreateSpaceshipWithColor(Gl, face1Color);

            float[] tableColor = [System.Drawing.Color.Azure.R/256f,
                          System.Drawing.Color.Azure.G/256f,
                          System.Drawing.Color.Azure.B/256f,
                          1f];
            table = GlCube.CreateSquare(Gl, tableColor);

            glCubeRotating = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color,
                                                            face4Color, face5Color, face6Color);

            skyBox = GlCube.CreateInteriorCube(Gl, "");
        }



        private static void Window_Closing()
        {
            foreach (var asteroid in asteroids)
            {
                asteroid.GlObject.ReleaseGlObject();
            }
            asteroids.Clear();
            spaceshipmodell.ReleaseGlObject();
            glCubeRotating.ReleaseGlObject();
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 1000);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static bool CheckCollisionAdjustable(Spaceship spaceship, Asteroid asteroid)
        {
            const float SPACESHIP_COLLISION_SCALE = 0.25f;
            const float ASTEROID_COLLISION_SCALE = 0.7f;
            const float DISTANCE_MULTIPLIER = 0.8f;

            var distance = Vector3D.Distance(spaceship.Position, asteroid.Position);
            var maxAllowedDistance = (spaceship.BoundingBoxSize.X * SPACESHIP_COLLISION_SCALE +
                                     asteroid.Scale * ASTEROID_COLLISION_SCALE) * DISTANCE_MULTIPLIER;

            return distance < maxAllowedDistance;
        }

        private static unsafe void SetViewMatrix()
        {
            Matrix4X4<float> viewMatrix = Matrix4X4.CreateLookAt(
               cameraDescriptor.Position,
               cameraDescriptor.Target,
               cameraDescriptor.UpVector
           );

            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

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