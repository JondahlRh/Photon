// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: ChatGpt for summary
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.Versioning;
using Avalonia.Animation.Easings;
using OpenTK.Graphics.ES30;
using Photon.Core.Geometry;
using Photon.Core.Mathematics;

namespace Photon.App.Platform.OpenGL;

/// <summary>
/// Renders triangle geometry and material colors from a model using OpenGL.
/// </summary>
internal sealed class TriangleRenderer
{
    /// <summary>
    /// OpenGL vertex array object.
    /// </summary>
    private int _vao;

    /// <summary>
    /// OpenGL vertex buffer object.
    /// </summary>
    private int _vbo;

    /// <summary>
    /// OpenGL shader program.
    /// </summary>
    private int _shaderProgram;

    /// <summary>
    /// Number of floats stored for each vertex: position, normal, and color.
    /// </summary>
    private const int FloatsPerVertex = 9;

    /// <summary>
    /// Number of vertices currently uploaded to the GPU.
    /// </summary>
    private int _vertexCount;

    /// <summary>
    /// Location of the camera's view-projection uniform.
    /// </summary>
    private int _viewProjectionLocation;

    /// <summary>
    /// Location of the light position uniform.
    /// </summary>
    private int _lightPositionLocation;

    /// <summary>
    /// Initializes GPU resources without loading a model.
    /// </summary>
    /// <param name="isOpenGlEs">Whether the context uses OpenGL ES.</param>
    public void Initialize(bool isOpenGlEs)
    {
        try
        {
            _shaderProgram = CreateShaderProgram(isOpenGlEs);

            _viewProjectionLocation = GL.GetUniformLocation(_shaderProgram, "viewProjection");

            _lightPositionLocation = GL.GetUniformLocation(_shaderProgram, "lightPosition");

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

            int stride = FloatsPerVertex * sizeof(float);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, IntPtr.Zero);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(
                1,
                3,
                VertexAttribPointerType.Float,
                false,
                stride,
                new IntPtr(3 * sizeof(float))
            );
            GL.EnableVertexAttribArray(1);

            GL.VertexAttribPointer(
                2,
                3,
                VertexAttribPointerType.Float,
                false,
                stride,
                new IntPtr(6 * sizeof(float))
            );
            GL.EnableVertexAttribArray(2);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
        catch
        {
            Deinitialize();
            throw;
        }
    }

    /// <summary>
    /// Converts a model into vertex data and uploads it to the GPU.
    /// </summary>
    /// <param name="model">The model to display.</param>
    public void UploadModel(Model model)
    {
        IReadOnlyList<Triangle> triangles = model.Mesh.Triangles;
        List<float> vertices = new List<float>();

        for (int i = 0; i < triangles.Count; i++)
        {
            Triangle triangle = triangles[i];

            Vector3 normal = Vector3.Cross(triangle.B - triangle.A, triangle.C - triangle.A);

            float normalLenght = normal.Length();

            if (!float.IsFinite(normalLenght) || normalLenght <= 0.0f)
            {
                continue;
            }

            normal /= normalLenght;

            int materialIndex = model.MaterialIndices[i];
            Vector3 color = model.Materials[materialIndex].DiffuseColor;

            AppendVertex(vertices, triangle.A, normal, color);
            AppendVertex(vertices, triangle.B, normal, color);
            AppendVertex(vertices, triangle.C, normal, color);
        }

        float[] vertexData = vertices.ToArray();

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertexData.Length * sizeof(float),
            vertexData,
            BufferUsageHint.StaticDraw
        );

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        _vertexCount = vertexData.Length / FloatsPerVertex;
    }

    /// <summary>
    /// Appends a vertex's position, normal, and color to the upload data.
    /// </summary>
    /// <param name="vertices">The destination vertex data.</param>
    /// <param name="position">The vertex position.</param>
    /// <param name="normal">The surface normal.</param>
    /// <param name="color">The diffuse material color.</param>
    private static void AppendVertex(
        List<float> vertices,
        Vector3 position,
        Vector3 normal,
        Vector3 color
    )
    {
        vertices.Add(position.X);
        vertices.Add(position.Y);
        vertices.Add(position.Z);

        vertices.Add(normal.X);
        vertices.Add(normal.Y);
        vertices.Add(normal.Z);

        vertices.Add(color.X);
        vertices.Add(color.Y);
        vertices.Add(color.Z);
    }

    /// <summary>
    /// Renders the model into the specified framebuffer.
    /// </summary>
    /// <param name="framebuffer">Avalonia's framebuffer.</param>
    /// <param name="width">Viewport width in physical pixels.</param>
    /// <param name="height">Viewport height in physical pixels.</param>
    public void Render(int framebuffer, int width, int height)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.Viewport(0, 0, width, height);

        GL.Disable(EnableCap.ScissorTest);
        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);

        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Less);

        GL.ClearColor(17f / 255f, 24f / 255f, 39f / 255f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (_vertexCount == 0)
        {
            GL.Disable(EnableCap.DepthTest);
            return;
        }

        Matrix4x4 view = Matrix4x4.CreateLookAt(
            new Vector3(3.0f, 2.0f, 4.0f),
            Vector3.Zero,
            Vector3.UnitY
        );

        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 3.0f,
            (float)width / height,
            0.1f,
            100.0f
        );

        // System.Numerics applies the view transform before the projection.
        Matrix4x4 viewProjection = view * projection;

        float[] matrixValues =
        {
            viewProjection.M11,
            viewProjection.M12,
            viewProjection.M13,
            viewProjection.M14,
            viewProjection.M21,
            viewProjection.M22,
            viewProjection.M23,
            viewProjection.M24,
            viewProjection.M31,
            viewProjection.M32,
            viewProjection.M33,
            viewProjection.M34,
            viewProjection.M41,
            viewProjection.M42,
            viewProjection.M43,
            viewProjection.M44,
        };

        GL.UseProgram(_shaderProgram);

        // OpenGL reads these values as the transpose needed by GLSL's
        // column-vector multiplication.
        GL.UniformMatrix4(_viewProjectionLocation, 1, false, matrixValues);
        GL.Uniform3(_lightPositionLocation, 4.0f, 5.0f, 3.0f);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);

        GL.BindVertexArray(0);
        GL.UseProgram(0);
        GL.Disable(EnableCap.DepthTest);
    }

    /// <summary>
    /// Releases the GPU resources created by this renderer.
    /// </summary>
    public void Deinitialize()
    {
        if (_vao != 0)
        {
            GL.DeleteVertexArray(_vao);
            _vao = 0;
        }

        if (_vbo != 0)
        {
            GL.DeleteBuffer(_vbo);
            _vbo = 0;
        }

        if (_shaderProgram != 0)
        {
            GL.DeleteProgram(_shaderProgram);
            _shaderProgram = 0;
        }

        _vertexCount = 0;
    }

    /// <summary>
    /// Loads, compiles, and links the preview shaders.
    /// </summary>
    /// <param name="isOpenGlEs">Whether the context uses OpenGL ES.</param>
    /// <returns>The linked shader program.</returns>
    private static int CreateShaderProgram(bool isOpenGlEs)
    {
        string version = isOpenGlEs
            ? "#version 300 es\nprecision highp float;\n"
            : "#version 150\n";

        string vertexSource = version + ReadShader("preview.vert");
        string fragmentSource = version + ReadShader("preview.frag");

        int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        int fragmentShader = 0;
        int program = 0;

        try
        {
            fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            program = GL.CreateProgram();

            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);

            GL.BindAttribLocation(program, 0, "position");
            GL.BindAttribLocation(program, 1, "normal");
            GL.BindAttribLocation(program, 2, "color");

            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);

            if (success == 0)
            {
                throw new InvalidOperationException(
                    "Shader linking failed: " + GL.GetProgramInfoLog(program)
                );
            }

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);

            return program;
        }
        catch
        {
            if (program != 0)
            {
                GL.DeleteProgram(program);
            }
            throw;
        }
        finally
        {
            GL.DeleteShader(vertexShader);
            if (fragmentShader != 0)
            {
                GL.DeleteShader(fragmentShader);
            }
        }
    }

    /// <summary>
    /// Reads an embedded preview shader.
    /// </summary>
    /// <param name="fileName">The shader file name.</param>
    /// <returns>The shader source code.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the shader resource cannot be found.
    /// </exception>
    private static string ReadShader(string fileName)
    {
        string resourceName = "Photon.App.Shaders." + fileName;

        using Stream? stream = typeof(TriangleRenderer).Assembly.GetManifestResourceStream(
            resourceName
        );

        if (stream == null)
        {
            throw new InvalidOperationException("Shader resource not found: " + resourceName);
        }

        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Compiles OpenGL shader
    /// </summary>
    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);

        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);

        if (success == 0)
        {
            string error = GL.GetShaderInfoLog(shader);

            GL.DeleteShader(shader);

            throw new InvalidOperationException($"Shader compilation failed: {error}");
        }

        return shader;
    }
}
