// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: ChatGpt for summary
// -----------------------------------------------------------------------------
using System;
using Avalonia.OpenGL;
using OpenTK.Graphics.ES30;
using Photon.Core.Mathematics;

namespace Photon.App.Platform.OpenGL;

/// <summary>
/// Renders a static triangle using OpenGL.
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
    /// Initializes GPU resources for the triangle.
    /// </summary>
    /// <param name="triangle">The triangle to render.</param>
    /// <param name="isOpenGlEs">Whether the context uses OpenGL ES.</param>
    public void Initialize(Triangle triangle, bool isOpenGlEs)
    {
        float[] vertices =
        {
            triangle.A.X,
            triangle.A.Y,
            triangle.A.Z,
            triangle.B.X,
            triangle.B.Y,
            triangle.B.Z,
            triangle.C.X,
            triangle.C.Y,
            triangle.C.Z,
        };

        // Create and bind the vertex array.
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        // Create and fill the vertex buffer.
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.StaticDraw
        );

        // Each vertex contains three floats: X, Y and Z.
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        GL.EnableVertexAttribArray(0);

        // Compile shaders and create the shader program.
        _shaderProgram = CreateShaderProgram(isOpenGlEs);

        // Unbind OpenGL objects.
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Renders the triangle into the specified framebuffer.
    /// </summary>
    /// <param name="framebuffer">Avalonia's framebuffer.</param>
    /// <param name="width">Viewport width in physical pixels.</param>
    /// <param name="height">Viewport height in physical pixels.</param>
    public void Render(int framebuffer, int width, int height)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);

        GL.Viewport(0, 0, width, height);

        // Dark background: #111827.
        GL.ClearColor(17f / 255f, 24f / 255f, 39f / 255f, 1.0f);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        // Select our shader program.
        GL.UseProgram(_shaderProgram);

        // Select our triangle's vertex data.
        GL.BindVertexArray(_vao);

        // Draw one triangle consisting of three vertices.
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        // Reset bindings.
        GL.BindVertexArray(0);
        GL.UseProgram(0);
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
    }

    /// <summary>
    /// Compiles and links the vertex and fragment shaders.
    /// </summary>
    private static int CreateShaderProgram(bool isOpenGlEs)
    {
        string version = isOpenGlEs
            ? "#version 300 es\nprecision highp float;\n"
            : "#version 150\n";

        string vertexSource =
            version
            + """
                in vec3 position;

                void main()
                {
                    gl_Position = vec4(position, 1.0);
                }
                """;

        string fragmentSource =
            version
            + """
                out vec4 fragColor;

                void main()
                {
                    fragColor = vec4(0.749, 0.106, 0.663, 1.0);
                }
                """;

        int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);

        int fragmentShader;

        try
        {
            fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
        }
        catch
        {
            GL.DeleteShader(vertexShader);
            throw;
        }

        int program = GL.CreateProgram();

        try
        {
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);

            // Associate the shader input with vertex attribute 0.
            GL.BindAttribLocation(program, 0, "position");

            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);

            if (success == 0)
            {
                throw new InvalidOperationException(
                    "Shader linking failed: " + GL.GetProgramInfoLog(program)
                );
            }

            return program;
        }
        catch
        {
            GL.DeleteProgram(program);
            throw;
        }
        finally
        {
            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }
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
