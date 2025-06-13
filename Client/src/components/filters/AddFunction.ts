import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class AddFunction implements FilterFunctionDef<readonly ["a", "b"]> {
  name = 'Add';
  label = 'Add (Addition)';
  params: FilterFunctionParam[] = [];
  inputLabels = ["a", "b"] as const;
  outputLabels = ['sum'];
  apply(inputs: InputMap<readonly ["a", "b"]>) {
    return [inputs.a + inputs.b];
  }
}
