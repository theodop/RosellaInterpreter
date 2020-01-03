using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RosellaInterpreter.Interpreter;

namespace RosellaInterpreter
{
    public class RosellaEnvironment
    {
        private readonly IDictionary<string, object> values = new Dictionary<string, object>();

        public object get(Token name)
        {
            if (values.ContainsKey(name.lexeme))
            {
                return values[name.lexeme];
            }

            throw new RuntimeError(name, $"Undefined variable '{name.lexeme}'.");
        }

        public void define(string name, object value)
        {
            values[name] = value;
        }

        public void assign(Token name, object value)
        {
            if (values.ContainsKey(name.lexeme))
            {
                values[name.lexeme] = value;
                return;
            }

            throw new RuntimeError(name, $"Undefined variable '{name.lexeme}'.");
        }
    }
}
