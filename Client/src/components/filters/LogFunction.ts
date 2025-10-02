import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class LogFunction implements FilterFunctionDef<readonly ["value", "base"]> {
  name = 'Log';
  label = 'Log (Logarithmus)';
  params: FilterFunctionParam[] = [];
  inputLabels = ["value", "base"] as const;
  outputLabels = ['result'];
  apply(inputs: InputMap<readonly ["value", "base"]>) {
    const value = inputs.value ?? 0;
    const base = inputs.base ?? Math.E; // Default to natural log
    if (value <= 0 || base <= 0 || base === 1) return [0]; // Avoid invalid log operations
    return [Math.log(value) / Math.log(base)];
  }
}
