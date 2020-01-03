using System;
using System.Collections.Generic;
using static RosellaInterpreter.TokenType;

namespace RosellaInterpreter
{
    public class Interpreter : Expr.Visitor<object>, Stmt.Visitor<object>
    {
        private Action<RuntimeError> errorFunc;

        private RosellaEnvironment environment = new RosellaEnvironment();

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

        public object evaluate(Expr expr)
        {
            return expr.accept(this);
        }

        public void execute(Stmt stmt)
        {
            stmt.accept(this);
        }

        public object visitExpressionStmt(Stmt.Expression stmt)
        {
            evaluate(stmt.expression);
            return null;
        }

        public object visitPrintStmt(Stmt.Print stmt)
        {
            var value = evaluate(stmt.expression);
            Console.WriteLine(stringify(value));
            return null;
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

        public object visitAssignExpr(Expr.Assign expr)
        {
            var value = evaluate(expr.value);

            environment.assign(expr.name, value);
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
            return environment.get(expr.name);
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

            return obj.ToString();
        }
    }
}
