using System;
using System.Collections.Generic;

public class GatingVariable
{
    public Func<double, double> Alpha { get; set; }
    public Func<double, double> Beta { get; set; }
    public string Name { get; set; }
    public List<double> Coefficients { get; set; }

    public GatingVariable(string name, Func<double, double> alpha, Func<double, double> beta)
    {
        Name = name;
        Alpha = alpha;
        Beta = beta;
        Coefficients = new List<double>();
    }
}

public class IonChannel
{
    public string Name { get; set; }                  // Name of the ion channel (e.g., K+ channel)
    public double Conductance { get; set; }           // g (conductance)
    public double ReversalPotential { get; set; }     // E (reversal potential)
    public List<GatingVariable> GatingVariables { get; set; } // List of gating variables (can be dynamic)

    public IonChannel(string name, double conductance, double reversalPotential)
    {
        Name = name;
        Conductance = conductance;
        ReversalPotential = reversalPotential;
        GatingVariables = new List<GatingVariable>();
    }

    public void AddGatingVariable(GatingVariable gatingVariable)
    {
        GatingVariables.Add(gatingVariable);
    }

    public void RemoveGatingVariable(string name)
    {
        GatingVariables.RemoveAll(gv => gv.Name == name);
    }

    public double CalculateCurrent(double voltage)
    {
        double totalCurrent = 0;

        foreach (var gatingVariable in GatingVariables)
        {
            double alpha = gatingVariable.Alpha(voltage);
            double beta = gatingVariable.Beta(voltage);
            foreach (double Coefficient in gatingVariable.Coefficients)
            {
                totalCurrent += Coefficient * (alpha - beta);
            }
        }

        return Conductance * (voltage - ReversalPotential) * totalCurrent;
    }
}
