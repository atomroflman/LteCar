import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class PowerFunction implements FilterFunctionDef<readonly ["base", "exponent"]> {
  name = 'Power';
  label = 'Power (Potenz)';
  params: FilterFunctionParam[] = [
    { name: 'base', label: 'base (default)', type: 'number', default: 0 },
    { name: 'exponent', label: 'exponent (default)', type: 'number', default: 2 },
  ];
  inputLabels = ["base", "exponent"];
  outputLabels = ['result'];
  apply(inputs: InputMap<readonly ["base", "exponent"]>, params: Record<string, any>) {
    const base = inputs.base ?? params.base ?? 0;
    const exponent = inputs.exponent ?? params.exponent ?? 2;
    return [Math.pow(base, exponent)];
  }
}
