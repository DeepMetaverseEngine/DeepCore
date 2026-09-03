using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Slave.Layer
{
    public interface IEnvironmentObject
    {
        object GetEnvironmentVar(string key);

        IEnumerable<string> ListEnvironmentVars();
        IEnumerable<KeyValuePair<string,object>> ListEnvironmentValues();
    }
}
