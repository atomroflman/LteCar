import { FilterFunctionDef, FilterFunctionParam, InputMap, storedToggleValues } from "./filter-function-registry";

export class ToggleFunction implements FilterFunctionDef<readonly ["input"]> {
  name = 'Toggle';
  label = 'Toggle (Umschalten)';
  params: FilterFunctionParam[] = [];
  inputLabels = ["input"] as const;
  outputLabels = ['Out'];
  apply(inputs: InputMap<readonly ["input"]>, params: Record<string, any>, nodeId?: number) {
    if (nodeId === undefined) return [0];

    const input = Math.round(inputs.input ?? 0);
    if (!storedToggleValues[nodeId]) {
      storedToggleValues[nodeId] = { switchState: false, toggleState: false };
    }

    const state = storedToggleValues[nodeId];
    if (input === 1 && !state.switchState) {
      state.toggleState = !state.toggleState;
    }
    state.switchState = input === 1;

    return [state.toggleState ? 1 : 0];
  }
}
