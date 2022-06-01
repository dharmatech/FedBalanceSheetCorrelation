﻿using System;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

using System.Globalization;

namespace FedBalanceSheetCorrelation // Note: actual namespace depends on the project name.
{
    public class SpxCsvRow
    {
        [CsvHelper.Configuration.Attributes.Index(0)] public long Time { get; set; }
        [CsvHelper.Configuration.Attributes.Index(1)] public decimal Open { get; set; }
        [CsvHelper.Configuration.Attributes.Index(2)] public decimal High { get; set; }
        [CsvHelper.Configuration.Attributes.Index(3)] public decimal Low { get; set; }
        [CsvHelper.Configuration.Attributes.Index(4)] public decimal Close { get; set; }
    }

    public class SpxRow
    {
        public DateTime DateTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }

    public class FedBalanceSheetCsvRow
    {
        [CsvHelper.Configuration.Attributes.Index(0)] public DateTime Date { get; set; }
        [CsvHelper.Configuration.Attributes.Index(1)] public decimal Change { get; set; }
    }

    public class Data
    {
        public DateTime DateTime { get; set; }
        public decimal FedBalanceSheetChange { get; set; }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public decimal Open { get; set; }

        public decimal Close { get; set; }

        public decimal Change { get; set; }


        public decimal High { get; set; }
        public decimal Low { get; set; }


        //public decimal HighChgPct { get; set; }
        public decimal HighChangePercent { get; set; }

        public decimal LowChangePercent { get; set; }



        // if    balance sheet change > 0    AND    HighChangePercent >  1%    THEN   1
        // if    balance sheet change < 0    AND    LowChangePercent  < -1%    THEN   1

        // if    balance sheet change > 0    AND    HighChangePercent < -1%    THEN  -1
        // if    balance sheet change < 0    AND    LowChangePercent  >  1%    THEN  -1

        // ELSE    0

        // threshold = 1% by default

        public int Influence1 { get; set; }

        public int Influence2 { get; set; }
        public int Influence3 { get; set; }
        
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { };
                        
            var spx_csv_items = new CsvReader(new StreamReader(@"C:\Users\dharm\Downloads\SP_SPX, 1D.csv"), config)
                .GetRecords<SpxCsvRow>()
                .ToList();

            var spx_items = spx_csv_items.Select(item =>
                new SpxRow()
                {
                    DateTime = DateTimeOffset.FromUnixTimeSeconds(item.Time).UtcDateTime.Date,
                    Open = item.Open,
                    High = item.High,
                    Low = item.Low,
                    Close = item.Close
                });

            var fed_balance_sheet_items = 
                new CsvReader(new StreamReader(@"C:\Users\dharm\Downloads\WALCL-weekly-changes.csv"), config)
                .GetRecords<FedBalanceSheetCsvRow>()
                .ToList();


            // Wednesday

            //var result = fed_balance_sheet_items.SkipLast(1).Select(item =>
            //    {
            //        var a = spx_items.First(elt => elt.DateTime == item.Date);

            //        var b = spx_items.First(elt => elt.DateTime == a.DateTime.AddDays(7));

            //        return new Data()
            //        {
            //            DateTime = item.Date,
            //            FedBalanceSheetChange = item.Change,
            //            SpxChange = b.Close - a.Close
            //        };
            //    });



            //var result = fed_balance_sheet_items.SkipLast(10).Select(item =>
            //    {
            //        var a = spx_items.FirstOrDefault(elt => elt.DateTime == item.Date.AddDays(1));

            //        var b = spx_items.FirstOrDefault(elt => elt.DateTime == a.DateTime.AddDays(7));

            //        if (a == null || b == null)
            //        {
            //            return new Data()
            //            {
            //                DateTime = item.Date,
            //                FedBalanceSheetChange = item.Change,
            //                SpxChange = 0
            //            };
            //        }

            //        else
            //            return new Data()
            //            {
            //                DateTime = item.Date,
            //                FedBalanceSheetChange = item.Change,
            //                SpxChange = b.Close - a.Close
            //            };
            //    });


            // Thursday

            //var result = fed_balance_sheet_items.SkipLast(1).Select(item =>
            //{
            //    var null_item = new Data()
            //    {
            //        DateTime = item.Date,
            //        FedBalanceSheetChange = item.Change,
            //        SpxChange = 123456
            //    };

            //    var a = spx_items.FirstOrDefault(elt => elt.DateTime == item.Date.AddDays(1));

            //    if (a == null) return null_item;

            //    var b = spx_items.FirstOrDefault(elt => elt.DateTime == a.DateTime.AddDays(7));

            //    if (b == null) return null_item;

            //    return new Data()
            //    {
            //        DateTime = item.Date,
            //        FedBalanceSheetChange = item.Change,
            //        SpxChange = b.Close - a.Close
            //    };
            //});



            // max and min gain

            var result = fed_balance_sheet_items.SkipLast(1).Select(item =>
            {
                //var null_item = new Data()
                //{
                //    DateTime = item.Date,
                //    FedBalanceSheetChange = item.Change,
                //    SpxChange = 123456
                //};
                                
                var items = spx_items
                    .SkipWhile(elt => elt.DateTime < item.Date)
                    .TakeWhile(elt => elt.DateTime <= item.Date.AddDays(7))
                    .ToList();

                var high = items.Max(elt => elt.High);

                var low = items.Min(elt => elt.Low);

                var open = items.First().Open;

                // decimal percent(decimal a, decimal b) => (a - b) / b;

                var high_change_percent = (high - open) / open;
                var low_change_percent  = (low  - open) / open;


                var threshold = (decimal) 0.01;

                //int influence(decimal threshold) =>
                //    item.Change > 0 ? (high_change_percent > threshold ? 1 : 0) :
                //    item.Change < 0 ? (low_change_percent * -1 > threshold ? 1 : 0) :
                //    0;

                //int influence(decimal threshold) =>
                //    item.Change > 0 ? (high_change_percent     > threshold ? 1 : low_change_percent * -1 > threshold ? -1 : 0) :
                //    item.Change < 0 ? (low_change_percent * -1 > threshold ? 1 : high_change_percent     > threshold ? -1 : 0) :
                //    0;


                int influence(decimal threshold)
                {
                    var pro  = high_change_percent     > threshold;
                    var anti = low_change_percent * -1 > threshold;

                    if (item.Change > 0) if (pro)  return 1; else if (anti) return -1; else return 0;
                    if (item.Change < 0) if (anti) return 1; else if (pro)  return -1; else return 0;

                    return 123;
                }

                return new Data()
                {
                    DateTime = item.Date,
                    FedBalanceSheetChange = item.Change,

                    Start = items.First().DateTime,
                    End = items.Last().DateTime,

                    Open = open,
                    Close = items.Last().Close,

                    Change = items.Last().Close - open,

                    High = high,
                    Low = low,

                    HighChangePercent = (high - open) / open,
                    LowChangePercent = (low - open) / open,

                    //Influence = 
                    //    item.Change > 0 ? (high_change_percent     > threshold ? 1 : 0) :
                    //    item.Change < 0 ? (low_change_percent * -1 > threshold ? 1 : 0) :
                    //    0,

                    Influence1 = influence(0.01m),
                    Influence2 = influence(0.02m),
                    Influence3 = influence(0.03m)

                };
            });




            //var result_a = result.Where(elt => elt.SpxChange != 123456);

            ////var result_b = result_a
            ////    //.Where(elt => elt.FedBalanceSheetChange > 0)
            ////    .Count(elt => elt.FedBalanceSheetChange * elt.SpxChange > 0)
            ////    ;

            //Console.WriteLine(
            //    (double)result_a.Count(elt => elt.FedBalanceSheetChange * elt.SpxChange > 0)
            //    / 
            //    (double)result_a.Count()
            //    );


            using (var writer = new StreamWriter(@"c:\temp\out.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(result);
            }

            // If balance sheet grew        was gain more than 1%
            // If balance sheet dropped     was loss more than 1%
        }
    }
}