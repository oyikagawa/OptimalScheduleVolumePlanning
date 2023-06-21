using System;
using System.Collections.Generic;

namespace OptimalScheduleVolumePlanning
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<long, ProductionBoundaries> productBoundaries = new()
            {
                [0] = new ProductionBoundaries(30, 1100),
                [1] = new ProductionBoundaries(200, 240)
            };

            Console.WriteLine("Write path to input file:");
            var inputFilePath = Console.ReadLine();
            if (inputFilePath == null)
                return;

            var parser = new OrdersDataParser();
            var orders = parser.GetOrders(inputFilePath);

            var eSolver = new ExternalSolver(inputFilePath);
            var cuts = eSolver.Solve(orders, new WorkCenterMachine());

            var solver = new Solver();
            cuts = solver.Step2(cuts, orders, productBoundaries);

            int dayDelta = 3;
            double speedPM = 10.0;
            DateTime startDate = new DateTime(2022, 11, 1);
            cuts = solver.Step3(cuts, orders, dayDelta, startDate, speedPM);
        }
    }
}
