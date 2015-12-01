/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System.Collections.Generic;

namespace Gibbed.JustCause3.PropertyFormats
{
    public class Node
    {
        private readonly Dictionary<uint, Node> _Children;
        private readonly Dictionary<uint, IVariant> _Properties;
        private readonly Dictionary<uint, string> _KnownNames;
        private string _Tag;

        public Node()
        {
            this._Children = new Dictionary<uint, Node>();
            this._Properties = new Dictionary<uint, IVariant>();
            this._KnownNames = new Dictionary<uint, string>();
        }

        public Dictionary<uint, Node> Children
        {
            get { return this._Children; }
        }

        public Dictionary<uint, IVariant> Properties
        {
            get { return this._Properties; }
        }

        public Dictionary<uint, string> KnownNames
        {
            get { return this._KnownNames; }
        }

        public string Tag
        {
            get { return this._Tag; }
            set { this._Tag = value; }
        }
    }
}
