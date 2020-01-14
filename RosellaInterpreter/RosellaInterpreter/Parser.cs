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
                if (match(FUN)) return function("function");
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
            if (match(FOR)) return forStatement();
            if (match(IF)) return ifStatement();
            if (match(PRINT)) return printStatement();
            if (match(RETURN)) return returnStatement();
            if (match(WHILE)) return whileStatement();
            if (match(LEFT_BRACE)) return new Stmt.Block(block());

            return expressionStatement();
        }

        private Stmt forStatement()
        {
            consume(LEFT_PAREN, "Expect '(' after 'for'.");

            Stmt initializer;
            if (match(SEMICOLON))
            {
                initializer = null;
            } 
            else if (match(VAR))
            {
                initializer = varDeclaration();
            }
            else
            {
                initializer = expressionStatement();
            }

            Expr condition = null;
            if (!check(SEMICOLON))
            {
                condition = expression();
            }
            consume(SEMICOLON, "Expect ';' after loop condition.");

            Expr increment = null;
            if (!check(RIGHT_PAREN))
            {
                increment = expression();
            }
            consume(RIGHT_PAREN, "Expect '(' after for clauses.");

            var body = statement();

            if (increment != null)
            {
                body = new Stmt.Block(new[] { body, new Stmt.Expression(increment) });
            }

            if (condition == null) condition = new Expr.Literal(true);
            body = new Stmt.While(condition, body);

            if (initializer != null)
            {
                body = new Stmt.Block(new[] { initializer, body });
            }

            return body;
        }

        private Stmt ifStatement()
        {
            consume(LEFT_PAREN, "Expect '(' after 'if'.");
            Expr condition = expression();
            consume(RIGHT_PAREN, "Expect ')' after if condition.");

            var thenBranch = statement();
            Stmt elseBranch = null;
            if (match (ELSE))
            {
                elseBranch = statement();
            }

            return new Stmt.If(condition, thenBranch, elseBranch);
        }

        private Stmt printStatement()
        {
            var value = expression();
            consume(SEMICOLON, "Expect ';' after value.");
            return new Stmt.Print(value);
        }

        private Stmt returnStatement()
        {
            var keyword = previous();
            Expr value = null;
            if (!check(SEMICOLON))
            {
                value = expression();
            }

            consume(SEMICOLON, "Expect ';' after return value.");
            return new Stmt.Return(keyword, value);
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

        private Stmt whileStatement()
        {
            consume(LEFT_PAREN, "Expect '(' after 'while'.");
            var condition = expression();
            consume(RIGHT_PAREN, "Expect ')' after condition.");
            var body = statement();

            return new Stmt.While(condition, body);
        }

        private Stmt expressionStatement()
        {
            var expr = expression();
            consume(SEMICOLON, "Expect ';' after expression.");
            return new Stmt.Expression(expr);
        }

        private Stmt.Function function(string kind)
        {
            var name = consume(IDENTIFIER, $"Expect {kind} name.");
            consume(LEFT_PAREN, $"Expect '(' after {kind} name.");
            var parameters = new LinkedList<Token>();
            if (!check(RIGHT_PAREN))
            {
                do
                {
                    if (parameters.Count >= 255)
                    {
                        error(peek(), "Cannot have more than 255 parameters.");
                    }

                    parameters.AddLast(consume(IDENTIFIER, "Expect parameter name."));
                } while (match(COMMA));
            }
            consume(RIGHT_PAREN, "Expect ')' after parameters.");

            consume(LEFT_BRACE, $"Expect '{{' before {kind} body.");
            var body = block();

            return new Stmt.Function(name, parameters.ToArray(), body);
        }

        private IList<Stmt> block()
        {
            var statements = new LinkedList<Stmt>();

            while (!check(RIGHT_BRACE) && !isAtEnd())
            {
                statements.AddLast(declaration());
            }

            consume(RIGHT_BRACE, "Expect '}' after block.");

            return statements.ToList();
        }

        private Expr expression()
        {
            return assignment();
        }

        private Expr assignment()
        {
            var expr = or();

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

        private Expr or()
        {
            var expr = and();

            while (match(OR))
            {
                var @operator = previous();
                var right = and();
                expr = new Expr.Logical(expr, @operator, right);
            }

            return expr;
        }

        private Expr and()
        {
            var expr = equality();

            while (match(AND))
            {
                var @operator = previous();
                var right = equality();
                expr = new Expr.Logical(expr, @operator, right);
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

            return call();
        }

        private Expr call()
        {
            var expr = primary();

            while (true)
            {
                if (match(LEFT_PAREN))
                {
                    expr = finishCall(expr);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private Expr finishCall(Expr callee)
        {
            var arguments = new List<Expr>();

            if (!check(RIGHT_PAREN))
            {
                do
                {
                    if (arguments.Count() >= 255)
                    {
                        error(peek(), "Cannot have more than 255 arguments");
                    }
                    arguments.Add(expression());
                } while (match(COMMA));
            }

            var paren = consume(RIGHT_PAREN, "Expect ')' after arguments.");

            return new Expr.Call(callee, paren, arguments);
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

                advance();
            }
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
