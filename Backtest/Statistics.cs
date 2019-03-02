/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Backtest
{
    internal sealed class Statistics
    {

        #region Members

        private const float Jmonth = 30.000000002328f;
        private static readonly Random R = new Random((int)DateTime.Now.Ticks);

        public int AvgTradesPerMonth;

        #endregion

        #region Public Methods

        public List<double> ConvertToMonthly(List<double> trades, List<DateTime> dates)
        {
            //How many months are between the start and end date?
            var months = 1 + Math.Floor((dates.Last() - dates.First()).TotalDays / Jmonth);
            var monthly = new List<double>(new double[(int)months]);
            var size = Math.Min((int)months, dates.Count);
            AvgTradesPerMonth = 0;
            for (var n = 0; n < size; ++n)
            {
                // Which month does this trade fit into?
                var month = (dates[n] - dates[0]).TotalDays / Jmonth;
                if ((int)month < monthly.Count)
                {
                    monthly[(int)month] += trades[n];
                    AvgTradesPerMonth++;
                }
            }
            AvgTradesPerMonth = trades.Count / (int)months;
            return monthly;
        }

        #endregion

        #region Static Methods

        public static double ValueAddedMonthlyIndex(List<double> monthlyPL)
        {
            double returnValue = 1000;
            var size = monthlyPL.Count;
            for (var i = 0; i < size; i++)
            {
                returnValue += (1 + monthlyPL[i]) * returnValue;
            }

            return returnValue;
        }

        public static double CompoundAnnualizedROR(List<double> monthlyPL)
        {
            var size = monthlyPL.Count;
            if (size < 1) return 0;

            var returnValue = 1 + CompoundMonthlyROR(monthlyPL);
            returnValue = Math.Pow(returnValue, size) - 1;
            return returnValue;
        }

        public static double StandardDeviation(List<double> monthlyPL)
        {
            var size = monthlyPL.Count;
            if (size < 2) return 0;

            double returnValue = 0;
            double mean = 0;
            int i;
            for (i = 0; i < size; i++)
            {
                mean += monthlyPL[i];
            }

            for (i = 0; i < size; i++)
            {
                returnValue += Math.Pow(monthlyPL[i] - mean, 2);
            }

            returnValue /= size - 1;
            returnValue = Math.Pow(returnValue, 0.5);
            return returnValue;
        }

        public static double AnnualizedStandardDeviation(List<double> monthlyPL)
        {
            var size = monthlyPL.Count;
            var returnValue = StandardDeviation(monthlyPL) * Math.Pow(size, 0.5);
            return returnValue;
        }

        public static double DownsideDeviation(List<double> monthlyPL, double minimumAcceptanceReturn)
        {
            var size = monthlyPL.Count;
            double returnValue = 0;
            if (size < 1)
            {
                return 0;
            }

            for (var i = 0; i < size; i++)
            {
                if (monthlyPL[i] < minimumAcceptanceReturn)
                {
                    returnValue += Math.Pow(monthlyPL[i] - minimumAcceptanceReturn, 2);
                }
            }

            returnValue /= size;
            returnValue = Math.Pow(returnValue, 0.5);
            return returnValue;
        }

        public static double SharpeRatio(List<double> monthlyPL, double periodRiskFreeReturn)
        {
            var size = monthlyPL.Count;
            double returnValue = 0;
            if (size < 2) return 0;
            double mean = 0;
            for (var i = 0; i < size; i++)
                mean += monthlyPL[i];

            var standardDeviation = StandardDeviation(monthlyPL);
            if (standardDeviation > 0)
                returnValue = (mean - periodRiskFreeReturn) / standardDeviation;

            return returnValue;
        }

        public static double SortinoRatio(List<double> monthlyPL, double minimumAcceptanceReturn)
        {
            double returnValue = 0;

            var size = monthlyPL.Count;
            double ddmar = 0;
            for (var i = 0; i < size; i++)
            {
                if (monthlyPL[i] < minimumAcceptanceReturn)
                {
                    ddmar += Math.Pow(monthlyPL[i] - minimumAcceptanceReturn, 2);
                }
            }

            if (size > 0)
            {
                ddmar /= size;
                ddmar = Math.Pow(ddmar, 0.5);
            }

            if (ddmar > 0)
                returnValue = (CompoundMonthlyROR(monthlyPL) - minimumAcceptanceReturn) / ddmar;

            return returnValue;
        }

        public static double AnnualizedSortinoRatio(List<double> monthlyPL, double minimumAcceptanceReturn)
        {
            var size = monthlyPL.Count;
            var returnValue = SortinoRatio(monthlyPL, minimumAcceptanceReturn);
            returnValue *= Math.Pow(size, 0.5);
            return returnValue;
        }

        public static double MaximumDrawdown(List<double> values)
        {
            var size = values.Count;
            double mdd = 0;
            double sum = 0;
            for (var i = 0; i < size; i++)
            {
                sum += values[i];
                if (sum < mdd)
                {
                    mdd = sum;
                }
            }
            return mdd;
        }

        public static double MaximumDrawdownMonteCarlo(List<double> values)
        {
            const int shuffleCount = 10000;
            var maxDrawDowns = new List<double>(shuffleCount);
            for (var n = 0; n < shuffleCount; ++n)
            {
                var shuffled = values.ToList();
                ShuffleValues(ref shuffled);
                maxDrawDowns.Add(MaximumDrawdown(shuffled));
            }

            double min = 0;
            foreach (var drawDown in maxDrawDowns)
            {
                if (drawDown < min)
                    min = drawDown;
            }

            return min;
        }

        public static double CalmarRatio(List<double> monthlyPL)
        {
            var max = Math.Abs(MaximumDrawdown(monthlyPL));
            if (max == 0)
            {
                return 0;
            }

            var returnValue = CompoundAnnualizedROR(monthlyPL) / max;
            if (returnValue > 1000000000000000 || returnValue < -1000000000000000)
            {
                returnValue = 0;
            }

            return returnValue;
        }

        public static double SterlingRatio(List<double> monthlyPL, double minimumAcceptanceReturn)
        {
            var size = monthlyPL.Count;
            var monthlyROR = CompoundMonthlyROR(monthlyPL);
            double returnValue = 0;

            double ddmar = 0;
            for (var i = 0; i < size; i++)
            {
                if (monthlyPL[i] < minimumAcceptanceReturn)
                {
                    ddmar += Math.Pow(monthlyPL[i] - minimumAcceptanceReturn, 2);
                }
            }
            if (size > 0)
            {
                ddmar /= size;
                ddmar = Math.Pow(ddmar, 0.5);
            }

            if (ddmar > 0)
            {
                returnValue = (monthlyROR - minimumAcceptanceReturn) / ddmar;
            }

            return returnValue;
        }

        #endregion

        #region Private Methods

        private static void ShuffleValues(ref List<double> values)
        {
            var size = values.Count;
            for (var left = 0; left < size - 1; ++left)
            {
                var right = R.Next(size - left);
                if (left != right)
                {
                    //swap left and right values
                    var temp = values[left];
                    values[left] = values[right];
                    values[right] = temp;
                }
            }
        }

        private static double CompoundMonthlyROR(List<double> monthlyPL)
        {
            var size = monthlyPL.Count;
            if (size < 1)
            {
                return 0;
            }

            var returnValue = ValueAddedMonthlyIndex(monthlyPL) / 1000;
            returnValue = Math.Pow(returnValue, (1.0 / size)) - 1;
            return returnValue;
        }

        #endregion

    }
}
