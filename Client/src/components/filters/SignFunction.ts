import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class SignFunction implements FilterFunctionDef<readonly ["input"]> {
  name = "Sign";
  label = "Sign";
  params: FilterFunctionParam[] = [
    { name: "input", label: "input (default)", type: "number", default: 0 },
  ];
  inputLabels = ["input"];
  outputLabels = ["sign"];

  apply(inputs: InputMap<readonly ["input"]>, params: Record<string, any>) {
    const value = Number(inputs.input ?? params.input ?? 0);
    // Math.sign returns NaN for non-numeric inputs; defaulting ensures numeric output.
    const sign = Math.sign(value);
    return [Number.isNaN(sign) ? 0 : sign];
  }
}
