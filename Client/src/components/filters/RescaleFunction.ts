import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class RescaleFunction implements FilterFunctionDef<readonly ["input"]> {
  name = 'Rescale';
  label = 'Rescale';
  params: FilterFunctionParam[] = [
    { name: 'inMin', label: 'Input Minimum', type: 'number', default: -1 },
    { name: 'inMax', label: 'Input Maximum', type: 'number', default: 1 },
    { name: 'outMin', label: 'Output Minimum', type: 'number', default: 0 },
    { name: 'outMax', label: 'Output Maximum', type: 'number', default: 1 },
  ];
  inputLabels = ["input"] as const;
  outputLabels = ['output'];

  apply(inputs: InputMap<readonly ["input"]>, params: Record<string, any>) {
    const input = inputs.input ?? 0;
    const inMin = typeof params?.inMin === 'number' ? params.inMin : -1;
    const inMax = typeof params?.inMax === 'number' ? params.inMax : 1;
    const outMin = typeof params?.outMin === 'number' ? params.outMin : 0;
    const outMax = typeof params?.outMax === 'number' ? params.outMax : 1;

    if (inMax === inMin) return [outMin]; // Vermeidung von Division durch 0

    const ratio = (input - inMin) / (inMax - inMin);
    const scaled = outMin + ratio * (outMax - outMin);

    return [scaled];
  }
}
