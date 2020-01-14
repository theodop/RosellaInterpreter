using System;
using System.Collections.Generic;
using System.Linq;
using static RosellaInterpreter.TokenType;

namespace RosellaInterpreter
{
    public class Interpreter : Expr.Visitor<object>, Stmt.Visitor<object>
    {
        private Action<RuntimeError> errorFunc;

        public RosellaEnvironment globals = new RosellaEnvironment();
        private readonly IDictionary<Expr, int> locals = new Dictionary<Expr, int>();
        private RosellaEnvironment environment;

        public class Clock : RosellaCallable
        {
            public int arity() => 0;
            public object call(Interpreter interpreter, IList<object> arguments)
            {
                return (double)DateTime.Now.ToUniversalTime().Subtract(
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    ).TotalMilliseconds;
            }

            public override string ToString() => "<native fn>";
        }

        public class RuntimeError : Exception
        {
            public readonly Token token;

            public RuntimeError(Token token, string message) : base(message)
            {
                this.token = token;
            }
        }

        public Interpreter(Action<RuntimeError> errorFunc)
        {
            this.errorFunc = errorFunc;
            environment = globals;
            globals.define("clock", new Clock());
        }

        public void interpret(IList<Stmt> statements)
        {
            try
            {
                foreach (var statement in statements)
                {
                    execute(statement);
                }
            }
            catch (RuntimeError error)
            {
                errorFunc(error);
            }
        }

        public object visitLiteralExpr(Expr.Literal expr)
        {
            return expr.value;
        }

        public object visitLogicalExpr(Expr.Logical expr)
        {
            var left = evaluate(expr.left);

            if (expr.@operator.type == TokenType.OR)
            {
                if (isTruthy(left)) return left;
            }
            else
            {
                if (!isTruthy(left)) return left;
            }

            return evaluate(expr.right);
        }

        public object visitGroupingExpr(Expr.Grouping expr)
        {
            return evaluate(expr.expression);
        }

        public object visitBinaryExpr(Expr.Binary expr)
        {
            var left = evaluate(expr.left);
            var right = evaluate(expr.right);

            switch (expr.@operator.type)
            {
                case GREATER:
                    checkNumberOperand(expr.@operator, left, right);
                    return (double)left > (double)right;
                case GREATER_EQUAL:
                    checkNumberOperand(expr.@operator, left, right);
                    return (double)left >= (double)right;
                case LESS:
                    checkNumberOperand(expr.@operator, left, right);
                    return (double)left < (double)right;
                case LESS_EQUAL:
                    checkNumberOperand(expr.@operator, left, right);
                    return (double)left <= (double)right;
                case MINUS:
                    checkNumberOperand(expr.@operator, left, right);
                    return (double)left - (double)right;
                case BANG_EQUAL: return !isEqual(left, right);
                case EQUAL_EQUAL: return isEqual(left, right);
                case PLUS:
                    if (left is double leftDouble && right is double rightDouble)
                    {
                        return leftDouble + rightDouble;
                    }

                    if (left is string leftString && right is string rightString)
                    {
                        return leftString + rightString;
                    }

                    throw new RuntimeError(expr.@operator,
                        "Operands must be two numbers or two strings.");
                case SLASH:
                    checkNumberOperand(expr.@operator, left, right);
                    return (double)left / (double)right;
                case STAR:
                    checkNumberOperand(expr.@operator, left, right);
                    return (double)left * (double)right;
            }

            return null;
        }

        public object visitCallExpr(Expr.Call expr)
        {
            var callee = evaluate(expr.callee);

            var arguments = new LinkedList<object>();

            foreach (var argument in expr.arguments)
            {
                arguments.AddLast(evaluate(argument));
            }

            if (callee is RosellaCallable function)
            {
                if (arguments.Count != function.arity())
                {
                    throw new RuntimeError(expr.paren, $"Expected {function.arity()} arguments but got {arguments.Count}.");
                }

                return function.call(this, arguments.ToList());
            }

            throw new RuntimeError(expr.paren, "Can only call functions and classes");
        }

        public object evaluate(Expr expr)
        {
            return expr.accept(this);
        }

        public void execute(Stmt stmt)
        {
            stmt.accept(this);
        }

        public void resolve(Expr expr, int depth)
        {
            locals.Add(expr, depth);
        }

        public void executeBlock(IList<Stmt> statements, RosellaEnvironment environment)
        {
            var previous = this.environment;

            try
            {
                this.environment = environment;

                foreach (var statement in statements)
                {
                    execute(statement);
                }
            }
            finally
            {
                this.environment = previous;
            }
        }

        public object visitBlockStmt(Stmt.Block stmt)
        {
            executeBlock(stmt.statements, new RosellaEnvironment(environment));
            return null;
        }

        public object visitExpressionStmt(Stmt.Expression stmt)
        {
            evaluate(stmt.expression);
            return null;
        }

        public object visitFunctionStmt(Stmt.Function stmt)
        {
            var function = new RosellaFunction(stmt, environment);
            environment.define(stmt.name.lexeme, function);
            return null;
        }

        public object visitIfStmt(Stmt.If stmt)
        {
            if (isTruthy(evaluate(stmt.condition)))
            {
                execute(stmt.thenBranch);
            } 
            else if (stmt.elseBranch != null)
            {
                execute(stmt.elseBranch);
            }
            return null;
        }

        public object visitPrintStmt(Stmt.Print stmt)
        {
            var value = evaluate(stmt.expression);
            Console.WriteLine(stringify(value));
            return null;
        }

        public object visitReturnStmt(Stmt.Return stmt)
        {
            object value = null;

            if (stmt.value != null) value = evaluate(stmt.value);

            throw new Return(value);
        }

        public object visitVarStmt(Stmt.Var stmt)
        {
            object value = null;

            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer);
            }

            environment.define(stmt.name.lexeme, value);
            return null;
        }

        public object visitWhileStmt(Stmt.While stmt)
        {
            while (isTruthy(evaluate(stmt.condition)))
            {
                execute(stmt.body);
            }

            return null;
        }

        public object visitAssignExpr(Expr.Assign expr)
        {
            var value = evaluate(expr.value);

            if (locals.ContainsKey(expr))
            {
                environment.assignAt(locals[expr], expr.name, value);
            } 
            else
            {
                globals.assign(expr.name, value);
            }
            
            return value;
        }

        public object visitUnaryExpr(Expr.Unary expr)
        {
            var right = evaluate(expr.right);

            switch (expr.@operator.type)
            {
                case BANG:
                    return !isTruthy(right);
                case MINUS:
                    checkNumberOperand(expr.@operator, right);
                    return -(double)right;
            }

            return null;
        }

        public object visitVariableExpr(Expr.Variable expr)
        {
            return lookupVariable(expr.name, expr);
        }

        private object lookupVariable(Token name, Expr expr)
        {
            if (locals.ContainsKey(expr))
            {
                return environment.getAt(locals[expr], name.lexeme);
            }

            return globals.get(name);
        }

        private void checkNumberOperand(Token @operator, object operand)
        {
            if (operand is double) return;
            throw new RuntimeError(@operator, "Operand must be a number.");
        }

        private void checkNumberOperand(Token @operator, object left, object right)
        {
            if (left is double && right is double) return;
            throw new RuntimeError(@operator, "Operands must be numbers");
        }

        private bool isTruthy(object @object)
        {
            if (@object == null) return false;
            if (@object is bool boolObject) return boolObject;
            return true;
        }

        private bool isEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null) return false;

            return a.Equals(b);
        }

        private string stringify(object obj)
        {
            if (obj == null) return "nil";

            if (obj is double)
            {
                var text = obj.ToString();
                if (text.EndsWith(".0"))
                {
                    text = text.JavaSubstring(0, text.Length - 2);
                }
                return text;
            }

            if (obj is bool) return obj.ToString().ToLower();

            return obj.ToString();
        }
    }
}
