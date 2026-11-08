/*
 * Vertex shader for the model preview.
 *
 * Transforms vertices into clip space using the camera's view-projection
 * matrix. Passes world-space positions, normals, and material colors to
 * the fragment shader for lighting.
 *
 * Converts the System.Numerics projection depth convention to OpenGL's.
 */
in  vec3 position;
in vec3 normal;
in vec3 color;

uniform mat4 viewProjection;

out vec3 worldPosition;
out vec3 worldNormal;
out vec3 baseColor;

void main(){
    worldPosition = position;
    worldNormal = normal;
    baseColor = color;

    vec4 clipPosition = viewProjection * vec4(position, 1.0);

    clipPosition.z = 2.0 * clipPosition.z -clipPosition.w;

    gl_Position = clipPosition;
}
