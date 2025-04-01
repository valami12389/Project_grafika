using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        private static GlCube[,,] smallCubes = new GlCube[3, 3, 3];

        private static GlCube glCubeRotating;

        private static float targetRotation=0f;

        private static bool isRotating = false;  
        private static float currentRotationAngle = 0; 
        private static float rotationSpeed = (float)(Math.PI / 100f);
        private static bool rotationDirection;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(500, 500);

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
            IInputContext inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);

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
                    ;
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

                if (Math.Abs(currentRotationAngle-targetRotation)<0.01f)
                {
                    currentRotationAngle = targetRotation;
                    isRotating = false;
                    ApplyFinalRotation();
                }
            }
        }

        private static unsafe void Window_Render(double deltaTime)
        {

            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            DrawRubikCube();


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

                        if ( z == 2) 
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
        }

        private static unsafe void SetUpObjects()
        {
            float[][] colors= new float[][]
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
                        smallCubes[x, y, z] = GlCube.CreateCubeWithFaceColors(Gl, u, f, l, d, b, r,new Vector3D<float>(x,y,z));
                    }
                }
            }

        }

        

        private static void Window_Closing()
        {
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
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
                else {
                    targetRotation -= (float)Math.PI / 2f;
                }
                isRotating = true;
                rotationDirection = dir;
                rotationSpeed = (float)(Math.PI / 20);
            }
        }

        public static void ApplyFinalRotation()
        {

            foreach (var cube in smallCubes) {
                if (cube.Position.Z == 2) {
                    float newX =  cube.Position.Y;
                    float newY = 2-cube.Position.X;

                    cube.Position = new Vector3D<float>(newX, newY, cube.Position.Z);
                }
            }
        }


    }
}