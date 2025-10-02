import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class FloatValueFunction implements FilterFunctionDef<readonly []> {
  name = 'FloatValue';
  label = 'Float Value (Gleitkommawert)';
  params: FilterFunctionParam[] = [
    {
      name: 'value',
      label: 'Value',
      type: 'number',
      default: 0.0,
    },
    {
      name: 'min',
      label: 'Min Value',
      type: 'number',
      default: -1.0,
    },
    {
      name: 'max',
      label: 'Max Value',
      type: 'number',
      default: 1.0,
    },
    {
      name: 'step',
      label: 'Step Size',
      type: 'number',
      default: 0.1,
    }
  ];
  inputLabels = [];
  outputLabels = ['value'];
  
  apply(inputs: InputMap<readonly []>, params: Record<string, any>, nodeId?: number) {
    const value = typeof params?.value === 'number' ? params.value : 0.0;
    const min = typeof params?.min === 'number' ? params.min : -1.0;
    const max = typeof params?.max === 'number' ? params.max : 1.0;
    
    // Clamp value to min/max range
    const clampedValue = Math.max(min, Math.min(max, value));
    
    return [clampedValue];
  }

  // Helper method to get current value
  getCurrentValue(params: Record<string, any>): number {
    const value = typeof params?.value === 'number' ? params.value : 0.0;
    const min = typeof params?.min === 'number' ? params.min : -1.0;
    const max = typeof params?.max === 'number' ? params.max : 1.0;
    
    return Math.max(min, Math.min(max, value));
  }

  // Helper method to set value programmatically
  setValue(params: Record<string, any>, newValue: number): Record<string, any> {
    const min = typeof params?.min === 'number' ? params.min : -1.0;
    const max = typeof params?.max === 'number' ? params.max : 1.0;
    const clampedValue = Math.max(min, Math.min(max, newValue));
    
    return { ...params, value: clampedValue };
  }
}
