import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class ClampFunction implements FilterFunctionDef<readonly ["input"]> {
  name = 'Clamp';
  label = 'Clamp';
  params: FilterFunctionParam[] = [
    { name: 'min', label: 'Minimum', type: 'number', default: -1 },
    { name: 'max', label: 'Maximum', type: 'number', default: 1 },
  ];
  inputLabels = ["input"] as const;
  outputLabels = ['output'];
  apply(inputs: InputMap<readonly ["input"]>, params: Record<string, any>) {
    const min = params?.min ?? -1;
    const max = params?.max ?? 1;
    return [Math.max(min, Math.min(max, inputs.input))];
  }
}
