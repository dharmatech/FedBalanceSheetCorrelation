using System;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

using System.Globalization;

namespace FedBalanceSheetCorrelation
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
                        
            // From TradingView CSV export

            var spx_csv_items = new CsvReader(new StreamReader(@"..\..\..\SP_SPX, 1D.csv"), config)
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

            // From:
            // 
            //     https://fred.stlouisfed.org/graph/?g=PRKH

            var fed_balance_sheet_items = 
                new CsvReader(new StreamReader(@"..\..\..\WALCL.csv"), config)
                .GetRecords<FedBalanceSheetCsvRow>()
                .ToList();

            var result = fed_balance_sheet_items.SkipLast(1).Select(item =>
            {
                // Wednesday to Wednesday

                //var items = spx_items
                //    .SkipWhile(elt => elt.DateTime < item.Date)
                //    .TakeWhile(elt => elt.DateTime <= item.Date.AddDays(7))
                //    .ToList();

                // Wednesday to Tuesday

                //var items = spx_items
                //    .SkipWhile(elt => elt.DateTime < item.Date)
                //    .TakeWhile(elt => elt.DateTime < item.Date.AddDays(7))
                //    .ToList();

                // Friday to Thursday

                var items = spx_items
                    .SkipWhile(elt => elt.DateTime < item.Date.AddDays(2))
                    .TakeWhile(elt => elt.DateTime < item.Date.AddDays(2+7))
                    .ToList();

                var high = items.Max(elt => elt.High);

                var low = items.Min(elt => elt.Low);

                var open = items.First().Open;

                // decimal percent(decimal a, decimal b) => (a - b) / b;

                var high_change_percent = (high - open) / open;
                var low_change_percent  = (low  - open) / open;


                var threshold = (decimal) 0.01;

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

                    Influence1 = influence(0.01m),
                    Influence2 = influence(0.02m),
                    Influence3 = influence(0.03m)

                };
            });

            using (var writer = new StreamWriter(@"..\..\..\out.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(result);
            }
        }
    }
}