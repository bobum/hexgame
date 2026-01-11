import * as THREE from 'three';

/**
 * Custom terrain shader combining:
 * 1. Texture atlas sampling
 * 2. Triplanar projection for walls
 * 3. Procedural noise for detail variation
 * 4. Texture splatting for biome blending
 * 5. PBR-style lighting
 */

const vertexShader = /* glsl */ `
  attribute vec3 terrainColor;
  attribute float terrainType;

  varying vec3 vWorldPosition;
  varying vec3 vWorldNormal;
  varying vec3 vTerrainColor;
  varying float vTerrainType;

  void main() {
    vec4 worldPos = modelMatrix * vec4(position, 1.0);
    vWorldPosition = worldPos.xyz;
    vWorldNormal = normalize((modelMatrix * vec4(normal, 0.0)).xyz);
    vTerrainColor = terrainColor;
    vTerrainType = terrainType;

    gl_Position = projectionMatrix * viewMatrix * worldPos;
  }
`;

const fragmentShader = /* glsl */ `
  uniform vec3 uSunDirection;
  uniform vec3 uSunColor;
  uniform vec3 uAmbientColor;
  uniform float uTime;
  uniform sampler2D uNoiseTexture;
  uniform float uTextureScale;
  uniform float uTriplanarSharpness;
  uniform float uNoiseStrength;
  uniform float uRoughness;

  varying vec3 vWorldPosition;
  varying vec3 vWorldNormal;
  varying vec3 vTerrainColor;
  varying float vTerrainType;

  // ============================================
  // PROCEDURAL NOISE (technique 3)
  // ============================================

  // Simple hash function
  float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
  }

  // Value noise
  float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f); // Smoothstep

    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
  }

  // Fractal Brownian Motion - multiple octaves of noise
  float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < 4; i++) {
      value += amplitude * noise(p * frequency);
      amplitude *= 0.5;
      frequency *= 2.0;
    }
    return value;
  }

  // ============================================
  // TRIPLANAR MAPPING (technique 2)
  // ============================================

  vec3 triplanarWeights(vec3 normal) {
    vec3 w = abs(normal);
    w = pow(w, vec3(uTriplanarSharpness));
    return w / (w.x + w.y + w.z);
  }

  vec3 triplanarSample(vec3 worldPos, vec3 normal, vec3 baseColor) {
    vec3 weights = triplanarWeights(normal);

    // Sample noise from each axis projection
    float noiseXY = fbm(worldPos.xy * uTextureScale);
    float noiseXZ = fbm(worldPos.xz * uTextureScale);
    float noiseYZ = fbm(worldPos.yz * uTextureScale);

    // Blend based on normal direction
    float blendedNoise = noiseYZ * weights.x + noiseXZ * weights.y + noiseXY * weights.z;

    // Apply noise to vary the base color
    return baseColor * (1.0 + (blendedNoise - 0.5) * uNoiseStrength);
  }

  // ============================================
  // TEXTURE SPLATTING (technique 4)
  // Based on terrain type, could blend multiple textures
  // For now, uses procedural variation per terrain
  // ============================================

  vec3 getTerrainDetail(float terrainType, vec3 worldPos) {
    // Different noise patterns per terrain type
    float detail = fbm(worldPos.xz * uTextureScale * 2.0 + terrainType * 10.0);
    return vec3(detail);
  }

  // ============================================
  // PBR-STYLE LIGHTING (technique 5)
  // Simplified PBR with diffuse + specular
  // ============================================

  vec3 pbrLighting(vec3 albedo, vec3 normal, vec3 viewDir) {
    // Diffuse (Lambert)
    float NdotL = max(dot(normal, uSunDirection), 0.0);
    vec3 diffuse = albedo * uSunColor * NdotL;

    // Ambient
    vec3 ambient = albedo * uAmbientColor;

    // Specular (Blinn-Phong approximation)
    vec3 halfDir = normalize(uSunDirection + viewDir);
    float NdotH = max(dot(normal, halfDir), 0.0);
    float specPower = mix(8.0, 64.0, 1.0 - uRoughness);
    float spec = pow(NdotH, specPower) * (1.0 - uRoughness) * 0.3;
    vec3 specular = uSunColor * spec;

    // Fresnel-like rim lighting
    float fresnel = pow(1.0 - max(dot(normal, viewDir), 0.0), 3.0) * 0.15;

    return ambient + diffuse + specular + fresnel * uSunColor;
  }

  void main() {
    vec3 normal = normalize(vWorldNormal);
    vec3 baseColor = vTerrainColor;

    // Apply noise based on surface orientation
    vec3 texturedColor;

    if (abs(normal.y) > 0.7) {
      // Hex tops - use top-down projection
      float noiseVal = fbm(vWorldPosition.xz * uTextureScale);
      texturedColor = baseColor * (1.0 + (noiseVal - 0.5) * uNoiseStrength);
    } else {
      // Walls/cliffs - use triplanar projection
      texturedColor = triplanarSample(vWorldPosition, normal, baseColor);
    }

    // PBR-style lighting
    vec3 viewDir = normalize(cameraPosition - vWorldPosition);
    vec3 finalColor = pbrLighting(texturedColor, normal, viewDir);

    // Tone mapping (Reinhard) and gamma correction
    finalColor = finalColor / (finalColor + vec3(1.0));
    finalColor = pow(finalColor, vec3(1.0 / 2.2));

    gl_FragColor = vec4(finalColor, 1.0);
  }
`;

export interface TerrainShaderUniforms {
  uSunDirection: THREE.Vector3;
  uSunColor: THREE.Color;
  uAmbientColor: THREE.Color;
  uTime: number;
  uTextureScale: number;
  uTriplanarSharpness: number;
  uNoiseStrength: number;
  uRoughness: number;
}

const defaultUniforms: TerrainShaderUniforms = {
  uSunDirection: new THREE.Vector3(0.5, 0.8, 0.3).normalize(),
  uSunColor: new THREE.Color(1.0, 0.95, 0.9),
  uAmbientColor: new THREE.Color(0.15, 0.18, 0.22),  // Reduced ambient for more contrast
  uTime: 0,
  uTextureScale: 3.0,        // Higher = smaller patterns, visible within each hex
  uTriplanarSharpness: 4.0,
  uNoiseStrength: 0.4,       // Subtle but visible
  uRoughness: 0.7,
};

export function createTerrainMaterial(options: Partial<TerrainShaderUniforms> = {}): THREE.ShaderMaterial {
  const uniforms = { ...defaultUniforms, ...options };

  return new THREE.ShaderMaterial({
    vertexShader,
    fragmentShader,
    uniforms: {
      uSunDirection: { value: uniforms.uSunDirection },
      uSunColor: { value: uniforms.uSunColor },
      uAmbientColor: { value: uniforms.uAmbientColor },
      uTime: { value: uniforms.uTime },
      uTextureScale: { value: uniforms.uTextureScale },
      uTriplanarSharpness: { value: uniforms.uTriplanarSharpness },
      uNoiseStrength: { value: uniforms.uNoiseStrength },
      uRoughness: { value: uniforms.uRoughness },
    },
  });
}

/**
 * Update time uniform for animated effects
 */
export function updateTerrainMaterial(material: THREE.ShaderMaterial, deltaTime: number): void {
  if (material.uniforms.uTime) {
    material.uniforms.uTime.value += deltaTime;
  }
}
