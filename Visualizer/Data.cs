using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Visualizer
{
    public class Data
    {
        public static string GetOutputsNames(string nodeType, int index)
        {
            var name = "";

            if (nodeType == "scnChoiceNode")
            {
                if (index == 0) name = "Option";
                if (index == 1) name = "AnyOption";
                if (index == 2) name = "Immediate";
                if (index == 3) name = "CancelFwd";
                if (index == 4) name = "NoOption";
                if (index == 5) name = "WhenDisplayed";
                if (index == 6) name = "Reminder";
            }
            else if (nodeType == "scnSectionNode")
            {
                if (index == 0) name = "Out";
                if (index == 1) name = "CancelForward";
                //if (index == 2) name = "TransmitSignal";
                //if (index == 3) name = "StopWork";
            }
            else if (nodeType == "scnStartNode")
            {
                if (index == 0) name = "Out";
            }
            else if (nodeType == "scnRewindableSectionNode")
            {
                if (index == 0) name = "CancelFwd";
                if (index == 1) name = "TransmitSignal";
            }
            else if (nodeType == "scnCutControlNode")
            {
                if (index == 0) name = "Out";
                if (index == 1) name = "CutSource";
            }
            else
                if (index == 0) name = "Out";

            return name != "" ? name + " " : "";
        }

        public static List<ItemInput> GetInputsNames(string nodeType, int inputVarCount = 1)
        {
            List<ItemInput> names = [];

            if (nodeType == "scnChoiceNode")
            {
                names.Add(new() { InputName = "In", Name = "0", Ordinal = "0" });
                names.Add(new() { InputName = "Cancel", Name = "1", Ordinal = "0" });
                names.Add(new() { InputName = "ReactivateGroup", Name = "2", Ordinal = "0" });
                names.Add(new() { InputName = "TimeLimitedFinish", Name = "3", Ordinal = "0" });
            }
            else if (nodeType == "scnEndNode")
            {
                names.Add(new() { InputName = "In", Name = "0", Ordinal = "0" });
            }
            else if (nodeType == "scnStartNode")
            {
            }
            else if (nodeType == "scnRewindableSectionNode")
            {
                names.Add(new() { InputName = "In", Name = "0", Ordinal = "0" });
                names.Add(new() { InputName = "Cancel", Name = "1", Ordinal = "0" });
                names.Add(new() { InputName = "Pause", Name = "2", Ordinal = "0" });
                names.Add(new() { InputName = "ForwardNormal", Name = "3", Ordinal = "0" });
                names.Add(new() { InputName = "ForwardSlow", Name = "4", Ordinal = "0" });
                names.Add(new() { InputName = "ForwardFast", Name = "5", Ordinal = "0" });
                names.Add(new() { InputName = "BackwardNormal", Name = "6", Ordinal = "0" });
                names.Add(new() { InputName = "BackwardSlow", Name = "7", Ordinal = "0" });
                names.Add(new() { InputName = "BackwardFast", Name = "8", Ordinal = "0" });
                names.Add(new() { InputName = "ForwardVeryFast", Name = "9", Ordinal = "0" });
                names.Add(new() { InputName = "BackwardVeryFast", Name = "10", Ordinal = "0" });
            }
            else if (nodeType == "scnCutControlNode")
            {
                names.Add(new() { InputName = "In", Name = "0", Ordinal = "0" });
            }
            else if (nodeType == "scnAndNode")
            {
                for (int i = 0; i < inputVarCount; i++)
                    names.Add(new() { InputName = "In", Name = "0", Ordinal = i.ToString() });
                names.Add(new() { InputName = "Cancel", Name = "1", Ordinal = "0" });
            }
            else
            {
                names.Add(new() { InputName = "In", Name = "0", Ordinal = "0" });
                names.Add(new() { InputName = "Cancel", Name = "1", Ordinal = "0" });
            }

            return names;
        }
    }
}
