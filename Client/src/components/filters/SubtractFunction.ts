import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class SubtractFunction implements FilterFunctionDef<readonly ["a", "b"]> {
  name = 'Subtract';
  label = 'Subtract (Subtraktion)';
  params: FilterFunctionParam[] = [
    { name: 'a', label: 'a (default)', type: 'number', default: 0 },
    { name: 'b', label: 'b (default)', type: 'number', default: 0 },
  ];
  inputLabels = ["a", "b"];
  outputLabels = ['difference'];
  apply(inputs: InputMap<readonly ["a", "b"]>, params: Record<string, any>) {
    const a = inputs.a ?? params.a ?? 0;
    const b = inputs.b ?? params.b ?? 0;
    return [a - b];
  }
}

