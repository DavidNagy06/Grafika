using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        //private static CameraDescriptor cameraDescriptor = new();

        //private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        //private static GlCube glCubeCentered;

        //private static GlCube glCubeRotating;

        private static IInputContext inputContext;
        private static ImGuiController controller;

        private const string NormalMatrixVariableName = "uNormal";
        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        private static System.Numerics.Vector3 lightColor = new(1.0f, 1.0f, 1.0f);
        private static System.Numerics.Vector3 lightPos = new(4.0f, 6.0f, 6.0f);
        private static float shininess = 32.0f;

        private const int CubeCount = 27;
        private static readonly GlCube[] rubikCubes = new GlCube[CubeCount];
        private static readonly Vector3D<float>[] cubePositions = new Vector3D<float>[CubeCount];

        private static readonly Vector3D<int>[] cubeGridCoords = new Vector3D<int>[CubeCount];

        private const string ModelMatrixVariableName = "uModel";
        private const string SliceRotationMatrixVariableName = "uSliceRotation";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static int modelLoc;
        private static int sliceLoc;
        private static int viewLoc;
        private static int projLoc;

        private static Vector3D<float> cameraPos = new Vector3D<float>(0.0f, 3.0f, 7.0f);
        private static float cameraYaw = -90.0f;
        private static float cameraPitch = -20.0f;
        private static readonly HashSet<Key> pressedKeys = new HashSet<Key>();

        private static float currentSliceAngle = 0.0f;
        private static float targetSliceAngle = 0.0f;
        private const float RotationSpeed = 3.5f;
            
        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNorm;

        uniform mat4 uModel;
        uniform mat4 uSliceRotation;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
			outCol = vCol;
            vec4 worldPos = uSliceRotation * uModel * vec4(vPos, 1.0);
            gl_Position = uProjection * uView * worldPos;
            outNormal = uNormal * vNorm;
            outWorldPosition = vec3(worldPos);
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core

        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;
        uniform float shininess;

        out vec4 FragColor;

        in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            float ambientStrength = 0.25;
            vec3 ambient = ambientStrength * lightColor;

            float diffuseStrength = 0.65;
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * diffuseStrength;

            float specularStrength = 0.5;
            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess);
            vec3 specular = specularStrength * spec * lightColor;

            vec3 result = (ambient + diffuse + specular) * outCol.xyz;
            FragColor = vec4(result, outCol.w);
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(800, 800);

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
                keyboard.KeyUp += Keyboard_KeyUp;
            }

            Gl = window.CreateOpenGL();
            controller = new ImGuiController(Gl, window, inputContext);
            window.FramebufferResize += s => Gl.Viewport(s);
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
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed: " + Gl.GetShaderInfoLog(fshader));

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

            modelLoc = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            sliceLoc = Gl.GetUniformLocation(program, SliceRotationMatrixVariableName);
            viewLoc = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            projLoc = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            pressedKeys.Add(key);

            // B) Feladat
            if (key == Key.Space)
            {
                targetSliceAngle += MathF.PI / 2.0f; // +90
            }
            else if (key == Key.Backspace)
            {
                targetSliceAngle -= MathF.PI / 2.0f; // -90
            }
        }

        private static void Keyboard_KeyUp(IKeyboard keyboard, Key key, int arg3)
        {
            pressedKeys.Remove(key);
        }

        private static void Window_Update(double deltaTime)
        {
            float dt = (float)deltaTime;

            float turnSpeed = 75.0f * dt;
            if (pressedKeys.Contains(Key.Left)) cameraYaw -= turnSpeed;
            if (pressedKeys.Contains(Key.Right)) cameraYaw += turnSpeed;
            if (pressedKeys.Contains(Key.Up)) cameraPitch += turnSpeed;
            if (pressedKeys.Contains(Key.Down)) cameraPitch -= turnSpeed;

            cameraPitch = Math.Clamp(cameraPitch, -89.0f, 89.0f);

            float yawRad = MathF.PI / 180.0f * cameraYaw;
            float pitchRad = MathF.PI / 180.0f * cameraPitch;

            Vector3D<float> front;
            front.X = MathF.Cos(pitchRad) * MathF.Cos(yawRad);
            front.Y = MathF.Sin(pitchRad);
            front.Z = MathF.Cos(pitchRad) * MathF.Sin(yawRad);
            front = Vector3D.Normalize(front);

            Vector3D<float> right = Vector3D.Normalize(Vector3D.Cross(front, Vector3D<float>.UnitY));

            float moveSpeed = 5.0f * dt;
            if (pressedKeys.Contains(Key.W)) cameraPos += front * moveSpeed;
            if (pressedKeys.Contains(Key.S)) cameraPos -= front * moveSpeed;
            if (pressedKeys.Contains(Key.D)) cameraPos += right * moveSpeed;
            if (pressedKeys.Contains(Key.A)) cameraPos -= right * moveSpeed;

            if (MathF.Abs(targetSliceAngle - currentSliceAngle) > 0.001f)
            {
                float step = RotationSpeed * dt;
                if (currentSliceAngle < targetSliceAngle)
                {
                    currentSliceAngle = MathF.Min(currentSliceAngle + step, targetSliceAngle);
                }
                else
                {
                    currentSliceAngle = MathF.Max(currentSliceAngle - step, targetSliceAngle);
                }
            }
            controller.Update(dt);
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

            Gl.Uniform3(Gl.GetUniformLocation(program, LightColorVariableName), lightColor.X, lightColor.Y, lightColor.Z);
            Gl.Uniform3(Gl.GetUniformLocation(program, LightPositionVariableName), lightPos.X, lightPos.Y, lightPos.Z);
            Gl.Uniform3(Gl.GetUniformLocation(program, ViewPosVariableName), cameraPos.X, cameraPos.Y, cameraPos.Z);
            Gl.Uniform1(Gl.GetUniformLocation(program, ShininessVariableName), shininess);

            DrawRubikCube();

            ImGui.Begin("Vilagitas es Forgatas", ImGuiWindowFlags.AlwaysAutoResize);

            ImGui.Text("Megvilagitas szinosszetetele:");
            ImGui.SliderFloat("Feny R", ref lightColor.X, 0.0f, 1.0f);
            ImGui.SliderFloat("Feny G", ref lightColor.Y, 0.0f, 1.0f);
            ImGui.SliderFloat("Feny B", ref lightColor.Z, 0.0f, 1.0f);

            ImGui.Separator();
            ImGui.Text("Fenyforras pozicioja:");
            ImGui.InputFloat("X", ref lightPos.X);
            ImGui.InputFloat("Y", ref lightPos.Y);
            ImGui.InputFloat("Z", ref lightPos.Z);

            ImGui.Separator();
            ImGui.Text("Szelet forgatasa:");
            if (ImGui.Button("+90 fok")) targetSliceAngle += MathF.PI / 2.0f;
            ImGui.SameLine();
            if (ImGui.Button("-90 fok")) targetSliceAngle -= MathF.PI / 2.0f;

            ImGui.Separator();
            ImGui.Text("Kamera forgatasa:");
            if (ImGui.Button("Balra")) cameraYaw -= 10.0f;
            ImGui.SameLine();
            if (ImGui.Button("Jobbra")) cameraYaw += 10.0f;
            ImGui.SameLine();
            if (ImGui.Button("Fel")) cameraPitch = Math.Clamp(cameraPitch + 10.0f, -89.0f, 89.0f);
            ImGui.SameLine();
            if (ImGui.Button("Le")) cameraPitch = Math.Clamp(cameraPitch - 10.0f, -89.0f, 89.0f);

            ImGui.End();

            controller.Render();
            DrawRubikCube();

        }

        private static unsafe void DrawRubikCube()
        {
            int sliceLoc = Gl.GetUniformLocation(program, SliceRotationMatrixVariableName);
            if (sliceLoc == -1)
                throw new Exception($"{SliceRotationMatrixVariableName} uniform not found.");

            int normalLoc = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (normalLoc == -1)
                throw new Exception($"{NormalMatrixVariableName} uniform not found.");

            Matrix4X4<float> sliceRotMatrix = Matrix4X4.CreateRotationY(currentSliceAngle);
            Matrix4X4<float> identityMatrix = Matrix4X4<float>.Identity;

            for (int i = 0; i < CubeCount; i++)
            {
                Matrix4X4<float> modelMatrix = Matrix4X4.CreateTranslation(cubePositions[i]);
                SetModelMatrix(modelMatrix);

                Matrix4X4<float> activeSliceRot = (cubeGridCoords[i].Y == 1) ? sliceRotMatrix : identityMatrix;
                Gl.UniformMatrix4(sliceLoc, 1, false, (float*)&activeSliceRot);

                Matrix4X4<float> combined = modelMatrix * activeSliceRot;
                combined.M41 = 0;
                combined.M42 = 0;
                combined.M43 = 0;
                combined.M44 = 1;

                Matrix4X4.Invert(combined, out Matrix4X4<float> inv);
                Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(inv));
                Gl.UniformMatrix3(normalLoc, 1, false, (float*)&normalMatrix);

                Gl.BindVertexArray(rubikCubes[i].Vao);
                Gl.DrawElements(GLEnum.Triangles, rubikCubes[i].IndexArrayLength, GLEnum.UnsignedInt, null);
            }
            Gl.BindVertexArray(0);
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
                        cubeGridCoords[index] = new Vector3D<int>(x, y, z);

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
            float yawRad = MathF.PI / 180.0f * cameraYaw;
            float pitchRad = MathF.PI / 180.0f * cameraPitch;

            Vector3D<float> front;
            front.X = MathF.Cos(pitchRad) * MathF.Cos(yawRad);
            front.Y = MathF.Sin(pitchRad);
            front.Z = MathF.Cos(pitchRad) * MathF.Sin(yawRad);
            front = Vector3D.Normalize(front);

            var viewMatrix = Matrix4X4.CreateLookAt(cameraPos, cameraPos + front, Vector3D<float>.UnitY);
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