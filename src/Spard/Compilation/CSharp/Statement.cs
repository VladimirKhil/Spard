using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Compilation.CSharp
{
    public abstract class Statement
    {
        public abstract string ToString(int indent);
    }
}
