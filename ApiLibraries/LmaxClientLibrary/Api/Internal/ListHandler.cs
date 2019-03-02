/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Com.Lmax.Api.Internal
{
    public class ListHandler : Handler
    {
        private readonly List<string> _contentList = new List<string>();
        private readonly StringBuilder _contentBuilder = new StringBuilder();

        public ListHandler(string tag) : base(tag)
        {
        }

        public override void Characters(string characterData, int start, int length)
        {
            string content = _contentBuilder.Append(characterData, start, length).ToString();
            _contentList.Add(content);
            _contentBuilder.Length = 0;
        }

        public List<TOutput> GetContentList<TOutput>(Converter<string, TOutput> converter)
        {
            return _contentList.ConvertAll(converter);
        }

        public void Clear()
        {
            _contentList.Clear();
        }
    }
}
