import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class PowerFunction implements FilterFunctionDef<readonly ["base", "exponent"]> {
  name = 'Power';
  label = 'Power (Potenz)';
  params: FilterFunctionParam[] = [];
  inputLabels = ["base", "exponent"] as const;
  outputLabels = ['result'];
  apply(inputs: InputMap<readonly ["base", "exponent"]>) {
    const base = inputs.base ?? 0;
    const exponent = inputs.exponent ?? 0;
    return [Math.pow(base, exponent)];
  }
}
