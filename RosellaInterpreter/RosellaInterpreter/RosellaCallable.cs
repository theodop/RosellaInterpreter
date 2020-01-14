using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosellaInterpreter
{
    public interface RosellaCallable
    {
        int arity();
        object call(Interpreter interpreter, IList<object> arguments);
    }
}
