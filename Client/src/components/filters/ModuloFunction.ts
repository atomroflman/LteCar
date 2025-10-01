import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class ModuloFunction implements FilterFunctionDef<readonly ["a", "b"]> {
  name = 'Modulo';
  label = 'Modulo (Rest)';
  params: FilterFunctionParam[] = [];
  inputLabels = ["a", "b"] as const;
  outputLabels = ['remainder'];
  apply(inputs: InputMap<readonly ["a", "b"]>) {
    const b = inputs.b ?? 0;
    if (b === 0) return [0]; // Avoid division by zero
    return [inputs.a % b];
  }
}
