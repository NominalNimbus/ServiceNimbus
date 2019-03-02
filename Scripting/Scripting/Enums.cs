/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

namespace Scripting
{
    public enum PriceConstants : byte
    {
        CLOSE = 0,
        OPEN,
        HIGH,
        LOW,
        MEDIAN,   // (high+low)/2
        TYPICAL,  // (high+low+close)/3
        WEIGHTED,  // (high+low+close+close)/4
        OHLC,
        OLHC
    }

    public enum MovingAverageType : byte
    {
        SMA = 0,  // simple moving average
        EMA,      // exponential moving average
        SSMA,     // smoothed moving average
        LWMA      // linear weighted moving average
    }
}
