using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Szeminarium1_24_02_17_2
{
    internal class GlBoard
    {
        public uint Vao { get; }
        public uint Vertices { get; }
        public uint Colors { get; }
        public uint Indices { get; }
        public uint IndexArrayLength { get; }

        private readonly GL Gl;

        private GlBoard(uint vao, uint vertices, uint colors, uint indices, uint indexArrayLength, GL gl)
        {
            Vao = vao;
            Vertices = vertices;
            Colors = colors;
            Indices = indices;
            IndexArrayLength = indexArrayLength;
            Gl = gl;
        }

        public static unsafe GlBoard CreateBoard(GL gl, float[] color, bool tiltedNormals)
        {
            uint vao = gl.GenVertexArray();
            gl.BindVertexArray(vao);

            float halfWidth = 0.5f;   // Szélesség: 1 egység
            float halfHeight = 1.0f;  // Magasság: 2 egység

            // 10 fok radiánban
            float angleRad = 10.0f * MathF.PI / 180.0f;

            float nxLeft = tiltedNormals ? -MathF.Sin(angleRad) : 0.0f;
            float nxRight = tiltedNormals ? MathF.Sin(angleRad) : 0.0f;
            float nz = tiltedNormals ? MathF.Cos(angleRad) : 1.0f;

            // X-Y síkban definiált téglalap (+Z felé néz, CCW körüljárás)
            // Formátum: X, Y, Z, Nx, Ny, Nz
            float[] vertexArray = new float[]
            {
                -halfWidth,  halfHeight, 0.0f,  nxLeft,  0.0f, nz, // Bal felső (0)
                -halfWidth, -halfHeight, 0.0f,  nxLeft,  0.0f, nz, // Bal alsó  (1)
                 halfWidth, -halfHeight, 0.0f,  nxRight, 0.0f, nz, // Jobb alsó (2)
                 halfWidth,  halfHeight, 0.0f,  nxRight, 0.0f, nz  // Jobb felső(3)
            };

            float[] colorArray = new float[]
            {
                color[0], color[1], color[2], color[3],
                color[0], color[1], color[2], color[3],
                color[0], color[1], color[2], color[3],
                color[0], color[1], color[2], color[3]
            };

            uint[] indexArray = new uint[]
            {
                0, 1, 2,
                0, 2, 3
            };

            uint offsetPos = 0;
            uint offsetNormal = 3 * sizeof(float);
            uint vertexSize = 6 * sizeof(float);

            uint vertices = gl.GenBuffer();
            gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);

            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            gl.EnableVertexAttribArray(0);

            gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);
            gl.EnableVertexAttribArray(2);

            uint colors = gl.GenBuffer();
            gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            gl.EnableVertexAttribArray(1);

            uint indices = gl.GenBuffer();
            gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

            gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            gl.BindVertexArray(0);

            return new GlBoard(vao, vertices, colors, indices, (uint)indexArray.Length, gl);
        }

        internal void Release()
        {
            Gl.DeleteBuffer(Vertices);
            Gl.DeleteBuffer(Colors);
            Gl.DeleteBuffer(Indices);
            Gl.DeleteVertexArray(Vao);
        }
    }
}
