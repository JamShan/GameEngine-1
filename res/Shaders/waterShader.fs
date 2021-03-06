#version 420

in vec2 texCoords;
in vec4 clipSpace;
in vec3 worldPos0;
out vec4 color;
uniform sampler2D reflectionMap;
uniform sampler2D refractionMap;
uniform sampler2D dudvMap;
uniform sampler2D normalMap;
uniform sampler2D depthMap;
uniform vec3 lightColor;
uniform vec3 eyePos;
uniform vec3 lightPos;
uniform vec4 fogColor;
uniform float moveFactor;

const float waveStrength = 0.03f;
const float specularPower = 100.0f;
const float specularIntensity = 0.6f;
const float density = 0.01f;
const float gradient = 1.5f;

vec4 calcLightSpec(vec3 color , vec3 directionToCamera , vec3 lightDir , vec3 normal)
{
	vec3 directionToEye = normalize(directionToCamera);
	vec3 reflectDirection = normalize(reflect(lightDir , normal));
	vec4 specularColor = vec4(0,0,0,0);

	float specularFactor = dot(directionToEye , reflectDirection);
	if(specularFactor > 0 )
	{
		specularFactor = pow(specularFactor , specularPower);
		specularColor = vec4(color , 1.0)*specularFactor*specularIntensity;
	}
	return specularColor;
}
float calcVisibility(float d)
{
	float v =  exp(-pow((d*density) , gradient));
	return clamp(v , 0.0f , 1.0f);
}
void main()
{
	vec2 ndc  = (clipSpace.xy/clipSpace.w)/2.0 + 0.5;
	vec2 reflectCoords = vec2(ndc.x , 1-ndc.y);
	vec2 refractCoords = vec2(ndc.x , ndc.y);

	float near = 0.1f ;
	float far = 200.0f;
	float d = texture2D(depthMap , refractCoords).r;
	float floorDistance = 2.0*near*far/(far+near -(2.0 * d-1.0)*(far-near));

	d = gl_FragCoord.z;
	float waterDistance = 2.0*near*far/(far+near -(2.0 * d-1.0)*(far-near));

	float waterDepth =  floorDistance - waterDistance;
	vec2 distortedTexCoords = (texture2D(dudvMap , vec2(texCoords.x + moveFactor , texCoords.y)).rg*2.0 - 1.0)* 														waveStrength; 
	distortedTexCoords = texCoords + vec2(distortedTexCoords.x , distortedTexCoords.y + moveFactor);
	vec2 totalDistortion = (texture2D(dudvMap , distortedTexCoords).rg*2 -1 )* waveStrength*clamp(waterDepth/25.0,0.0f ,1.0f);
	refractCoords += totalDistortion;
	refractCoords = clamp(refractCoords , 0.001 , 0.999);

	reflectCoords += totalDistortion;
	reflectCoords = clamp(reflectCoords , 0.001 , 0.999);

	vec4 normalColor = texture2D(normalMap , distortedTexCoords + totalDistortion);
	vec3 normal = vec3(normalColor.r*2.0 -1.0 , normalColor.b*7 , normalColor.g*2.0 - 1.0);
	normal = normalize(normal);
	vec4 tspec = calcLightSpec( lightColor , (eyePos-worldPos0) , worldPos0 - lightPos , normal);
	
	vec4 reflectionColor = texture2D(reflectionMap , reflectCoords) ;
	vec4 refractionColor = texture2D(refractionMap , refractCoords);

	float fresnelFactor = dot( normal , normalize(eyePos-worldPos0));
	fresnelFactor = pow(fresnelFactor , 3.0);
	color = mix(reflectionColor , refractionColor , fresnelFactor) + tspec;
	//color = mix(fogColor , color , calcVisibility(length(worldPos0-eyePos)));
	color.a = clamp(waterDepth/1.0 , 0.0f ,1.0f);
	
}
