using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosellaInterpreter
{
    public class Resolver : Expr.Visitor<object>, Stmt.Visitor<object>
    {
        private enum FunctionType
        {
            NONE,
            FUNCTION
        }

        private readonly Interpreter interpreter;
        private readonly Action<Token, string> errorFunc;
        private readonly Stack<IDictionary<string, bool>> scopes = new Stack<IDictionary<string, bool>>();
        private FunctionType currentFunction = FunctionType.NONE;
        public Resolver(Interpreter interpreter, Action<Token, string> errorFunc)
        {
            this.interpreter = interpreter;
            this.errorFunc = errorFunc;
        }

        public void resolve(IList<Stmt> statements)
        {
            foreach (var statement in statements)
            {
                resolve(statement);
            }
        }

        private void resolve(Stmt stmt)
        {
            stmt.accept(this);
        }

        private void resolve(Expr expr)
        {
            expr.accept(this);
        }

        private void resolveFunction(Stmt.Function function, FunctionType type)
        {
            var enclosingFunction = currentFunction;
            currentFunction = type;

            beginScope();
            foreach (var param in function.@params)
            {
                declare(param);
                define(param);
            }
            resolve(function.body);
            endScope();
            currentFunction = enclosingFunction;
        }

        private void beginScope()
        {
            scopes.Push(new Dictionary<string, bool>());
        }

        private void endScope()
        {
            scopes.Pop();
        }

        private void declare(Token name)
        {
            if (!scopes.Any()) return;
            var scope = scopes.Peek();
            if (scope.ContainsKey(name.lexeme))
            {
                errorFunc(name, "Variable with this name already declared in this scope.");
            }

            scope.Add(name.lexeme, false);
        }

        private void define(Token name)
        {
            if (!scopes.Any()) return;
            scopes.Peek()[name.lexeme] = true;
        }

        private void resolveLocal(Expr expr, Token name)
        {
            var i = scopes.Count - 1;
            foreach (var scope in scopes)
            {
                if (scope.ContainsKey(name.lexeme))
                {
                    interpreter.resolve(expr, scopes.Count - 1 - i);
                    return;
                }
                i--;
            }
        }

        public object visitBlockStmt(Stmt.Block stmt)
        {
            beginScope();
            resolve(stmt.statements);
            endScope();
            return null;
        }

        public object visitExpressionStmt(Stmt.Expression stmt)
        {
            resolve(stmt.expression);
            return null;
        }

        public object visitVarStmt(Stmt.Var stmt)
        {
            declare(stmt.name);
            if (stmt.initializer != null)
            {
                resolve(stmt.initializer);
            }
            define(stmt.name);
            return null;
        }

        public object visitVariableExpr(Expr.Variable expr)
        {
            if (scopes.Any() && scopes.Peek()[expr.name.lexeme] == false)
            {
                errorFunc(expr.name, "Cannot read local variable in its own initializer.");
            }

            resolveLocal(expr, expr.name);
            return null;
        }

        public object visitAssignExpr(Expr.Assign expr)
        {
            resolve(expr.value);
            resolveLocal(expr, expr.name);
            return null;
        }

        public object visitBinaryExpr(Expr.Binary expr)
        {
            resolve(expr.left);
            resolve(expr.right);
            return null;
        }

        public object visitCallExpr(Expr.Call expr)
        {
            resolve(expr.callee);

            foreach (var argument in expr.arguments)
            {
                resolve(argument);
            }

            return null;
        }

        public object visitGroupingExpr(Expr.Grouping expr)
        {
            resolve(expr.expression);
            return null;
        }

        public object visitLiteralExpr(Expr.Literal expr) => null;

        public object visitLogicalExpr(Expr.Logical expr)
        {
            resolve(expr.left);
            resolve(expr.right);
            return null;
        }

        public object visitUnaryExpr(Expr.Unary expr)
        {
            resolve(expr.right);
            return null;
        }

        public object visitFunctionStmt(Stmt.Function stmt)
        {
            declare(stmt.name);
            define(stmt.name);

            resolveFunction(stmt, FunctionType.FUNCTION);
            return null;
        }

        public object visitIfStmt(Stmt.If stmt)
        {
            resolve(stmt.condition);
            resolve(stmt.thenBranch);
            if (stmt.elseBranch != null) resolve(stmt.elseBranch);
            return null;
        }

        public object visitPrintStmt(Stmt.Print stmt)
        {
            resolve(stmt.expression);
            return null;
        }

        public object visitReturnStmt(Stmt.Return stmt)
        {
            if (currentFunction == FunctionType.NONE)
            {
                errorFunc(stmt.keyword, "Cannot return from top-level code.");
            }
            if (stmt.value != null) resolve(stmt.value);
            return null;
        }

        public object visitWhileStmt(Stmt.While stmt)
        {
            resolve(stmt.condition);
            resolve(stmt.body);
            return null;
        }
    }
}
