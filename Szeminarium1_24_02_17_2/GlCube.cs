using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Szeminarium1_24_02_17_2
{
    internal class GlCube
    {
        public uint Vao { get; }
        public uint Colors { get; }

        private static uint vertices = 0;
        private static uint indices = 0;
        private static uint indexArrayLength = 0;

        public uint IndexArrayLength => indexArrayLength;

        private GL Gl;

        private GlCube(uint vao, uint colors, GL gl)
        {
            this.Vao = vao;
            this.Colors = colors;
            this.Gl = gl;
        }

        public static unsafe GlCube CreateCubeWithFaceColors(GL Gl, float[] face1Color, float[] face2Color, float[] face3Color, float[] face4Color, float[] face5Color, float[] face6Color)
        {
            if (vertices == 0)
            {
                // counter clockwise is front facing
                float[] vertexArray = new float[] {
                    -0.5f,  0.5f,  0.5f,  0f,  1f,  0f,
                     0.5f,  0.5f,  0.5f,  0f,  1f,  0f,
                     0.5f,  0.5f, -0.5f,  0f,  1f,  0f,
                    -0.5f,  0.5f, -0.5f,  0f,  1f,  0f,

                    -0.5f,  0.5f,  0.5f,  0f,  0f,  1f,
                    -0.5f, -0.5f,  0.5f,  0f,  0f,  1f,
                     0.5f, -0.5f,  0.5f,  0f,  0f,  1f,
                     0.5f,  0.5f,  0.5f,  0f,  0f,  1f,

                    -0.5f,  0.5f,  0.5f, -1f,  0f,  0f,
                    -0.5f,  0.5f, -0.5f, -1f,  0f,  0f,
                    -0.5f, -0.5f, -0.5f, -1f,  0f,  0f,
                    -0.5f, -0.5f,  0.5f, -1f,  0f,  0f,

                    -0.5f, -0.5f,  0.5f,  0f, -1f,  0f,
                     0.5f, -0.5f,  0.5f,  0f, -1f,  0f,
                     0.5f, -0.5f, -0.5f,  0f, -1f,  0f,
                    -0.5f, -0.5f, -0.5f,  0f, -1f,  0f,

                    0.5f,  0.5f, -0.5f,  0f,  0f, -1f,
                    -0.5f,  0.5f, -0.5f,  0f,  0f, -1f,
                    -0.5f, -0.5f, -0.5f,  0f,  0f, -1f,
                     0.5f, -0.5f, -0.5f,  0f,  0f, -1f,

                    0.5f,  0.5f,  0.5f,  1f,  0f,  0f,
                    0.5f,  0.5f, -0.5f,  1f,  0f,  0f,
                    0.5f, -0.5f, -0.5f,  1f,  0f,  0f,
                    0.5f, -0.5f,  0.5f,  1f,  0f,  0f,
                };

                uint[] indexArray = new uint[] {
                    0, 1, 2,
                    0, 2, 3,

                    4, 5, 6,
                    4, 6, 7,

                    8, 9, 10,
                    10, 11, 8,

                    12, 14, 13,
                    12, 15, 14,

                    17, 16, 19,
                    17, 19, 18,

                    20, 22, 21,
                    20, 23, 22
                };

                indexArrayLength = (uint)indexArray.Length;

                vertices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);

                indices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
                Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            }

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            uint offsetPos = 0;
            uint offsetNormal = 3 * sizeof(float);
            uint vertexSize = 6 * sizeof(float);

            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);
            Gl.EnableVertexAttribArray(2);

            List<float> colorsList = new List<float>();
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face2Color);
            colorsList.AddRange(face2Color);
            colorsList.AddRange(face2Color);
            colorsList.AddRange(face2Color);

            colorsList.AddRange(face3Color);
            colorsList.AddRange(face3Color);
            colorsList.AddRange(face3Color);
            colorsList.AddRange(face3Color);

            colorsList.AddRange(face4Color);
            colorsList.AddRange(face4Color);
            colorsList.AddRange(face4Color);
            colorsList.AddRange(face4Color);

            colorsList.AddRange(face5Color);
            colorsList.AddRange(face5Color);
            colorsList.AddRange(face5Color);
            colorsList.AddRange(face5Color);

            colorsList.AddRange(face6Color);
            colorsList.AddRange(face6Color);
            colorsList.AddRange(face6Color);
            colorsList.AddRange(face6Color);

            float[] colorArray = colorsList.ToArray();

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            Gl.BindVertexArray(0);

            return new GlCube(vao, colors, Gl);
        }

        internal void ReleaseGlCube()
        {
            Gl.DeleteBuffer(Colors);
            Gl.DeleteVertexArray(Vao);
        }

        public static void ReleaseSharedResources(GL gl)
        {
            if (vertices != 0)
            {
                gl.DeleteBuffer(vertices);
                vertices = 0;
            }
            if (indices != 0)
            {
                gl.DeleteBuffer(indices);
                indices = 0;
            }
        }
    }
}