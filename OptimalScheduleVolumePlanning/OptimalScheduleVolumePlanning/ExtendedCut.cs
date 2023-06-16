using System;
using System.Collections.Generic;

public class ExtendedCut
{
    public FormatPlanCut Cut;
    public DateTime Date;
    public long IdProductType;
    public double Volume;
    public List<int> KnifeOffsets;

    public ExtendedCut(FormatPlanCut cut, DateTime date, long idProductType, double volume)
    {
        Cut = cut;
        Date = date;
        IdProductType = idProductType;
        Volume = volume;
        KnifeOffsets = KnifeOffsetsCalculation(Cut);
    }

    private List<int> KnifeOffsetsCalculation(FormatPlanCut cut)
    {
        int knifeOffset = 0;
        var knifeOffsets = new List<int>();

        for (int i = 0; i < cut.Items.Length; i++)
            for (int k = 0; k < cut.Items[i].Count; k++)
            {
                knifeOffset += cut.Items[i].Format;
                knifeOffsets.Add(knifeOffset);
            }
        return knifeOffsets;
    }
}