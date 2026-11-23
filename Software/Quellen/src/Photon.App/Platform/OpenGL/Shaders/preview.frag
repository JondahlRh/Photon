// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: https://learnopengl.com/Lighting/Basic-Lighting; ChatGPT Codex for configurable point-light intensity
// -----------------------------------------------------------------------------

in vec3 worldPosition;
in vec3 worldNormal;
in vec3 baseColor;

uniform vec3 lightPosition;
uniform vec3 lightIntensity;

out vec4 fragColor;

void main()
{
    vec3 normal = normalize(worldNormal);
    vec3 toLight = lightPosition - worldPosition;
    float distanceSquared = dot(toLight, toLight);

    vec3 directLight = vec3(0.0);

    if (distanceSquared > 1e-10)
    {
        vec3 lightDirection = toLight * inversesqrt(distanceSquared);
        float diffuse = max(dot(normal, lightDirection), 0.0);

        directLight = lightIntensity * diffuse;
    }

    vec3 ambientLight = vec3(0.15);
    vec3 color = baseColor * (ambientLight + directLight);

    fragColor = vec4(color, 1.0);
}
