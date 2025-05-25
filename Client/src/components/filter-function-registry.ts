// client-side filter function registry for SetupFilterType.TypeName
export const filterFunctionRegistry: Record<string, (input: any, params?: any) => any> = {
  // Beispiel: "Clamp" filter
  Clamp: (input, params) => {
    const min = params?.min ?? -1;
    const max = params?.max ?? 1;
    return Math.max(min, Math.min(max, input));
  },
  // Beispiel: "Scale" filter
  Scale: (input, params) => {
    const factor = params?.factor ?? 1;
    return input * factor;
  },
  Reverse: (input) => {
    return -input;
  }
};
