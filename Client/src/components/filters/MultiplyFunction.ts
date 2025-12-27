import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class MultiplyFunction implements FilterFunctionDef<readonly ["a", "b"]> {
  name = 'Multiply';
  label = 'Multiply (a × b)';
  params: FilterFunctionParam[] = [
    { name: 'a', label: 'a (default)', type: 'number', default: 1 },
    { name: 'b', label: 'b (default)', type: 'number', default: 1 },
  ];
  inputLabels = ["a", "b"];
  outputLabels = ['result'];

  apply(inputs: InputMap<readonly ["a", "b"]>, params: Record<string, any>) {
    const a = inputs.a ?? params.a ?? 1;
    const b = inputs.b ?? params.b ?? 1;
    return [a * b];
  }
}

