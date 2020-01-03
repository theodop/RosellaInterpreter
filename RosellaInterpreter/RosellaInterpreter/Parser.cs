using System;
using System.Collections.Generic;
using System.Linq;
using static RosellaInterpreter.TokenType;

namespace RosellaInterpreter
{
    public class Parser
    {
        private class ParseError : Exception { };

        private readonly IList<Token> tokens;
        private readonly Action<Token, string> errorFunc;
        private int current = 0;

        public Parser(IList<Token> tokens, Action<Token, string> errorFunc)
        {
            this.tokens = tokens;
            this.errorFunc = errorFunc;
        }

        public IList<Stmt> parse()
        {
            var statements = new LinkedList<Stmt>();
            while (!isAtEnd())
            {
                statements.AddLast(declaration());
            }
            return statements.ToArray();
        }

        private Stmt declaration()
        {
            try
            {
                if (match(VAR)) return varDeclaration();

                return statement();
            } 
            catch (ParseError)
            {
                synchronize();
                return null;
            }
        }

        private Stmt statement()
        {
            if (match(PRINT)) return printStatement();

            return expressionStatement();
        }

        private Stmt printStatement()
        {
            var value = expression();
            consume(SEMICOLON, "Expect ';' after value.");
            return new Stmt.Print(value);
        }

        private Stmt varDeclaration()
        {
            var name = consume(IDENTIFIER, "Expect variable name.");

            Expr initializer = null;

            if (match(EQUAL))
            {
                initializer = expression();
            }

            consume(SEMICOLON, "Expect ';' after variable declaration");
            return new Stmt.Var(name, initializer);
        }

        private Stmt expressionStatement()
        {
            var expr = expression();
            consume(SEMICOLON, "Expect ';' after expression.");
            return new Stmt.Expression(expr);
        }

        private Expr expression()
        {
            return assignment();
        }

        private Expr assignment()
        {
            var expr = equality();

            if (match(EQUAL))
            {
                var equals = previous();
                var value = assignment();

                if (expr is Expr.Variable var)
                {
                    var name = var.name;
                    return new Expr.Assign(name, value);
                }

                error(equals, "Invalid assignment target.");
            }

            return expr;
        }

        private Expr equality()
        {
            var expr = comparison();

            while (match(BANG_EQUAL, EQUAL_EQUAL))
            {
                var @operator = previous();
                var right = comparison();
                expr = new Expr.Binary(expr, @operator, right);
            }

            return expr;
        }

        private Expr comparison()
        {
            var expr = addition();

            while (match (GREATER, GREATER_EQUAL, LESS, LESS_EQUAL))
            {
                var @operator = previous();
                var right = addition();
                expr = new Expr.Binary(expr, @operator, right);
            }

            return expr;
        }

        private Expr addition()
        {
            var expr = multiplication();

            while (match(MINUS, PLUS))
            {
                var @operator = previous();
                var right = multiplication();
                expr = new Expr.Binary(expr, @operator, right);
            }

            return expr;
        }

        private Expr multiplication()
        {
            var expr = unary();

            while (match(SLASH, STAR))
            {
                var @operator = previous();
                var right = unary();
                expr = new Expr.Binary(expr, @operator, right);
            }

            return expr;
        }

        private Expr unary()
        {
            if (match(BANG,MINUS))
            {
                var @operator = previous();
                var right = unary();
                return new Expr.Unary(@operator, right);
            }

            return primary();
        }

        private Expr primary()
        {
            if (match(FALSE)) return new Expr.Literal(false);
            if (match(TRUE)) return new Expr.Literal(true);
            if (match(NIL)) return new Expr.Literal(null);

            if (match(NUMBER, STRING))
            {
                return new Expr.Literal(previous().literal);
            }

            if (match(IDENTIFIER))
            {
                return new Expr.Variable(previous());
            }

            if (match(LEFT_PAREN))
            {
                var expr = expression();
                consume(RIGHT_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr);
            }

            throw error(peek(), "Expect expression.");
        }

        private bool match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (check(type))
                {
                    advance();
                    return true;
                }
            }

            return false;
        }

        private Token consume(TokenType type, string message)
        {
            if (check(type)) return advance();

            throw error(peek(), message);
        }

        private ParseError error(Token token, string message)
        {
            errorFunc(token, message);
            return new ParseError();
        }

        private void synchronize()
        {
            advance();

            while (!isAtEnd())
            {
                if (previous().type == SEMICOLON) return;

                switch (peek().type)
                {
                    case CLASS:
                    case FUN:
                    case VAR:
                    case FOR:
                    case IF:
                    case WHILE:
                    case PRINT:
                    case RETURN:
                        return;
                }
            }

            advance();
        }

        private bool check(TokenType type)
        {
            if (isAtEnd()) return false;
            return peek().type == type;
        }

        private Token advance()
        {
            if (!isAtEnd()) current++;
            return previous();
        }

        private bool isAtEnd()
        {
            return peek().type == EOF;
        }

        private Token peek()
        {
            return tokens[current];
        }

        private Token previous()
        {
            return tokens[current - 1];
        }
    }
}
