using System;
using System.Collections.Generic;

namespace Fiddler.Controls.UserControls.TileView
{
    public class CacheItemEventArgs : EventArgs
    {
        public CacheItemEventArgs(List<int> indices)
        {
            Indices = indices;
        }

        public List<int> Indices { get; }

        public bool Success;
    }

}
