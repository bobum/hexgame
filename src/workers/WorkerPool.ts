/**
 * Generic worker pool for parallel task execution.
 * Manages a pool of web workers and distributes tasks across them.
 */

export interface WorkerTask<T = unknown> {
  id: number;
  type: string;
  data: T;
}

export interface WorkerResult<T = unknown> {
  id: number;
  type: string;
  data: T;
  error?: string;
}

type TaskResolver<T> = {
  resolve: (value: T) => void;
  reject: (reason: Error) => void;
};

export class WorkerPool {
  private workers: Worker[] = [];
  private taskQueue: Array<{ task: WorkerTask; resolver: TaskResolver<unknown> }> = [];
  private pendingTasks: Map<number, TaskResolver<unknown>> = new Map();
  private nextTaskId = 0;
  private workerBusy: boolean[] = [];

  // Statistics
  private _tasksCompleted = 0;
  private _tasksFailed = 0;

  /**
   * Create a worker pool.
   * @param createWorker - Factory function to create workers
   * @param poolSize - Number of workers (defaults to navigator.hardwareConcurrency - 1)
   */
  constructor(
    private createWorker: () => Worker,
    poolSize?: number
  ) {
    const size = poolSize ?? Math.max(1, (navigator.hardwareConcurrency || 4) - 1);

    for (let i = 0; i < size; i++) {
      this.addWorker();
    }
  }

  private addWorker(): void {
    const worker = this.createWorker();
    const workerIndex = this.workers.length;

    worker.onmessage = (e: MessageEvent<WorkerResult>) => {
      this.handleWorkerResult(workerIndex, e.data);
    };

    worker.onerror = (e: ErrorEvent) => {
      console.error('Worker error:', e.message);
      this.workerBusy[workerIndex] = false;
      this.processQueue();
    };

    this.workers.push(worker);
    this.workerBusy.push(false);
  }

  private handleWorkerResult(workerIndex: number, result: WorkerResult): void {
    const resolver = this.pendingTasks.get(result.id);

    if (resolver) {
      this.pendingTasks.delete(result.id);

      if (result.error) {
        resolver.reject(new Error(result.error));
        this._tasksFailed++;
      } else {
        resolver.resolve(result.data);
        this._tasksCompleted++;
      }
    }

    this.workerBusy[workerIndex] = false;
    this.processQueue();
  }

  private processQueue(): void {
    if (this.taskQueue.length === 0) return;

    // Find an idle worker
    const idleIndex = this.workerBusy.findIndex(busy => !busy);
    if (idleIndex === -1) return;

    const { task, resolver } = this.taskQueue.shift()!;
    this.workerBusy[idleIndex] = true;
    this.pendingTasks.set(task.id, resolver);
    this.workers[idleIndex].postMessage(task);
  }

  /**
   * Run a task on an available worker.
   * Returns a promise that resolves with the result.
   */
  runTask<TInput, TOutput>(type: string, data: TInput): Promise<TOutput> {
    return new Promise((resolve, reject) => {
      const task: WorkerTask<TInput> = {
        id: this.nextTaskId++,
        type,
        data,
      };

      const resolver: TaskResolver<unknown> = {
        resolve: resolve as (value: unknown) => void,
        reject,
      };

      // Find an idle worker
      const idleIndex = this.workerBusy.findIndex(busy => !busy);

      if (idleIndex !== -1) {
        // Send immediately
        this.workerBusy[idleIndex] = true;
        this.pendingTasks.set(task.id, resolver);
        this.workers[idleIndex].postMessage(task);
      } else {
        // Queue for later
        this.taskQueue.push({ task: task as WorkerTask, resolver });
      }
    });
  }

  /**
   * Check if workers are available (not all busy).
   */
  hasAvailableWorker(): boolean {
    return this.workerBusy.some(busy => !busy);
  }

  /**
   * Get the number of workers in the pool.
   */
  get size(): number {
    return this.workers.length;
  }

  /**
   * Get statistics.
   */
  get stats() {
    return {
      workers: this.workers.length,
      busy: this.workerBusy.filter(b => b).length,
      queued: this.taskQueue.length,
      pending: this.pendingTasks.size,
      completed: this._tasksCompleted,
      failed: this._tasksFailed,
    };
  }

  /**
   * Terminate all workers.
   */
  terminate(): void {
    for (const worker of this.workers) {
      worker.terminate();
    }
    this.workers = [];
    this.workerBusy = [];
    this.taskQueue = [];

    // Reject any pending tasks
    for (const [, resolver] of this.pendingTasks) {
      resolver.reject(new Error('Worker pool terminated'));
    }
    this.pendingTasks.clear();
  }
}
