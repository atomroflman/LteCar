var storedToggleValues = {} as Record<number, {switchState: boolean, toggleState: boolean}>;

// Hilfstyp: Aus inputLabels ein Input-Objekt machen
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

class ClampFunction implements FilterFunctionDef<readonly ["input"]> {
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

class ScaleFunction implements FilterFunctionDef<readonly ["input"]> {
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

class ReverseFunction implements FilterFunctionDef<readonly ["input"]> {
  name = 'Reverse';
  label = 'Reverse';
  params: FilterFunctionParam[] = [];
  inputLabels = ["input"] as const;
  outputLabels = ['output'];
  apply(inputs: InputMap<readonly ["input"]>) {
    return [-inputs.input];
  }
}

class AddFunction implements FilterFunctionDef<readonly ["a", "b"]> {
  name = 'Add';
  label = 'Add (Addition)';
  params: FilterFunctionParam[] = [];
  inputLabels = ["a", "b"] as const;
  outputLabels = ['sum'];
  apply(inputs: InputMap<readonly ["a", "b"]>) {
    return [inputs.a + inputs.b];
  }
}

class ToggleFunction implements FilterFunctionDef<readonly ["input"]> {
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

class RescaleFunction implements FilterFunctionDef<readonly ["input"]> {
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

// This function adds a behavior similar to a turn signal in a car.
// It enables the turn signal when the enable input is 1.
// It keeps track if the steering.
// In a car there is a click for each step of the turn signal.
// If the steering is turned against direction and over the click (step), it will disable the turn signal.
class TurnSignal implements FilterFunctionDef<readonly ["enable", "steering"]> {
  // Store state per nodeId: { isEnabled, initialSteering, hasCrossed, isLeftTurn, lastEnable }
  private state: Record<number, { isEnabled: boolean; initialSteering: number; hasCrossed: boolean; isLeftTurn: boolean; lastEnable: number }> = {};
  name = 'TurnSignal';
  label = 'Turn Signal';
  params: FilterFunctionParam[] = [{
    name: 'steps',
    label: 'steps',
    type: 'number',
    default: 5,
  }, {
    name: 'isLeftTurn',
    label: 'isLeftTurn',
    type: 'boolean',
    default: true, // true = left, false = right
  }];
  inputLabels = ["enable", "steering"] as const;
  outputLabels = ['isEnabled'];
  apply(inputs: InputMap<readonly ["enable", "steering"]>, params: Record<string, any>, nodeId?: number) {
        if (nodeId === undefined) return [0];
  const enable = Math.round(inputs.enable);
  const steering = inputs.steering ?? 0;
  const steps = typeof params?.steps === 'number' ? params.steps : 5;
  const isLeftTurn = typeof params?.isLeftTurn === 'boolean' ? params.isLeftTurn : true;

  const clickSize = 1.0 / steps;

  if (!this.state[nodeId]) {
    this.state[nodeId] = {
      isEnabled: false,
      initialSteering: steering,
      isLeftTurn,
      lastEnable: 0,
      maxStepInDirection: 0,
    } as any;
  }

  const s = this.state[nodeId] as typeof this.state[number] & { maxStepInDirection: number };

  // Rising edge: toggle
  if (s.lastEnable === 0 && enable === 1) {
    s.isEnabled = !s.isEnabled;
    if (s.isEnabled) {
      s.initialSteering = steering;
      s.isLeftTurn = isLeftTurn;
      s.maxStepInDirection = 0;
    }
  }

  s.lastEnable = enable;

  if (s.isEnabled) {
    const delta = steering - s.initialSteering;
    const stepIndex = Math.floor(delta / clickSize);

    // Merken wie weit man in Blinker-Richtung gedreht hat
    if (isLeftTurn) {
      if (stepIndex < s.maxStepInDirection) {
        s.maxStepInDirection = stepIndex;
      }
      // Blinker aus, wenn man wieder über max zurückdreht
      if (stepIndex > s.maxStepInDirection) {
        s.isEnabled = false;
      }
    } else {
      if (stepIndex > s.maxStepInDirection) {
        s.maxStepInDirection = stepIndex;
      }
      if (stepIndex < s.maxStepInDirection) {
        s.isEnabled = false;
      }
    }
  }

  return [s.isEnabled ? 1 : 0];
  }
}

export const filterFunctionRegistry = {
  Clamp: new ClampFunction(),
  Scale: new ScaleFunction(),
  Reverse: new ReverseFunction(),
  Add: new AddFunction(),
  Toggle: new ToggleFunction(),
  TurnSignal: new TurnSignal(),
  Rescale: new RescaleFunction(),
};

