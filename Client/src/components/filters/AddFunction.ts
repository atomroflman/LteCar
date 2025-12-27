import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class AddFunction implements FilterFunctionDef<readonly ["a", "b"]> {
  name = 'Add';
  label = 'Add (Addition)';
  params: FilterFunctionParam[] = [
    { name: 'a', label: 'a (default)', type: 'number', default: 0 },
    { name: 'b', label: 'b (default)', type: 'number', default: 0 },
  ];
  inputLabels = ["a", "b"];
  outputLabels = ['sum'];
  apply(inputs: InputMap<readonly ["a", "b"]>, params: Record<string, any>) {
    const a = inputs.a ?? params.a ?? 0;
    const b = inputs.b ?? params.b ?? 0;
    return [a + b];
  }
}
