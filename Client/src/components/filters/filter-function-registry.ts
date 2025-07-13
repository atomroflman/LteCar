import { AddFunction } from "./AddFunction";
import { ClampFunction } from "./ClampFunction";
import { RescaleFunction } from "./RescaleFunction";
import { ReverseFunction } from "./ReverseFunction";
import { ScaleFunction } from "./ScaleFunction";
import { ToggleFunction } from "./ToggleFunction";
import { TurnSignal } from "./TurnSignal";

export var storedToggleValues = {} as Record<number, {switchState: boolean, toggleState: boolean}>;

export type InputMap<T extends readonly string[]> = { [K in T[number]]: number };

export type FilterFunctionParam = {
  name: string;
  label: string;
  type: 'number' | 'string' | 'boolean';
  default: any;
};

export type FilterFunctionDef<TInputs extends ReadonlyArray<string>> = {
  name: string;
  label: string;
  params: FilterFunctionParam[];
  inputLabels: TInputs; // readonly string[]
  outputLabels: string[];
  apply: (inputs: InputMap<TInputs>, params: Record<string, any>, nodeId?: number) => number[];
};

export const filterFunctionRegistry = {
  Clamp: new ClampFunction(),
  Scale: new ScaleFunction(),
  Reverse: new ReverseFunction(),
  Add: new AddFunction(),
  Toggle: new ToggleFunction(),
  TurnSignal: new TurnSignal(),
  Rescale: new RescaleFunction(),
};

