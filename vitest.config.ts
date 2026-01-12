import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    // Use jsdom for any DOM-related tests
    environment: 'node',

    // Include test files
    include: ['src/**/*.test.ts', 'tests/**/*.test.ts'],

    // Coverage configuration
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: [
        'node_modules/',
        'src/main.ts',
        'src/**/*.d.ts',
        // Exclude renderers (Three.js dependent)
        'src/rendering/**',
        'src/camera/**',
      ],
    },

    // Timeouts
    testTimeout: 10000,
    hookTimeout: 10000,
  },
});
