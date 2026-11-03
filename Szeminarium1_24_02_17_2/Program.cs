using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();

        //private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        //private static GlCube glCubeCentered;

        //private static GlCube glCubeRotating;

        private const int CubeCount = 27;
        private static readonly GlCube[] rubikCubes = new GlCube[CubeCount];
        private static readonly Vector3D<float>[] cubePositions = new Vector3D<float>[CubeCount];

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
            //Console.WriteLine("Load");

            // set up input handling
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

            for (int i = 0; i < 18; i++) cameraDescriptor.IncreaseDistance();
            for (int i = 0; i < 7; i++) cameraDescriptor.IncreaseZYAngle();
            for (int i = 0; i < 5; i++) cameraDescriptor.IncreaseZXAngle();
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
                    ;
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
                //case Key.Space:
                //    cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                //    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            //cubeArrangementModel.AdvanceTime(deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            DrawRubikCube();

        }

        //private static unsafe void DrawRevolvingCube()
        //{
        //    Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(0.25f);
        //    Matrix4X4<float> rotx = Matrix4X4.CreateRotationX((float)Math.PI / 4f);
        //    Matrix4X4<float> rotz = Matrix4X4.CreateRotationZ((float)Math.PI / 4f);
        //    Matrix4X4<float> rotLocY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleOwnRevolution);
        //    Matrix4X4<float> trans = Matrix4X4.CreateTranslation(1f, 1f, 0f);
        //    Matrix4X4<float> rotGlobY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleRevolutionOnGlobalY);
        //    Matrix4X4<float> modelMatrix = diamondScale * rotx * rotz * rotLocY * trans * rotGlobY;

        //    SetModelMatrix(modelMatrix);
        //    Gl.BindVertexArray(glCubeRotating.Vao);
        //    Gl.DrawElements(GLEnum.Triangles, glCubeRotating.IndexArrayLength, GLEnum.UnsignedInt, null);
        //    Gl.BindVertexArray(0);
        //}

        //private static unsafe void DrawPulsingCenterCube()
        //{
        //    var modelMatrixForCenterCube = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
        //    SetModelMatrix(modelMatrixForCenterCube);
        //    Gl.BindVertexArray(glCubeCentered.Vao);
        //    Gl.DrawElements(GLEnum.Triangles, glCubeCentered.IndexArrayLength, GLEnum.UnsignedInt, null);
        //    Gl.BindVertexArray(0);
        //}

        private static unsafe void DrawRubikCube()
        {
            for (int i = 0; i < CubeCount; i++)
            {
                Matrix4X4<float> modelMatrix = Matrix4X4.CreateTranslation(cubePositions[i]);
                SetModelMatrix(modelMatrix);

                Gl.BindVertexArray(rubikCubes[i].Vao);
                Gl.DrawElements(GLEnum.Triangles, rubikCubes[i].IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);
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

            float[] colWhite = [1.0f, 1.0f, 1.0f, 1.0f];
            float[] colGreen = [0.0f, 0.6f, 0.2f, 1.0f];
            float[] colOrange = [1.0f, 0.4f, 0.0f, 1.0f];
            float[] colYellow = [1.0f, 0.85f, 0.0f, 1.0f];
            float[] colBlue = [0.0f, 0.3f, 0.85f, 1.0f];
            float[] colRed = [0.85f, 0.0f, 0.0f, 1.0f];

            float[] colInside = [0.08f, 0.08f, 0.08f, 1.0f];

            float spacing = 1.05f;

            int index = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        cubePositions[index] = new Vector3D<float>(x * spacing, y * spacing, z * spacing);

                        float[] fTop = (y == 1) ? colWhite : colInside;
                        float[] fFront = (z == 1) ? colGreen : colInside;
                        float[] fLeft = (x == -1) ? colOrange : colInside;
                        float[] fBottom = (y == -1) ? colYellow : colInside;
                        float[] fBack = (z == -1) ? colBlue : colInside;
                        float[] fRight = (x == 1) ? colRed : colInside;

                        rubikCubes[index] = GlCube.CreateCubeWithFaceColors(
                            Gl, fTop, fFront, fLeft, fBottom, fBack, fRight);

                        index++;
                    }
                }
            }
        }

        

        private static void Window_Closing()
        {
            for (int i = 0; i < CubeCount; i++)
            {
                rubikCubes[i]?.ReleaseGlCube();
            }
            GlCube.ReleaseSharedResources(Gl);
            Gl.DeleteProgram(program);
        }

        private static unsafe void SetProjectionMatrix()
        {
            float aspect = (float)window.Size.X / window.Size.Y;
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, aspect, 0.1f, 100f);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");
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
    }
}