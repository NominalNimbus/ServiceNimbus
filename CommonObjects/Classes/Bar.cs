/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Runtime.Serialization;

namespace CommonObjects
{
    [DataContract]
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("${MeanClose} @ {Date.ToString(\"MMMd HH:mm:ss\"),nq}")]
    public class Bar
    {
        [DataMember]
        public DateTime Date { get; set; }
        
        [DataMember]
        public decimal OpenBid { get; set; }

        [DataMember]
        public decimal OpenAsk { get; set; }

        public decimal MeanOpen => OpenBid == 0M ? OpenAsk : (OpenAsk == 0M ? OpenBid : ((OpenBid + OpenAsk) / 2M));

        [DataMember]
        public decimal HighBid { get; set; }

        [DataMember]
        public decimal HighAsk { get; set; }

        public decimal MeanHigh => HighBid == 0M ? HighAsk : (HighAsk == 0M ? HighBid : ((HighBid + HighAsk) / 2M));

        [DataMember]
        public decimal LowBid { get; set; }

        [DataMember]
        public decimal LowAsk { get; set; }

        public decimal MeanLow => LowBid == 0M ? LowAsk : (LowAsk == 0M ? LowBid : ((LowBid + LowAsk) / 2M));

        [DataMember]
        public decimal CloseBid { get; set; }

        [DataMember]
        public decimal CloseAsk { get; set; }

        public decimal MeanClose => CloseBid == 0M ? CloseAsk : (CloseAsk == 0M ? CloseBid : ((CloseBid + CloseAsk) / 2M));

        [DataMember]
        public decimal VolumeBid { get; set; }

        [DataMember]
        public decimal VolumeAsk { get; set; }

        public decimal MeanVolume => VolumeBid == 0M ? VolumeAsk : (VolumeAsk == 0M ? VolumeBid : ((VolumeBid + VolumeAsk) / 2M));

        public Bar()
        {
        }

        public Bar(Tick tick, DateTime date) : this(tick)
        {
            this.Date = date;
        }

        public Bar(Tick tick)
        {
            if (tick == null)
                throw new ArgumentNullException($"{nameof(tick)} is null");

           Date = tick.Date;
           OpenBid = tick.Bid;
           OpenAsk = tick.Ask;
           HighBid = tick.Bid;
           HighAsk = tick.Ask;
           LowBid = tick.Bid;
           LowAsk = tick.Ask;
           CloseBid = tick.Bid;
           CloseAsk = tick.Ask;
           VolumeBid = tick.BidSize;
           VolumeAsk = tick.AskSize;
        }

        public Bar(DateTime date, decimal open, decimal high, decimal low, decimal close, decimal volume)
        {
            this.Date = date;
            this.OpenBid = open;
            this.OpenAsk = open;
            this.HighBid = high;
            this.HighAsk = high;
            this.LowBid = low;
            this.LowAsk = low;
            this.CloseBid = close;
            this.CloseAsk = close;
            this.VolumeBid = volume;
            this.VolumeAsk = volume;
        }

        public Bar(DateTime date, decimal bid, decimal ask, decimal bidSize, decimal askSize)
        {
            this.Date = date;
            this.OpenBid = bid;
            this.OpenAsk = ask;
            this.HighBid = bid;
            this.HighAsk = ask;
            this.LowBid = bid;
            this.LowAsk = ask;
            this.CloseBid = bid;
            this.CloseAsk = ask;
            this.VolumeBid = bidSize;
            this.VolumeAsk = askSize;
        }

        public Bar(Bar bar)
        {
            this.Date = bar.Date;
            this.OpenBid = bar.OpenBid;
            this.OpenAsk = bar.OpenAsk;
            this.HighBid = bar.HighBid;
            this.HighAsk = bar.HighAsk;
            this.LowBid = bar.LowBid;
            this.LowAsk = bar.LowAsk;
            this.CloseBid = bar.CloseBid;
            this.CloseAsk = bar.CloseAsk;
            this.VolumeBid = bar.VolumeBid;
            this.VolumeAsk = bar.VolumeAsk;
        }

        public Bar(Bar bar, DateTime date) : this(bar)
        {
            this.Date = date;
        }

        public void AppendBar(Bar bar)
        {
            this.CloseBid = bar.CloseBid;
            this.CloseAsk = bar.CloseAsk;
            this.VolumeBid += bar.VolumeBid;
            this.VolumeAsk += bar.VolumeAsk;
            if (this.HighBid < bar.HighBid)
                this.HighBid = bar.HighBid;
            if (this.HighAsk < bar.HighAsk)
                this.HighAsk = bar.HighAsk;
            if (this.LowBid > bar.LowBid && bar.LowBid > 0M)
                this.LowBid = bar.LowBid;
            if (this.LowAsk > bar.LowAsk && bar.LowAsk > 0M)
                this.LowAsk = bar.LowAsk;
        }

        public void AppendTick(Tick tick)
        {
            if (tick == null)
                return;

           CloseBid = tick.Bid;
           CloseAsk = tick.Ask;
           VolumeBid += tick.BidSize;
           VolumeAsk += tick.AskSize;
            if (HighBid < tick.Bid)
                HighBid = tick.Bid;
            if (HighAsk < tick.Ask)
                HighAsk = tick.Ask;
            if (LowBid > tick.Bid && tick.Bid > decimal.Zero)
                LowBid = tick.Bid;
            if (LowAsk > tick.Ask && tick.Ask > decimal.Zero)
                LowAsk = tick.Ask;
        }

        public void AppendTick(decimal bid, decimal ask, decimal bidSize, decimal askSize)
        {
            this.CloseBid = bid;
            this.CloseAsk = ask;
            this.VolumeBid += bidSize;
            this.VolumeAsk += askSize;
            if (this.HighBid < bid)
                this.HighBid = bid;
            if (this.HighAsk < ask)
                this.HighAsk = ask;
            if (this.LowBid > bid && bid > decimal.Zero)
                this.LowBid = bid;
            if (this.LowAsk > ask && ask > decimal.Zero)
                this.LowAsk = ask;
        }
    }

    public class BarWithInstrument : Bar
    {
        public string Symbol { get; private set; }
        public string DataFeed { get; private set; }

        public BarWithInstrument() : base()
        {
        }

        public BarWithInstrument(Bar bar, string symbol, string dataFeed) : base(bar)
        {
            Symbol = symbol;
            DataFeed = dataFeed;
        }

        public BarWithInstrument(Bar bar, DateTime timestamp, string symbol, string dataFeed) : base(bar, timestamp)
        {
            Symbol = symbol;
            DataFeed = dataFeed;
        }
    }
}