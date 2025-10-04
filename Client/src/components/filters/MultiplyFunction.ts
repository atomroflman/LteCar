import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class MultiplyFunction implements FilterFunctionDef<readonly ["a", "b"]> {
  name = 'Multiply';
  label = 'Multiply (a × b)';
  params: FilterFunctionParam[] = [];
  inputLabels = ["a", "b"];
  outputLabels = ['result'];

  apply(inputs: InputMap<readonly ["a", "b"]>) {
    const a = inputs.a ?? 0;
    const b = inputs.b ?? 0;
    return [a * b];
  }
}

