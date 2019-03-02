/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.Collections.Generic;

namespace Com.Lmax.Api.Internal
{
    public class SaxContentHandler : ISaxContentHandler
    {
        private readonly Stack<Handler> _handlers = new Stack<Handler>();

        public SaxContentHandler(Handler rootHandler)
        {
            _handlers.Push(rootHandler);
        }

        public void StartElement(string tagName)
        {
            Handler handler = _handlers.Peek().GetHandler(tagName);
            handler.Reset(tagName);
            _handlers.Push(handler);            
        }

        public void Content(string value)
        {
            _handlers.Peek().Characters(value, 0, value.Length);
        }

        public void EndElement(string tagName)
        {
            _handlers.Pop().EndElement(tagName);
        }
    }
}
