import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class LogFunction implements FilterFunctionDef<readonly ["value"]> {
  name = 'Log';
  label = 'Log (Logarithmus)';
  params: FilterFunctionParam[] = [];
  inputLabels = ["value"] as const;
  outputLabels = ['result'];
  apply(inputs: InputMap<readonly ["value"]>) {
    const value = inputs.value ?? 0;
    if (value <= 0) return [0]; // Avoid log of zero or negative numbers
    return [Math.log(value)];
  }
}
