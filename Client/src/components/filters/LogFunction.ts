import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class LogFunction implements FilterFunctionDef<readonly ["value", "base"]> {
  name = 'Log';
  label = 'Log (Logarithmus)';
  params: FilterFunctionParam[] = [
    { name: 'value', label: 'value (default)', type: 'number', default: 1 },
    { name: 'base', label: 'base (default)', type: 'number', default: Math.E },
  ];
  inputLabels = ["value", "base"] as const;
  outputLabels = ['result'];
  apply(inputs: InputMap<readonly ["value", "base"]>, params: Record<string, any>) {
    const value = inputs.value ?? params.value ?? 1;
    const base = inputs.base ?? params.base ?? Math.E; // Default to natural log
    if (value <= 0 || base <= 0 || base === 1) return [0]; // Avoid invalid log operations
    return [Math.log(value) / Math.log(base)];
  }
}
