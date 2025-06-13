import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class ScaleFunction implements FilterFunctionDef<readonly ["input"]> {
  name = 'Scale';
  label = 'Scale';
  params: FilterFunctionParam[] = [
    { name: 'factor', label: 'Faktor', type: 'number', default: 1 },
  ];
  inputLabels = ["input"] as const;
  outputLabels = ['output'];
  apply(inputs: InputMap<readonly ["input"]>, params: Record<string, any>) {
    const factor = params?.factor ?? 1;
    return [inputs.input * factor];
  }
}
