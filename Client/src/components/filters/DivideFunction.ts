import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class DivideFunction implements FilterFunctionDef<readonly ["a", "b"]> {
  name = 'Divide';
  label = 'Divide (Division)';
  params: FilterFunctionParam[] = [
    { name: 'a', label: 'a (default)', type: 'number', default: 0 },
    { name: 'b', label: 'b (default)', type: 'number', default: 1 },
  ];
  inputLabels = ["a", "b"] as const;
  outputLabels = ['quotient'];
  apply(inputs: InputMap<readonly ["a", "b"]>, params: Record<string, any>) {
    const a = inputs.a ?? params.a ?? 0;
    const b = inputs.b ?? params.b ?? 1;
    if (b === 0) return [0]; // Avoid division by zero
    return [a / b];
  }
}
