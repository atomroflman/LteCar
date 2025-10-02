import { FilterFunctionDef, FilterFunctionParam, InputMap } from "./filter-function-registry";

export class IifFunction implements FilterFunctionDef<readonly ["a", "b", "trueValue", "falseValue"]> {
  name = 'Iif';
  label = 'IIF (If-Then-Else)';
  params: FilterFunctionParam[] = [
    {
      name: 'operator',
      label: 'Operator',
      type: 'string',
      default: '>',
    }
  ];
  inputLabels = ["a", "b", "ifTrue", "ifFalse"];
  outputLabels = ['result'];
  apply(inputs: InputMap<readonly ["a", "b", "trueValue", "falseValue"]>, params: Record<string, any>) {
    const a = inputs.a ?? 0;
    const b = inputs.b ?? 0;
    const trueValue = inputs.trueValue ?? 0;
    const falseValue = inputs.falseValue ?? 0;
    const operator = params?.operator || '>';
    
    let condition = false;
    
    switch (operator) {
      case '>':
        condition = a > b;
        break;
      case '<':
        condition = a < b;
        break;
      case '=':
      case '==':
        condition = Math.abs(a - b) < 0.00001; // Float comparison with tolerance
        break;
      case '>=':
        condition = a >= b;
        break;
      case '<=':
        condition = a <= b;
        break;
      case '!=':
      case '<>':
        condition = Math.abs(a - b) >= 0.00001; // Float comparison with tolerance
        break;
      default:
        condition = a > b; // Default to greater than
        break;
    }
    
    return [condition ? trueValue : falseValue];
  }
}
