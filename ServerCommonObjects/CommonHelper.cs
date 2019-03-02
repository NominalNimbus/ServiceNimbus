/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CommonObjects;

namespace ServerCommonObjects
{
    public static class CommonHelper
    {
        public static List<Bar> CompressBars(List<Bar> bars, Selection parameters)
        {
            if (parameters == null || bars == null || bars.Count < 2)
                return bars;

            var compressedBars = new List<Bar>();
            Bar currentBar = null;
            DateTime nextBarTime = DateTime.MinValue;
            foreach (var bar in bars)
            {
                if(bar.Date >= nextBarTime)
                {
                    var barDate = GetIdealBarTime(bar.Date, parameters.Timeframe, parameters.TimeFactor);
                    currentBar =new Bar(bar, barDate);
                    nextBarTime = GetNextBarStart(currentBar.Date, parameters.Timeframe, parameters.TimeFactor);
                    compressedBars.Add(currentBar);
                }
                else
                {
                    currentBar.AppendBar(bar);
                }
            }

            return compressedBars;
        }

        public static List<Bar> CombineBars(List<Bar> mainSource, List<Bar> additionalSource)
        {
            if (mainSource == null)
                return additionalSource;
            else if (additionalSource == null)
                return mainSource;

            var result = new List<Bar>(mainSource);
            foreach (var bar in additionalSource)
            {
                if (!result.Any(b => b.Date.Equals(bar.Date)))
                    result.Add(bar);
            }

            return result.OrderBy(b => b.Date).ToList();
        }


        public static List<Bar> AdjustBars(List<Bar> bars, Selection parameters, bool sortResult = false)
        {
            if (parameters == null || bars == null || bars.Count < 2)
                return bars;

            bars = AdjustRawBars(parameters.Timeframe, parameters.TimeFactor, bars);
            bars = AdjustFlatBars(bars, parameters);

            if (bars == null || bars.Count < 2)
                return bars;

            var useBarCount = parameters.From == DateTime.MinValue;
            if (useBarCount)
            {
                if (bars.Count > parameters.BarCount)
                    return bars.OrderBy(c => c.Date).Skip(bars.Count - parameters.BarCount).ToList();
            }
            else
            {
                return bars.Where(b => b.Date >= parameters.From && b.Date <= parameters.To)
                    .OrderBy(b => b.Date).ToList();
            }

            return sortResult ? bars.OrderBy(b => b.Date).ToList() : bars;
        }

        public static List<Bar> AdjustRawBars(Timeframe periodicity, int interval, List<Bar> bars)
        {
            if (bars == null || bars.Count == 1)
                return bars;

            var res = new List<Bar>();
            var orig = bars.ToList();
            orig.Reverse();

            if ((periodicity == Timeframe.Tick || periodicity == Timeframe.Minute
                || periodicity == Timeframe.Day) && interval == 1)
            {
                return bars.ToList();
            }

            if (periodicity == Timeframe.Tick)
            {
                res.Capacity = (int)Math.Ceiling(bars.Count / (float)interval);
                for (int i = 0; i < bars.Count;)
                {
                    res.Add(bars[i++]);
                    for (int c = 1; c < interval && i < bars.Count; c++, i++)
                        res[res.Count - 1].AppendBar(bars[i]);
                }
            }
            else if (periodicity == Timeframe.Minute)
            {
                while (orig.Count > 0 && orig.First().Date.Minute % interval != 0)
                    orig.RemoveAt(0);
                res = orig;
            }
            else if (periodicity == Timeframe.Hour)
            {
                var date = orig[0].Date;
                res.Add(new Bar(orig[0], new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0)));

                foreach (var bar in orig)
                {
                    var lastBar = res.Last();
                    if (lastBar.Date.Hour != bar.Date.Hour || lastBar.Date.Day != bar.Date.Day)
                        res.Add(new Bar(bar, new DateTime(bar.Date.Year, bar.Date.Month, bar.Date.Day, bar.Date.Hour, 0, 0)));
                    else
                        lastBar.AppendBar(bar);
                }
            }
            else if (periodicity == Timeframe.Month)
            {
                res.Add(new Bar(orig[0], new DateTime(orig[0].Date.Year, orig[0].Date.Month, 1, 0, 0, 0)));

                foreach (var bar in orig)
                {
                    var lastBar = res.Last();
                    if (lastBar.Date.Month != bar.Date.Month || lastBar.Date.Year != bar.Date.Year)
                        res.Add(new Bar(bar, new DateTime(bar.Date.Year, bar.Date.Month, 1, 0, 0, 0)));
                    else
                        lastBar.AppendBar(bar);
                }
            }
            else
            {
                res = orig;
            }

            if (res.Count < 2 || interval == 1 || periodicity == Timeframe.Tick)
            {
                res.Reverse();
                return res;
            }

            var tmp = new List<Bar>();
            var startDate = res.First().Date;

            while (startDate < res.Last().Date)
            {
                var endDate = startDate;

                switch (periodicity)
                {
                    case Timeframe.Minute:
                        endDate = startDate.AddMinutes(interval); break;
                    case Timeframe.Hour:
                        endDate = startDate.AddHours(interval); break;
                    case Timeframe.Day:
                        endDate = startDate.AddDays(interval); break;
                    case Timeframe.Month:
                        endDate = startDate.AddMonths(interval); break;
                }

                var selection = res.Where(p => p.Date >= startDate && p.Date < endDate).ToList();
                if (selection.Count == 0)
                {
                    startDate = endDate;
                    continue;
                }

                tmp.Add(new Bar
                {
                    Date = startDate,
                    OpenBid = selection[0].OpenBid,
                    OpenAsk = selection[0].OpenAsk,
                    CloseBid = selection.Last().CloseBid,
                    CloseAsk = selection.Last().CloseAsk,
                    HighBid = selection.Max(p => p.HighBid),
                    HighAsk = selection.Max(p => p.HighAsk),
                    LowBid = selection.Where(p => p.LowBid > 0M).Min(p => p.LowBid),
                    LowAsk = selection.Where(p => p.LowAsk > 0M).Min(p => p.LowAsk),
                    VolumeBid = selection.Sum(p => p.VolumeBid),
                    VolumeAsk = selection.Sum(p => p.VolumeAsk)
                });

                startDate = endDate;
            }

            tmp.Reverse();
            return tmp;
        }

        public static List<Bar> AdjustFlatBars(List<Bar> bars, Selection sel)
        {
            if (!sel.IncludeWeekendData.HasValue || bars == null || bars.Count < 2 || sel.Timeframe == Timeframe.Tick
                || sel.Timeframe > Timeframe.Day || (sel.Timeframe == Timeframe.Day && sel.TimeFactor > 1))
            {
                return bars;
            }

            //! bars are supposed to be sorted by date in ascending order
            bool usedDescOrder = bars[0].Date > bars[1].Date;
            if (usedDescOrder)
                bars = bars.OrderBy(b => b.Date).ToList();

            var ret = new List<Bar>(bars.Count);
            if (sel.IncludeWeekendData == false)
            {
                foreach (var b in bars)
                {
                    if ((b.Date.DayOfWeek == DayOfWeek.Friday && b.Date.TimeOfDay.TotalHours > 18)  //18:00 - approximate time
                        || b.Date.DayOfWeek == DayOfWeek.Saturday || b.Date.DayOfWeek == DayOfWeek.Sunday)
                    {
                        //optional TODO: compare current bar value with previous bar close

                        if (b.MeanOpen == b.MeanHigh && b.MeanOpen == b.MeanLow && b.MeanOpen == b.MeanClose)  //flat
                            continue;
                    }
                    ret.Add(b);
                }
            }
            else
            {
                Bar prevBar = bars[0];
                DateTime dt = GetIdealBarTime(prevBar.Date, sel.Timeframe, sel.TimeFactor);
                ret.Add(prevBar);
                for (int i = 1; i < bars.Count; i++)
                {
                    dt = GetNextBarStart(dt, sel.Timeframe, sel.TimeFactor);
                    var nextAvailTime = GetIdealBarTime(bars[i].Date, sel.Timeframe, sel.TimeFactor);
                    while (nextAvailTime > dt)
                    {
                        ret.Add(new Bar(dt, prevBar.CloseBid, prevBar.CloseAsk, prevBar.VolumeBid, prevBar.VolumeAsk));
                        dt = GetNextBarStart(dt, sel.Timeframe, sel.TimeFactor);
                    }

                    ret.Add(bars[i]);

                    prevBar = bars[i];
                    dt = GetIdealBarTime(prevBar.Date, sel.Timeframe, sel.TimeFactor);
                }
            }

            return usedDescOrder ? ret.OrderByDescending(i => i.Date).ToList() : ret;
        }

        private static DateTime GetNextBarStart(DateTime currentBarTime, Timeframe period, int interval)
        {
            if (interval < 1)
                interval = 1;

            switch (period)
            {
                case Timeframe.Minute: return currentBarTime.AddMinutes(interval);
                case Timeframe.Hour: return currentBarTime.AddHours(interval);
                case Timeframe.Day: return currentBarTime.AddDays(interval);
                case Timeframe.Month: return currentBarTime.AddMonths(interval);
                default:
                    throw new ArgumentException(period.ToString() + " periodicity is not supported", "period");
            }
        }

        public static bool IsNewBar(DateTime currentBarTime, Timeframe period, int interval, DateTime newTime)
            => newTime >= GetNextBarStart(currentBarTime, period, interval);
        

        public static DateTime GetIdealBarTime(DateTime time, Timeframe period, int interval)
        {
            int val = 0;

            switch (period)
            {
                case Timeframe.Minute:
                    val = time.Minute;
                    val -= val % interval;
                    return new DateTime(time.Year, time.Month, time.Day, time.Hour, val, 0, time.Kind);
                    
                case Timeframe.Hour:
                    val = time.Hour;
                    val -= val % interval;
                    return new DateTime(time.Year, time.Month, time.Day, val, 0, 0, time.Kind);

                case Timeframe.Day:
                    return time.Date;

                case Timeframe.Month:
                    return new DateTime(time.Year, time.Month, 1, 0, 0, 0, time.Kind);

                default:
                    throw new ArgumentException(period.ToString() + " periodicity is not supported", "period");
            }
        }

        public static string GetFileVersion(string file)
        {
            return File.Exists(file)
                ? System.Diagnostics.FileVersionInfo.GetVersionInfo(file).FileVersion
                : String.Empty;

            //try
            //{
            //    var assembly = Assembly.LoadFile(new FileInfo(file).FullName);
            //    if (assembly == null)
            //        return String.Empty;
            //    return assembly.FullName;
            //}
            //catch { return String.Empty; }
        }

        public static byte[] ReadFromFileAndCompress(string fileName)
        {
            if (!File.Exists(fileName))
                return new byte[0];

            var bytes = File.ReadAllBytes(fileName);
            return bytes.Length > 0 ? Compression.Compress(bytes) : bytes;
        }

        public static void UnzipContent(string path, byte[] data)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                if (data != null && data.Length > 0)
                {
                    using (var stream = new MemoryStream(data))
                    using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                        zip.ExtractToDirectory(path);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to unzip data into '{path}': {e.Message}");
            }
        } 

        public static string GetDirectoryName(string path)
        {
            char SEP = Path.DirectorySeparatorChar;
            if (path == null || !path.Contains(SEP))
                return path;

            //remove trailing backslash(es)
            while (path.Length > 0 && path[path.Length - 1] == SEP)
                path = path.Remove(path.Length - 1);
            if (!path.Contains(SEP))
                return path;

            var lastDir = path.Substring(path.LastIndexOf(SEP) + 1);

            if (lastDir.Contains('.'))  //optional: assume this is a file
                lastDir = path.Remove(path.LastIndexOf(SEP)).Substring(path.LastIndexOf(SEP) + 1);

            return lastDir;
        }

        public static TimeSpan PeriodicityToTimeSpan(Timeframe periodicity, int interval)
        {
            switch (periodicity)
            {
                case Timeframe.Day:
                    return new TimeSpan(1*interval, 0, 0, 0);
                case Timeframe.Hour:
                    return new TimeSpan(1*interval, 0, 0);
                case Timeframe.Minute:
                    return new TimeSpan(0, 1*interval, 0);
                case Timeframe.Month:
                    return new TimeSpan(30*interval, 0, 0, 0);
            }
            return new TimeSpan();
        }

        public static DateTime GetTimeRoundToMinute(DateTime time)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, time.Kind);
        }

        public static bool IsEmpty(this System.Collections.ICollection collection)
        {
            return collection == null || collection.Count == 0;
        }
    }
}
