/* 
This project is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/
Any copyright is dedicated to the NominalNimbus.
https://github.com/NominalNimbus 
*/

using CommonObjects;
using ServerCommonObjects;
using ServerCommonObjects.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Server
{

    internal sealed class HistoryDataMultiTimeframeCache : IHistoryDataMultiTimeframeCache
    {
        #region members

        private readonly Dictionary<string, Dictionary<string, HistoryBarCache>> _barStore;
        //private readonly Dictionary<string, List<Bar>> _tickStore;
        private readonly List<LastUse> _lastBarUseStore;
        private readonly List<LastUse> _lastTickUseStore;
        private readonly IDataCacheManager _dataCacheManager;

        private readonly object _locker;

        #endregion //members

        #region Constructor

        internal HistoryDataMultiTimeframeCache(IDataCacheManager dataCacheManager)
        {
            _dataCacheManager = dataCacheManager;
            _barStore = new Dictionary<string, Dictionary<string, HistoryBarCache>>();
            _lastBarUseStore = new List<LastUse>();
            //_tickStore = new Dictionary<string, List<Bar>>();
            _lastTickUseStore = new List<LastUse>();
            _locker = new object();

            var cleanupTimer = new Timer();
            cleanupTimer.Elapsed += CleanupTimerElapsed;
            cleanupTimer.Interval = 1000 * 60 * 10; // 10 min
            cleanupTimer.Enabled = true;
            cleanupTimer.Start();
        }

        #endregion //Constructor

        #region IHistoryDataMultiTimeframeCache members

        public bool CacheTickData { get; set; }

        public void GetHistory(IDataFeed dataFeed, Selection p, HistoryAnswerHandler callback)
        {
            if (p.From == DateTime.MinValue && p.BarCount < 1) //use bar count (and end date if specified)
            {
                callback(p, new List<Bar>());
                return;
            }

            var cached = GetFromtCache(p);
            if (cached.Count > 0 && p.IsEnoughData(cached))
            {
                cached = p.TrimBars(cached);
                callback(p, cached);
                return;
            }

            _dataCacheManager.GetHistory(p, dataFeed, (@params, data) =>
            {
                Add2Cache(p, data);
                cached = p.TrimBars(data);
                
                callback(p, cached);
            });
        }

        public void Add2Cache(Tick tick)
        {
            if (tick == null)
                return;

            lock (_locker)
            {
                //update bar
                var key = GetSymbolKey(tick, 0);
                if (_barStore.TryGetValue(key, out var symbolsHistory))
                {
                    UpdateBars(tick, symbolsHistory);
                }
                foreach (var level2 in tick.Level2)
                {
                    key = GetSymbolKey(tick, level2.DomLevel);
                    if (_barStore.TryGetValue(key, out symbolsHistory))
                    {
                        UpdateBars(tick, symbolsHistory);
                    }
                }
            }
        }
        
        public void Add2Cache(Selection selection, List<Bar> barList)
        {
            if (selection == null || barList == null)
                return;

            if (!CacheTickData && selection.Timeframe == Timeframe.Tick)
                return;

            lock (_locker)
            {
                if (selection.Timeframe == Timeframe.Tick)
                {
                    /*key = selection.DataFeed + selection.Symbol.ToLower() + selection.Level;
                    if (_tickStore.TryGetValue(key, out var ticks))
                    {
                        if (selection.BarCount > ticks.Count)
                        {
                            _barStore[key] = barList.ToList();
                            key2Update = key;
                        }
                    }
                    else
                    {
                        _tickStore.Add(key, barList);
                    }*/
                }
                else
                {
                    var symbolKey = GetSymbolKey(selection);
                    if(!_barStore.TryGetValue(symbolKey, out var symbolsHistory))
                    {
                        symbolsHistory = new Dictionary<string, HistoryBarCache>();
                        _barStore[symbolKey] = symbolsHistory;
                    }

                    var key = selection.GetKey();
                    if(symbolsHistory.TryGetValue(key, out var bars))
                    {
                        //combine bars
                        //need to improve
                        //symbolsHistory[key] = barList;
                        bars.Clear();
                        bars.AddRange(barList);
                    }
                    else
                    {
                        var historyBars = new HistoryBarCache
                        {
                            Timeframe = selection.Timeframe,
                            TimeFactor = selection.TimeFactor
                        };
                        historyBars.AddRange(barList);
                        symbolsHistory[key] = historyBars;
                    }
                    AddOrUpdateLastUse(_lastBarUseStore, key);
                }
                
            }
        }
        #endregion //IHistoryDataMultiTimeframeCache members

        #region Private Members

        private void UpdateBars(Tick tick, Dictionary<string, HistoryBarCache> symbolHistory)
        {
            foreach (var item in symbolHistory)
            {
                var lastBar = item.Value.LastOrDefault();
                if(lastBar == null || CommonHelper.IsNewBar(lastBar.Date, item.Value.Timeframe, item.Value.TimeFactor, tick.Date))
                {
                    var barDate = CommonHelper.GetIdealBarTime(tick.Date, item.Value.Timeframe, item.Value.TimeFactor);
                    item.Value.Add(new Bar(tick, barDate));
                }
                else
                {
                    lastBar.AppendTick(tick);
                }
            }
        }

        private List<Bar> GetFromtCache(Selection selection)
            => selection.Timeframe == Timeframe.Tick ? GetTicksCache(selection) : GetBarsCache(selection);

        private List<Bar> GetTicksCache(Selection selection)
        {
            //need to implement latter
            var bars = default(List<Bar>);
            /*lock (_locker)
            {
                var key = GetTickKey(selection);
                 _tickStore.TryGetValue(key, out bars);

                if (bars != null && selection.BarCount <= bars.Count)
                    AddOrUpdateLastUse(_lastTickUseStore, key);
            }*/

            return bars ?? new List<Bar>();
        }
        
        private List<Bar> GetBarsCache(Selection selection)
        {
            var bars = default(List<Bar>);
            lock (_locker)
            {
                var symbolKey = GetSymbolKey(selection);
                if (_barStore.TryGetValue(symbolKey, out var symbolBars))
                {

                    var key = selection.GetKey();
                    if (symbolBars.TryGetValue(key, out var barsCache))
                    {
                        bars = new List<Bar>(barsCache);
                        if (bars != null && selection.BarCount <= bars.Count)
                            AddOrUpdateLastUse(_lastBarUseStore, key);
                    }
                }
            }

            return bars ?? new List<Bar>();
        }

        private string GetSymbolKey(Selection selection) => selection.DataFeed + selection.Symbol.ToLower() + selection.Level;

        private string GetSymbolKey(Tick tick, int level) => tick.DataFeed + tick.Symbol.Symbol.ToLower() + level;

        private void AddOrUpdateLastUse(List<LastUse> useList, string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var item = useList.FirstOrDefault(x => x.Key == key);
            if (item == null)
                useList.Add(new LastUse(key, DateTime.UtcNow));
            else
                item.Time = DateTime.UtcNow;
        }
        
        #endregion // Private Members

        #region Event Handlers

        private void CleanupTimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_locker)
            {
                var timeInterval = new TimeSpan(0, 10, 0);
                for (int i = 0; i < _lastBarUseStore.Count;)
                {
                    var bar = _lastBarUseStore[i];
                    if (DateTime.UtcNow - bar.Time >= timeInterval)
                    {
                        _barStore.Remove(bar.Key);
                        _lastBarUseStore.RemoveAt(i);
                        continue;
                    }

                    i++;
                }
            }
        }

        #endregion // Event Handlers
    }
}
