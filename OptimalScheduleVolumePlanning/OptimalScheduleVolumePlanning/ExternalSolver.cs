using System.Collections.Generic;
using System.IO;

public class ExternalSolver
{
    private const string fileName = "externalCuts.txt";
    private const int cutFieldsAmount = 3;

    private string path;
    private FileStream fileStream;
    private List<FormatPlanCut> cuts;

    public ExternalSolver(string path)
    {
        this.path = path;
    }

    public List<FormatPlanCut> Solve(List<Order> orders, WorkCenterMachine machine)
    {
        var curPath = path.Contains(fileName) ? path : Path.Combine(path, fileName);
        if (!File.Exists(curPath))
            return null;

        cuts = new();
        using (fileStream = File.OpenRead(curPath))
            ParseData();

        return cuts;
    }

    private void ParseData()
    {
        int s;
        while (!CheckByteEqualTo(s = fileStream.ReadByte(), -1))
        {
            var cutItems = GetCutItems(s);
            if (cutItems == null)
                continue;
            
            var planCut = GetPlanCut();
            if (planCut == null)
                continue;
            
            planCut.Items = cutItems;
            cuts.Add(planCut);
        }
    }

    private FormatPlanCutItem[] GetCutItems(int startByte)
    {
        var cutItems = new List<FormatPlanCutItem>();
        int s = startByte;
        do
        {
            var cData = GetCutData(s);
            var cItem = ConvertCutData(cData);
            if (cItem != null)
                cutItems.Add(cItem);
        } while (!CheckByteEqualTo(s = fileStream.ReadByte(), '|') && !CheckByteEqualTo(s, -1));
        
        return cutItems.ToArray();
    }

    private string[] GetCutData(int startByte)
    {
        string cData = string.Empty;
        int s = startByte;
        do
        {
            cData += (char)s;
        } while (!CheckByteEqualTo(s = fileStream.ReadByte(), '\n') && !CheckByteEqualTo(s, -1));

        return cData.Split(';');
    }

    private FormatPlanCutItem ConvertCutData(string[] line)
    {
        if (line.Length != cutFieldsAmount)
            return null;

        return new FormatPlanCutItem()
        {
            Format = int.Parse(line[0]),
            Count = int.Parse(line[1]),
            IdOrderMan = long.Parse(line[2])
        };
    }

    private FormatPlanCut GetPlanCut()
    {
        string line = string.Empty;
        int s;
        while (!CheckByteEqualTo(s = fileStream.ReadByte(), '|') && !CheckByteEqualTo(s, -1))
            line += (char)s;
        return new FormatPlanCut() { Count = int.Parse(line) };
    }

    private bool CheckByteEqualTo(int checkByte, char checkChar)
        => (char)checkByte == checkChar;

    private bool CheckByteEqualTo(int checkByte, int checkInt)
        => checkByte == checkInt;
}
