using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGIOController.Models; 

public class TruthTable {
    public readonly struct TruthTableSettings {
        public IReadOnlyList<int> OutputOrder { get; init; }
        public IReadOnlyList<int> InputOrder { get; init; }
        public IReadOnlyList<string> Labels { get; init; }
        public TimeSpan Delay { get; init; }
    }
    
    public async static Task<List<List<bool>>> GenerateTruthTable(IDigioController controller, TruthTableSettings settings) {
        Bit[] outputs = await controller.Outputs.FirstAsync();
        Bit[] inputs = await controller.Inputs.FirstAsync();

        List<List<bool>> result = new(1 << settings.OutputOrder.Count);

        for (int i = 0; i < (1 << settings.OutputOrder.Count); i++) {
            int iShifted = i;
            foreach (int bit in settings.OutputOrder.Reverse()) {
                outputs[bit].Set = (iShifted & 1) == 1;
                iShifted >>= 1;
            }
            await Task.Delay(settings.Delay);
            List<bool> row = new(settings.OutputOrder.Count + settings.InputOrder.Count);
            row.AddRange(settings.OutputOrder.Select(bit => outputs[bit].Set));
            row.AddRange(settings.InputOrder.Select(bit => inputs[bit].Set));
            result.Add(row);
        }
        return result;
    }

    public static string ConvertToCsv(List<List<bool>> truthTable, TruthTableSettings settings) {
        StringBuilder result = new();
        foreach (string label in settings.Labels) {
            result.Append(label).Append(',');
        }
        result.Remove(result.Length - 1, 1);
        result.AppendLine();
        foreach (List<bool> row in truthTable) {
            foreach (bool bit in row) {
                result.Append(bit ? "1," : "0,");
            }
            result.Remove(result.Length - 1, 1);
            result.AppendLine();
        }
        return result.ToString();
    }
}