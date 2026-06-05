#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

out vec2 texCoord;

uniform vec2 uViewportSize;
uniform vec2 uTexSize;

uniform vec2 uRelativeCenter;
uniform float uZoom;

void main(void)
{
	vec2 screenPos = vec2(
		(aPosition.x * 0.5 + 0.5) * uViewportSize.x,
		(1.0 - (aPosition.y * 0.5 + 0.5)) * uViewportSize.y
	);
	
	vec2 centeredPos = screenPos - uViewportSize * 0.5;
	
	vec2 cameraOffset;

    cameraOffset.x =
        -(uRelativeCenter.x * uViewportSize.x
        - uViewportSize.x * 0.5);

    cameraOffset.y =
        (-uRelativeCenter.y * uViewportSize.y
        + uViewportSize.y * 0.5);
		
	vec2 worldPos =
        (centeredPos + cameraOffset)
        / uZoom;
	
	texCoord = (worldPos + (uTexSize * 0.5)) / uTexSize;
	
    gl_Position = vec4(aPosition, 1.0);
}