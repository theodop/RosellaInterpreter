using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosellaInterpreter
{
    public class RosellaFunction : RosellaCallable
    {
        private readonly Stmt.Function declaration;
        private readonly RosellaEnvironment closure;

        public RosellaFunction(Stmt.Function declaration, RosellaEnvironment closure)
        {
            this.declaration = declaration;
            this.closure = closure;
        }

        public int arity() => declaration.@params.Count;

        public object call(Interpreter interpreter, IList<object> arguments)
        {
            var environment = new RosellaEnvironment(closure);

            for (var i=0; i<declaration.@params.Count; i++)
            {
                environment.define(declaration.@params[i].lexeme, arguments[i]);
            }

            try
            {
                interpreter.executeBlock(declaration.body, environment);
            } 
            catch (Return returnValue)
            {
                return returnValue.value;
            }
            return null;
        }

        public override string ToString() => $"<fn {declaration.name.lexeme}>";
    }
}
