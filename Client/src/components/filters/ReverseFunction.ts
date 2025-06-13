import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class ReverseFunction implements FilterFunctionDef<readonly ["input"]> {
  name = 'Reverse';
  label = 'Reverse';
  params: FilterFunctionParam[] = [];
  inputLabels = ["input"] as const;
  outputLabels = ['output'];
  apply(inputs: InputMap<readonly ["input"]>) {
    return [-inputs.input];
  }
}
