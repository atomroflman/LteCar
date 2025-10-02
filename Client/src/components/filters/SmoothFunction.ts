import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class SmoothFunction implements FilterFunctionDef<readonly ["input"]> {
  name = 'Smooth';
  label = 'Smooth (Sanfte Annäherung)';
  params: FilterFunctionParam[] = [
    {
      name: 'speed',
      label: 'Speed (0.01-1.0)',
      type: 'number',
      default: 0.1,
    },
    {
      name: 'threshold',
      label: 'Threshold',
      type: 'number',
      default: 0.001,
    },
    {
      name: 'fps',
      label: 'FPS (10-120)',
      type: 'number',
      default: 60,
    }
  ];
  inputLabels = ["input"];
  outputLabels = ['output'];
  
  // Store current values for each node
  private static currentValues: Map<number, number> = new Map();
  private static intervals: Map<number, NodeJS.Timeout> = new Map();
  private static flowControl: any = null;
  
  // Static method to set the flow control reference
  static setFlowControl(flowControl: any) {
    SmoothFunction.flowControl = flowControl;
  }
  
  apply(inputs: InputMap<readonly ["input"]>, params: Record<string, any>, nodeId?: number) {
    const targetValue = inputs.input ?? 0;
    const speed = Math.max(0.01, Math.min(1.0, params?.speed ?? 0.1));
    const threshold = Math.max(0.0001, params?.threshold ?? 0.001);
    const fps = Math.max(10, Math.min(120, params?.fps ?? 60));
    const intervalMs = 1000 / fps;
    
    if (!nodeId) return [targetValue];
    
    // Get or initialize current value
    if (!SmoothFunction.currentValues.has(nodeId)) {
      SmoothFunction.currentValues.set(nodeId, targetValue);
      return [targetValue];
    }
    
    const currentValue = SmoothFunction.currentValues.get(nodeId)!;
    const difference = targetValue - currentValue;
    
    // If we're close enough, just return the target
    if (Math.abs(difference) <= threshold) {
      SmoothFunction.currentValues.set(nodeId, targetValue);
      this.clearInterval(nodeId);
      return [targetValue];
    }
    
    // Clear existing interval
    this.clearInterval(nodeId);
    
    // Start smooth transition
    if (SmoothFunction.flowControl) {
      const intervalId = setInterval(() => {
        const current = SmoothFunction.currentValues.get(nodeId!) ?? currentValue;
        const diff = targetValue - current;
        
        if (Math.abs(diff) <= threshold) {
          // We've reached the target
          SmoothFunction.currentValues.set(nodeId!, targetValue);
          SmoothFunction.flowControl.recalculateNode(nodeId!);
          this.clearInterval(nodeId!);
        } else {
          // Move closer to target
          const newValue = current + (diff * speed);
          SmoothFunction.currentValues.set(nodeId!, newValue);
          SmoothFunction.flowControl.recalculateNode(nodeId!);
        }
      }, intervalMs);
      
      SmoothFunction.intervals.set(nodeId, intervalId);
    }
    
    return [currentValue];
  }
  
  private clearInterval(nodeId: number) {
    if (SmoothFunction.intervals.has(nodeId)) {
      clearInterval(SmoothFunction.intervals.get(nodeId)!);
      SmoothFunction.intervals.delete(nodeId);
    }
  }
  
  // Cleanup method to clear all intervals
  static cleanup() {
    SmoothFunction.intervals.forEach(intervalId => clearInterval(intervalId));
    SmoothFunction.intervals.clear();
    SmoothFunction.currentValues.clear();
  }
  
  // Method to reset a specific node
  static resetNode(nodeId: number) {
    if (SmoothFunction.intervals.has(nodeId)) {
      clearInterval(SmoothFunction.intervals.get(nodeId)!);
      SmoothFunction.intervals.delete(nodeId);
    }
    SmoothFunction.currentValues.delete(nodeId);
  }
}
