#version 330

out vec4 outputColor;

in vec2 texCoord;
uniform vec4 color;
uniform sampler2D texture0;
uniform bool selection;

void main()
{
	if (selection) {
		outputColor = color;
	}
	else {
		outputColor = texture(texture0, texCoord);
		
		if (outputColor.r < 0.1 && outputColor.g < 0.1 && outputColor.b < 0.1) {
			discard;
		}
		
		outputColor = outputColor * color;
	}
}