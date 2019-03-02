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
using System.Runtime.Remoting.Lifetime;
using CommonObjects;

namespace Scripting
{
    public abstract partial class IndicatorBase : MarshalByRefObject, IScripting
    {
        protected const double EMPTY_VALUE = 0x7FFFFFFF;

        private readonly object _locker = new object();
        private readonly List<string> _alerts = new List<string>();
        private readonly List<ScriptingParameterBase> _origParameters = new List<ScriptingParameterBase>();

        public string ID { get; private set; }

        public string Name { get; protected set; }

        public string Owner { get; set; }

        /// <summary>
        /// Determinate Start function call (on new tick or new bar)
        /// </summary>
        public bool StartOnNewBar { get; set; }

        /// <summary>
        /// List of indicator series
        /// </summary>
        public List<Series> Series { get; protected set; }

        /// <summary>
        /// Is the series on same panel with chart
        /// </summary>
        /// 
        public bool IsOverlay { get; protected set; }

        public PriceType PriceType { get; set; }

        public List<ScriptingParameterBase> OrigParameters
        {
            get { return _origParameters.Select(p => p.Clone() as ScriptingParameterBase).ToList(); }
        }

        /// <summary>
        /// Indicator display name
        /// </summary>
        public string DisplayName { get; protected set; }
        
        /// <summary>
        /// Base constructor. Use for Series and parameter
        /// </summary>
        protected IndicatorBase()
        {
            ID = Guid.NewGuid().ToString("N");
            Series = new List<Series>();
            Name = String.Empty;
            DisplayName = String.Empty;
        }

        /// <summary>
        /// Initialize scripting inner parameters
        /// </summary>
        /// <param name="selection">Data description on which  code will be run</param>
        /// <param name="dataProvider">Provides access to historical and real time data</param>
        /// <returns>True if succeeded</returns>
        protected abstract bool InternalInit(Selection selection, IDataProvider dataProvider);

        /// <summary>
        /// Calculate function. Called after new tick or new bar arrived (dependent for StartOnNewBar property)
        /// </summary>
        /// <param name="bars">Historical data to run calculation on (optional)</param>
        /// <returns>Number of values added/updated</returns>
        /// <remarks>If bars collection is not provided indicator will request necessary data</remarks>
        protected abstract int InternalCalculate(IEnumerable<Bar> bars = null);
        
        /// <summary>
        /// Get list of parameters for configuration on client side
        /// </summary>
        protected abstract List<ScriptingParameterBase> InternalGetParameters();
        
        /// <summary>
        /// Apply parameters configured on client side
        /// </summary>
        /// <param name="parameterBases">List of configured parameters</param>
        /// <returns>True if case of succeeded configuration</returns>
        protected abstract bool InternalSetParameters(List<ScriptingParameterBase> parameterBases);
        
        /// <summary>
        /// Call scripting inner parameters Initialization using cross-thread lock's 
        /// </summary>
        /// <param name="selection">Data description on which  code will be run</param>
        /// <param name="dataProvider">Object which provide access to historical and real time data</param>
        /// <returns>True if case of succeeded initialization</returns>
        public bool Init(Selection selection, IDataProvider dataProvider)
        {
            lock (_locker)
            {
                try
                {
                    return InternalInit(selection, dataProvider);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Call calculate function  using cross-thread lock's
        /// </summary>
        /// <returns>Working result. True in case of series changed</returns>
        public int Calculate(IEnumerable<Bar> bars = null)
        {
            lock (_locker)
            {
                try
                {
                    return InternalCalculate(bars);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Show alert with message
        /// </summary>
        public void Alert(string message)
        {
            lock (_alerts)
                _alerts.Add(message);
        }

        /// <summary>
        /// Return list of non showed alerts. Calling of this function clear list of alerts.
        /// </summary>
        public List<string> GetActualAlerts()
        {
            lock (_alerts)
            {
                var res = _alerts.ToList();
                _alerts.Clear();
                return res;
            }
        }

        /// <summary>
        /// Get parameters for settings on client side using cross-thread lock's 
        /// </summary>
        /// <returns>List of parameters</returns>
        public List<ScriptingParameterBase> GetParameters()
        {
            lock (_locker)
            {
                try
                {
                    return InternalGetParameters();
                }
                catch (Exception)
                {
                    return new List<ScriptingParameterBase>();
                }
            }
        }

        /// <summary>
        /// Set code parameters that configured on client side using cross-thread lock's 
        /// </summary>
        /// <param name="parameterBases">List of parameters</param>
        /// <returns>False in case of invalid parameters</returns>
        public bool SetParameters(List<ScriptingParameterBase> parameterBases)
        {
            lock (_locker)
            {
                try
                {
                    _origParameters.Clear();
                    _origParameters.AddRange(parameterBases.Select(p => p.Clone() as ScriptingParameterBase));
                    return InternalSetParameters(parameterBases);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public override Object InitializeLifetimeService()
        {
            var lease = (ILease)base.InitializeLifetimeService();
            if (lease != null && lease.CurrentState == LeaseState.Initial)
                lease.InitialLeaseTime = TimeSpan.FromDays(256);
            
            return lease;
        }
    }
}
