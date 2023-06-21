using System;
using System.Collections.Generic;
using System.Linq;

public class Solver
{
    public List<FormatPlanCut> Step2(List<FormatPlanCut> cuts, List<Order> orders,
        Dictionary<long, ProductionBoundaries> productBoundaries)
    {
        var _cuts = new List<FormatPlanCut>();
        foreach (var cut in cuts)
            for (int i = 0; i < cut.Count; i++)
                _cuts.Add(new FormatPlanCut(cut.Items, 1));
        cuts = _cuts;

        var orderById = orders.ToDictionary(order => order.Id);

        var extendedCuts = cuts.Select(cut => new ExtendedCut(
            cut,
            cut.Items.Select(item => orderById[item.IdOrderMan].Date).Min(),
            orderById[cut.Items.First().IdOrderMan].IdProductType,
            cut.Items.Select(item => orderById[item.IdOrderMan].OneRollMass * item.Count).Min()
            )).OrderBy(pair => pair.Date).ToList();

        var answerCuts = new List<FormatPlanCut>();
        long previousType = -1;

        while (extendedCuts.Count > 0)
        {
            var type = GetType(extendedCuts, productBoundaries, previousType);

            GetVolume(extendedCuts, type, productBoundaries,
                out double minVolume, out double maxVolume);

            var newExtentedCuts = new List<ExtendedCut>();
            var currentVolume = 0.0;

            var flag = true;
            for (int i = 0; i < extendedCuts.Count; i++)
                if (extendedCuts[i].IdProductType == type
                    && (currentVolume <= minVolume || flag && currentVolume <= maxVolume))
                {
                    answerCuts.Add(extendedCuts[i].Cut);
                    currentVolume += extendedCuts[i].Volume;
                }
                else
                {
                    newExtentedCuts.Add(extendedCuts[i]);
                    flag = false;
                }

            extendedCuts = newExtentedCuts;
            previousType = type;
        }

        return answerCuts;
    }

    private long GetType(List<ExtendedCut> cuts,
        Dictionary<long, ProductionBoundaries> productBoundaries, long previousType)
    {
        var volumes = cuts.GroupBy(cut => cut.IdProductType,
            (t, group) => new Tuple<long, double>(t, group.Select(c => c.Volume).Sum()))
            .ToDictionary(pair => pair.Item1, pair => pair.Item2);

        var min = volumes.ToDictionary(pair => pair.Key,
            pair => Math.Ceiling(pair.Value / productBoundaries[pair.Key].MaximumVolumeInTons));

        var max = volumes.ToDictionary(pair => pair.Key,
            pair => Math.Ceiling(pair.Value / productBoundaries[pair.Key].MinimumVolumeInTons));

        var sum = max.Select(pair => pair.Value).Sum();

        foreach (var key in min.Keys)
            if (min[key] > sum - max[key])
                return key;

        var type = cuts.First(cut => cut.IdProductType != previousType).IdProductType;

        return type;
    }

    private bool GetVolume(List<ExtendedCut> cuts, long type,
        Dictionary<long, ProductionBoundaries> productBoundaries,
        out double bottom, out double top)
    {
        var flag = Culc(productBoundaries[type].MinimumVolumeInTons, productBoundaries[type].MaximumVolumeInTons,
            cuts.Where(cut => cut.IdProductType == type).Select(cut => cut.Volume).Sum(),
            out bottom, out top);
        return flag;
    }

    private bool Culc(double minimum, double maximum, double value, out double bottom, out double top)
    {
        if (!IsFeasible(value, minimum, maximum))
        {
            bottom = minimum;
            top = maximum;
            return false;
        }

        if (value <= maximum)
        {
            if (value <= 2 * minimum)
            {
                bottom = value;
                top = value;
                return true;
            }
            bottom = minimum;
            top = value - minimum;
            return true;
        }

        var k = Math.Ceiling((value - maximum) / maximum);

        if (k * (maximum - minimum) >= minimum)
        {
            bottom = minimum;
            top = maximum;
            return true;
        }

        bottom = Math.Max(minimum, value - k * maximum);
        top = Math.Min(maximum, value - k * minimum);
        return value >= minimum;
    }

    private bool IsFeasible(double productVolumeInTons, double minimumVolumeInTons, double maximumVolumeInTons)
        => Math.Ceiling(productVolumeInTons / maximumVolumeInTons) * minimumVolumeInTons <= productVolumeInTons;

    public List<FormatPlanCut> Step3(List<FormatPlanCut> cuts, List<Order> orders, int dayDelta,
        DateTime startDate, double speedPM)
    {
        var orderById = orders.ToDictionary(order => order.Id);

        var extendedCuts = cuts.Select(cut => new ExtendedCut(
            cut,
            cut.Items.Select(item => orderById[item.IdOrderMan].Date).Min(),
            orderById[cut.Items.First().IdOrderMan].IdProductType,
            cut.Items.Select(item => orderById[item.IdOrderMan].OneRollMass * item.Count).Min()
            )).OrderBy(pair => pair.Date).ToList();

        var limits = GetLimits(extendedCuts);

        var record = Cost(extendedCuts);
        bool flag = false;

        do
        {
            flag = false;
            foreach (var indices in limits)
                for (int i = indices.Item1; i < indices.Item2; i++)
                    for (int j = i + 1; j <= indices.Item2; j++)
                        if (DayDifference(extendedCuts[i], extendedCuts[j]) < dayDelta)
                        {
                            Swap(extendedCuts, i, j);
                            var newCost = Cost(extendedCuts);
                            if (Acceptably(extendedCuts, startDate, speedPM) && record < newCost)
                            {
                                record = newCost;
                                flag = true;
                                break;
                            }
                            Swap(extendedCuts, i, j);
                        }
        }
        while (flag);

        return extendedCuts.Select(cut => cut.Cut).ToList();
    }

    private List<Tuple<int, int>> GetLimits(List<ExtendedCut> cuts)
    {
        var limitsForCuts = new List<Tuple<int, int>>();
        int startIndex = 0;
        for (int i = 1; i <= cuts.Count; i++)
            if (i == cuts.Count || cuts[i].IdProductType != cuts[i - 1].IdProductType)
            {
                if (i - 1 > startIndex)
                    limitsForCuts.Add(new Tuple<int, int>(startIndex, i - 1));
                startIndex = i;
            }

        return limitsForCuts;
    }

    private int Cost(List<ExtendedCut> cuts)
    {
        var s = 0;
        for (int i = 1; i < cuts.Count; i++)
            s += KnifeMovementsNumberCalculation(cuts[i - 1], cuts[i]);
        return s;
    }

    private int DayDifference(ExtendedCut cut1, ExtendedCut cut2)
    {
        return Math.Abs(cut1.Date.Subtract(cut2.Date).Days);
    }

    private void Swap(List<ExtendedCut> cuts, int i, int j)
    {
        var tmp = cuts[i];
        cuts[i] = cuts[j];
        cuts[j] = tmp;
    }

    private bool Acceptably(List<ExtendedCut> cuts, DateTime startDate, double speedPM)
    {
        var currentDate = startDate;
        for (int i = 0; i < cuts.Count; i++)
        {
            currentDate = currentDate.AddHours(cuts[i].Volume / speedPM);
            if (currentDate > cuts[i].Date)
                return false;
        }
        return true;
    }

    private int KnifeMovementsNumberCalculation(ExtendedCut cut1, ExtendedCut cut2)
    {
        int knifeMovementsNumber = Math.Abs(cut1.KnifeOffsets.Count - cut2.KnifeOffsets.Count);
        int min = Math.Min(cut1.KnifeOffsets.Count, cut2.KnifeOffsets.Count);

        for (int k = 0; k < min; k++)
            if (cut1.KnifeOffsets[k] != cut2.KnifeOffsets[k])
                knifeMovementsNumber++;

        return knifeMovementsNumber;
    }
}
