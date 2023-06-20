using System;
using System.Collections.Generic;
using System.IO;

public class OrdersDataParser
{
    private const string fileName = "inputOrders.txt";
    private const int orderFieldsAmount = 11;

    private FileStream fileStream;
    private List<Order> orders;

	public List<Order> GetOrders(string path)
    {
        var curPath = path.Contains(fileName) ? path : Path.Combine(path, fileName);
        if (!File.Exists(curPath))
            return null;

        orders = new();
        using (fileStream = File.OpenRead(curPath))
            ParseData();

        return orders;
    }

    private void ParseData()
    {
        int s;
        while (!CheckByteEqualTo(s = fileStream.ReadByte(), -1))
        {
            var oData = GetOrderData(s);
            var order = ConvertOrderData(oData);
            if (order != null)
                orders.Add(order);
        }
    }

    private string[] GetOrderData(int startByte)
    {
        string line = string.Empty;   
        int s = startByte;
        do
        {
            line += (char)s;
        } while (!CheckByteEqualTo(s = fileStream.ReadByte(), '\n') && !CheckByteEqualTo(s, -1));

        return line.Split(';');
    }

    private Order ConvertOrderData(string[] line)
    {
        if (line.Length != orderFieldsAmount)
            return null;

        return new Order()
        {
            Id = int.Parse(line[0]),
            Date = DateTime.Parse(line[1]),
            FormatInMillimeters = int.Parse(line[2]),
            DiameterInMillimeters = int.Parse(line[3]),
            IdProductType = line[4] == "T" ? 0 : 1,
            GrammageInGramPerSquareMeter = double.Parse(line[5]),
            NumberOfLayers = int.Parse(line[6]),
            VolumeMaxInTons = double.Parse(line[7]),
            VolumeMinInTons = double.Parse(line[8]),
            OneRollMass = double.Parse(line[9]),
            NumberOfRolls = int.Parse(line[10])
        };
    }

    private bool CheckByteEqualTo(int checkByte, char checkChar)
        => (char)checkByte == checkChar;

    private bool CheckByteEqualTo(int checkByte, int checkInt)
        => checkByte == checkInt;
}
