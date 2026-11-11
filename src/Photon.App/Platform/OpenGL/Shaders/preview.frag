// -----------------------------------------------------------------------------
// Author:        BrackyDayn
// AI Assistance: https://learnopengl.com/Lighting/Basic-Lighting
// -----------------------------------------------------------------------------

in vec3 worldPosition;
in vec3 worldNormal;
in vec3 baseColor;

uniform vec3 lightPosition;

out vec4 fragColor;

void main(){
    vec3 normal = normalize(worldNormal);
    vec3 lightDirection = normalize(lightPosition - worldPosition);

    //lambert diffuse
    float diffuse =  max(dot(normal, lightDirection), 0.0);

    float brightness = 0.15 + 0.85 * diffuse;

    fragColor = vec4(baseColor *  brightness, 1.0);
}