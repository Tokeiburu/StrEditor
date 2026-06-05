#version 330 core

layout(location = 0) in vec3 aPosition;

out vec2 texCoord;

uniform mat4 m;
uniform mat4 vp;

void main(void)
{
    gl_Position = vec4(aPosition, 1.0) * m * vp;
}