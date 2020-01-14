using System.Collections.Generic;
using static RosellaInterpreter.Interpreter;

namespace RosellaInterpreter
{
    public class RosellaEnvironment
    {
        private readonly IDictionary<string, object> values = new Dictionary<string, object>();

        public readonly RosellaEnvironment enclosing;

        public RosellaEnvironment(RosellaEnvironment enclosing = null)
        {
            this.enclosing = enclosing;
        }

        public object get(Token name)
        {
            if (values.ContainsKey(name.lexeme))
            {
                return values[name.lexeme];
            }

            if (enclosing != null) return enclosing.get(name);

            throw new RuntimeError(name, $"Undefined variable '{name.lexeme}'.");
        }

        public void define(string name, object value)
        {
            values[name] = value;
        }

        public object getAt(int distance, string name)
        {
            return ancestor(distance).values[name];
        }

        private RosellaEnvironment ancestor(int distance)
        {
            var environment = this;

            for(var i=0; i<distance; i++)
            {
                environment = environment.enclosing;
            }

            return environment;
        }

        public void assign(Token name, object value)
        {
            if (values.ContainsKey(name.lexeme))
            {
                values[name.lexeme] = value;
                return;
            }

            if (enclosing != null)
            {
                enclosing.assign(name, value);
                return;
            }

            throw new RuntimeError(name, $"Undefined variable '{name.lexeme}'.");
        }

        public void assignAt(int distance, Token name, object value)
        {
            ancestor(distance).values.Add(name.lexeme, value);
        }
    }
}
