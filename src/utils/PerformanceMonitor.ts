/**
 * Performance monitoring utility with frame time tracking and visual graph.
 */
export class PerformanceMonitor {
  // Frame time history
  private frameTimes: number[] = [];
  private readonly maxSamples = 120;

  // Timing markers for breakdown
  private markers: Map<string, number> = new Map();
  private timings: Map<string, number> = new Map();

  // Statistics
  private _fps = 0;
  private _avgFrameTime = 0;
  private _minFrameTime = Infinity;
  private _maxFrameTime = 0;
  private _onePercentLow = 0;

  // Generation timing
  private _lastGenerationTime = 0;

  // Canvas for graph
  private canvas: HTMLCanvasElement | null = null;
  private ctx: CanvasRenderingContext2D | null = null;
  private graphVisible = true;

  // Graph settings
  private readonly graphWidth = 200;
  private readonly graphHeight = 60;
  private readonly targetFrameTime = 16.67; // 60fps target
  private readonly maxGraphFrameTime = 50; // max ms shown on graph

  constructor() {
    this.createGraph();
  }

  /**
   * Create the visual graph canvas.
   */
  private createGraph(): void {
    this.canvas = document.createElement('canvas');
    this.canvas.width = this.graphWidth;
    this.canvas.height = this.graphHeight;
    this.canvas.style.cssText = `
      position: fixed;
      bottom: 10px;
      left: 10px;
      background: rgba(0, 0, 0, 0.7);
      border: 1px solid #444;
      border-radius: 4px;
      z-index: 1000;
      image-rendering: pixelated;
    `;
    document.body.appendChild(this.canvas);
    this.ctx = this.canvas.getContext('2d');
  }

  /**
   * Toggle graph visibility.
   */
  toggleGraph(): void {
    this.graphVisible = !this.graphVisible;
    if (this.canvas) {
      this.canvas.style.display = this.graphVisible ? 'block' : 'none';
    }
  }

  /**
   * Start timing a section.
   */
  beginSection(name: string): void {
    this.markers.set(name, performance.now());
  }

  /**
   * End timing a section.
   */
  endSection(name: string): void {
    const start = this.markers.get(name);
    if (start !== undefined) {
      this.timings.set(name, performance.now() - start);
    }
  }

  /**
   * Get timing for a section.
   */
  getSectionTime(name: string): number {
    return this.timings.get(name) ?? 0;
  }

  /**
   * Record a frame time and update statistics.
   */
  recordFrame(deltaTime: number): void {
    const frameTimeMs = deltaTime * 1000;

    this.frameTimes.push(frameTimeMs);
    if (this.frameTimes.length > this.maxSamples) {
      this.frameTimes.shift();
    }

    // Update stats
    this._fps = Math.round(1000 / frameTimeMs);
    this.updateStatistics();

    // Draw graph
    this.drawGraph();
  }

  /**
   * Record map generation time.
   */
  recordGenerationTime(timeMs: number): void {
    this._lastGenerationTime = timeMs;
  }

  /**
   * Update rolling statistics.
   */
  private updateStatistics(): void {
    if (this.frameTimes.length === 0) return;

    // Average
    const sum = this.frameTimes.reduce((a, b) => a + b, 0);
    this._avgFrameTime = sum / this.frameTimes.length;

    // Min/Max (recent frames only)
    this._minFrameTime = Math.min(...this.frameTimes);
    this._maxFrameTime = Math.max(...this.frameTimes);

    // 1% low (worst 1% of frames)
    const sorted = [...this.frameTimes].sort((a, b) => b - a);
    const worstCount = Math.max(1, Math.floor(sorted.length * 0.01));
    const worstFrames = sorted.slice(0, worstCount);
    this._onePercentLow = Math.round(1000 / (worstFrames.reduce((a, b) => a + b, 0) / worstCount));
  }

  /**
   * Draw the frame time graph.
   */
  private drawGraph(): void {
    if (!this.ctx || !this.canvas || !this.graphVisible) return;

    const ctx = this.ctx;
    const w = this.graphWidth;
    const h = this.graphHeight;

    // Clear
    ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
    ctx.fillRect(0, 0, w, h);

    // Draw target line (16.67ms = 60fps)
    const targetY = h - (this.targetFrameTime / this.maxGraphFrameTime) * h;
    ctx.strokeStyle = '#0a0';
    ctx.lineWidth = 1;
    ctx.setLineDash([2, 2]);
    ctx.beginPath();
    ctx.moveTo(0, targetY);
    ctx.lineTo(w, targetY);
    ctx.stroke();
    ctx.setLineDash([]);

    // Draw 33ms line (30fps)
    const thirtyFpsY = h - (33.33 / this.maxGraphFrameTime) * h;
    ctx.strokeStyle = '#aa0';
    ctx.beginPath();
    ctx.moveTo(0, thirtyFpsY);
    ctx.lineTo(w, thirtyFpsY);
    ctx.stroke();

    // Draw frame times
    if (this.frameTimes.length < 2) return;

    const barWidth = w / this.maxSamples;

    for (let i = 0; i < this.frameTimes.length; i++) {
      const ft = this.frameTimes[i];
      const barHeight = Math.min((ft / this.maxGraphFrameTime) * h, h);
      const x = i * barWidth;
      const y = h - barHeight;

      // Color based on frame time
      if (ft <= this.targetFrameTime) {
        ctx.fillStyle = '#0f0'; // Good (60+ fps)
      } else if (ft <= 33.33) {
        ctx.fillStyle = '#ff0'; // OK (30-60 fps)
      } else {
        ctx.fillStyle = '#f00'; // Bad (<30 fps)
      }

      ctx.fillRect(x, y, barWidth - 1, barHeight);
    }

    // Draw text overlay
    ctx.fillStyle = '#fff';
    ctx.font = '10px monospace';
    ctx.fillText(`FPS: ${this._fps} | Avg: ${this._avgFrameTime.toFixed(1)}ms`, 4, 12);
    ctx.fillText(`1% Low: ${this._onePercentLow} | Max: ${this._maxFrameTime.toFixed(1)}ms`, 4, 24);
  }

  /**
   * Reset all statistics.
   */
  reset(): void {
    this.frameTimes = [];
    this._minFrameTime = Infinity;
    this._maxFrameTime = 0;
    this.timings.clear();
  }

  /**
   * Get current stats object for debug UI.
   */
  get stats() {
    return {
      fps: this._fps,
      avgFrameTime: this._avgFrameTime,
      minFrameTime: this._minFrameTime,
      maxFrameTime: this._maxFrameTime,
      onePercentLow: this._onePercentLow,
      lastGenerationTime: this._lastGenerationTime,
    };
  }

  // Getters for individual stats
  get fps(): number { return this._fps; }
  get avgFrameTime(): number { return this._avgFrameTime; }
  get minFrameTime(): number { return this._minFrameTime; }
  get maxFrameTime(): number { return this._maxFrameTime; }
  get onePercentLow(): number { return this._onePercentLow; }
  get lastGenerationTime(): number { return this._lastGenerationTime; }

  /**
   * Clean up resources.
   */
  dispose(): void {
    if (this.canvas && this.canvas.parentNode) {
      this.canvas.parentNode.removeChild(this.canvas);
    }
  }
}
