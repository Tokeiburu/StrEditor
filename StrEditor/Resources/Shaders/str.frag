#version 330

out vec4 outputColor;

in vec2 texCoord;
uniform vec4 color;
uniform sampler2D texture0;
uniform bool selection;
uniform float cutoff;

void main()
{
	if (selection) {
		outputColor = color;
	}
	else {
		outputColor = texture(texture0, texCoord);
		
		// STR images' have their quality lowered, so while the shader skips (0, 0, 0)
		// it's in fact more than that.
		// For PNG images, a value below or equal to 15 is skipped.
		// For BMP images, a value below or equal to 7 is skipped.
		// The downgrade is done on the CPU though, so it results in 0 here either way.
		if (outputColor.r == 0 && outputColor.g == 0 && outputColor.b == 0) {
			discard;
		}
		
		outputColor = outputColor * color;
	}
}