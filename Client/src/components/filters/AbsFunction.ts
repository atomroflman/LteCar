import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class AbsFunction implements FilterFunctionDef<readonly ["input"]> {
  name = "Abs";
  label = "Absolute Value";
  params: FilterFunctionParam[] = [
    { name: "input", label: "input (default)", type: "number", default: 0 },
  ];
  inputLabels = ["input"];
  outputLabels = ["absolute"];

  apply(inputs: InputMap<readonly ["input"]>, params: Record<string, any>) {
    const value = Number(inputs.input ?? params.input ?? 0);
    return [Math.abs(value)];
  }
}
