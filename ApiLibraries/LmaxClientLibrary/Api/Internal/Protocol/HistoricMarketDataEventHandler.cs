/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;

namespace Com.Lmax.Api.Internal.Protocol
{
    public class HistoricMarketDataEventHandler : DefaultHandler
    {
        private const string RootNodeName = "historicMarketData";
        private const string InstructionIdNodeName = "instructionId";
        public event OnHistoricMarketDataEvent HistoricMarketDataReceived;

        private readonly List<Uri> _urls;
        private readonly URLHandler _urlHandler;

        public HistoricMarketDataEventHandler()
            : base(RootNodeName)
        {
            _urls = new List<Uri>();
            _urlHandler = new URLHandler(_urls);
            AddHandler(InstructionIdNodeName);
            AddHandler(_urlHandler);
        }

        public override void EndElement(string endElement)
        {
            if (HistoricMarketDataReceived != null && RootNodeName.Equals(endElement))
            {
                TryGetValue(InstructionIdNodeName, out string instructionId);
                HistoricMarketDataReceived?.Invoke(instructionId, _urlHandler.GetFiles());
            }
        }

        public override void Reset(string element)
        {
            base.Reset(element);
            if (RootNodeName.Equals(element))
            {
                _urls.Clear();
            }
        }
    }

    internal class URLHandler : Handler
    {
        private const string RootNodeName = "url";
        
        private readonly List<Uri> _urls;

        public URLHandler(List<Uri> urls)
            : base(RootNodeName)
        {
            _urls = urls;
        }

        public override void EndElement (string endElement)
        {
            if (RootNodeName.Equals(endElement))
            {
                _urls.Add(new Uri(Content));
                
            }
        }

        public List<Uri> GetFiles()
        {
            return _urls;
        }
    }
}