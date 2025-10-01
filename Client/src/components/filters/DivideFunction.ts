import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class DivideFunction implements FilterFunctionDef<readonly ["a", "b"]> {
  name = 'Divide';
  label = 'Divide (Division)';
  params: FilterFunctionParam[] = [];
  inputLabels = ["a", "b"] as const;
  outputLabels = ['quotient'];
  apply(inputs: InputMap<readonly ["a", "b"]>) {
    const b = inputs.b ?? 0;
    if (b === 0) return [0]; // Avoid division by zero
    return [inputs.a / b];
  }
}
