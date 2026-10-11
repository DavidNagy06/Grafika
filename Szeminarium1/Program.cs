using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;

namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static uint program;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
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
            windowOptions.Title = "1. szeminárium - háromszög";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;

            graphicWindow.Run();
        }

        private static bool CheckGlError(string stepName)
        {
            bool hasError = false;
            GLEnum err;
            while ((err = Gl.GetError()) != GLEnum.NoError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[GL Error] Step: '{stepName}' -> Error: {err} (0x{(int)err:X})");
                Console.ResetColor();
                hasError = true;
            }
            return hasError;
        }

        private static void GraphicWindow_Load()
        {
            // egszeri beallitasokat
            //Console.WriteLine("Loaded");

            Gl = graphicWindow.CreateOpenGL();
            CheckGlError("OpenGL context initialization");

            Gl.ClearColor(System.Drawing.Color.White);
            CheckGlError("Set clear color");

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader); //System.Exception: 'Vertex shader failed to compile: ' kivetelt dob, ha ez nem lenne
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            CheckGlError("Vertex shader compilation");

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
            {
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));
            }
            CheckGlError("Fragment shader compilation");

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.GetProgram(program, GLEnum.LinkStatus, out var linkStatus);
            if (linkStatus == 0)
            {
                throw new Exception("Shader program linking failed: " + Gl.GetProgramInfoLog(program));
            }
            CheckGlError("Shader program linking");

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
            CheckGlError("Shader cleanup");

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }

        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO GL
            // make it threadsave
            //Console.WriteLine($"Update after {deltaTime} [s]");
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s]");

            Gl.Clear(ClearBufferMask.ColorBufferBit);
            CheckGlError("Clear buffer");

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            CheckGlError("Bind VAO");

            float[] vertexArray = new float[] {
                -0.5f, -0.5f, 0.0f,
                +0.5f, -0.5f, 0.0f,
                 0.0f, +0.5f, 0.0f,
                 1f, 1f, 0f
            };

            //Tul keves adat a koordinatakban
            //float[] vertexArray = new float[] {
            //    -0.5f, -0.5f, 0.0f,
            //    +0.5f, -0.5f, 0.0f,
            //     0.0f, +0.5f, 0.0f
            // a 4es pontot bar kitoroltem, az indexArray meg hivatkozik a 3as indexre, emiatt a BufferData kevesebb bajtot masol a GPU-ra
            //};

            float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
            };

            uint[] indexArray = new uint[] {
                0, 1, 2,
                2, 1, 3
            };

            //Ervenytelen index beirasa az indexArraybe (9es index nem letezik)
            //uint[] indexArray = new uint[]
            //{
            //    0, 1, 2,
            //    2, 1, 9
            //};

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);

            //Gl.BindBuffer(GLEnum.ElementArrayBuffer, vertices); A VertexAttribPointer hibat jelez, mert a vertex attributumok beallitasahoz kotelezo, hogy egy ArrayBuffer legyen aktiv

            //Gl.BindBuffer(GLEnum.ArrayBuffer, vertices); Ha ez a sor nincs, az OpenGL nem engedi az adatfeltoltest, mivel a 0-s puffer van bekotve, emiatt hibat dob

            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0);
            //Gl.EnableVertexAttribArray(50);
            CheckGlError("Configure position VBO (attribute 0)");

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1); //Ha ez nem lenne, a shader nem kapja meg a csucspont szineket, a haromszogek fekete szinuek lesznek a szinatmenet helyett
            CheckGlError("Configure color VBO (attribute 1)");

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            CheckGlError("Configure index buffer (EBO)");

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            Gl.UseProgram(program);

            Gl.DrawElements(GLEnum.Triangles, (uint)indexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
            CheckGlError("Draw elements");

            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(vao);

            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteVertexArray(vao);
            CheckGlError("Buffer deletion cleanup");
        }
    }
}
