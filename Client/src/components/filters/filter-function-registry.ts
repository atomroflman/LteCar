import { AddFunction } from "./AddFunction";
import { ClampFunction } from "./ClampFunction";
import { DivideFunction } from "./DivideFunction";
import { FloatValueFunction } from "./FloatValueFunction";
import { GearboxFunction } from "./GearboxFunction";
import { IifFunction } from "./IifFunction";
import { LogFunction } from "./LogFunction";
import { ModuloFunction } from "./ModuloFunction";
import { MultiplyFunction } from "./MultiplyFunction";
import { PowerFunction } from "./PowerFunction";
import { RescaleFunction } from "./RescaleFunction";
import { ReverseFunction } from "./ReverseFunction";
import { ScaleFunction } from "./ScaleFunction";
import { SmoothFunction } from "./SmoothFunction";
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
  inputLabels: string[];
  outputLabels: string[];
  init?: () => void;
  apply: (inputs: InputMap<TInputs>, params: Record<string, any>, nodeId?: number) => (number | string)[];
};

export const filterFunctionRegistry = {
  Clamp: new ClampFunction(),
  Scale: new ScaleFunction(),
  Reverse: new ReverseFunction(),
  Add: new AddFunction(),
  Multiply: new MultiplyFunction(),
  Divide: new DivideFunction(),
  Modulo: new ModuloFunction(),
  Power: new PowerFunction(),
  Log: new LogFunction(),
  Iif: new IifFunction(),
  Toggle: new ToggleFunction(),
  TurnSignal: new TurnSignal(),
  Rescale: new RescaleFunction(),
  Gearbox: new GearboxFunction(),
  FloatValue: new FloatValueFunction(),
  Smooth: new SmoothFunction(),
};

