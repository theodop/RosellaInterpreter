using System;
using System.Collections.Generic;
using static RosellaInterpreter.TokenType;

namespace RosellaInterpreter
{
    public class Scanner
    {
        private static readonly IDictionary<string, TokenType> keywords = new Dictionary<string, TokenType>
        {
            {"and",    AND},
            {"class",  CLASS},
            {"else",   ELSE},
            {"false",  FALSE},
            {"for",    FOR},
            {"fun",    FUN},
            {"if",     IF},
            {"nil",    NIL},
            {"or",     OR},
            {"print",  PRINT},
            {"return", RETURN},
            {"super",  SUPER},
            {"this",   THIS},
            {"true",   TRUE},
            {"var",    VAR},
            {"while",  WHILE},
        };

        private readonly string source;
        private readonly Action<int, string> errorFunc;
        private readonly IList<Token> tokens = new List<Token>();
        private int start = 0;
        private int current = 0;
        private int line = 1;


        public Scanner(string source, Action<int, string> errorFunc)
        {
            this.source = source;
            this.errorFunc = errorFunc;
        }

        public IList<Token> scanTokens()
        {
            while (!isAtEnd())
            {
                start = current;
                scanToken();
            }

            tokens.Add(new Token(EOF, "", null, line));

            return tokens;
        }

        private bool isAtEnd()
        {
            return current >= source.Length;
        }

        private void scanToken()
        {
            char c = advance();
            switch (c)
            {
                case '(': addToken(LEFT_PAREN); break;
                case ')': addToken(RIGHT_PAREN); break;
                case '{': addToken(LEFT_BRACE); break;
                case '}': addToken(RIGHT_BRACE); break;
                case ',': addToken(COMMA); break;
                case '.': addToken(DOT); break;
                case '-': addToken(MINUS); break;
                case '+': addToken(PLUS); break;
                case ';': addToken(SEMICOLON); break;
                case '*': addToken(STAR); break;
                case '!': addToken(match('=') ? BANG_EQUAL : BANG); break;
                case '=': addToken(match('=') ? EQUAL_EQUAL : EQUAL); break;
                case '<': addToken(match('=') ? LESS_EQUAL : LESS); break;
                case '>': addToken(match('=') ? GREATER_EQUAL : GREATER); break;
                case '/':
                    if (match('/'))
                    {
                        while (peek() != '\n' && !isAtEnd()) advance();
                    }
                    else
                    {
                        addToken(SLASH);
                    }
                    break;
                case ' ':
                case '\r':
                case '\t':
                    // Ignore whitespace.                      
                    break;

                case '\n':
                    line++;
                    break;

                case '"': @string(); break;

                default:
                    if (isDigit(c))
                    {
                        number();
                    }
                    else if (isAlpha(c))
                    {
                        identifier();
                    }
                    else
                    {
                        errorFunc(line, "Unexpected character.");
                    }
                    break;
            }
        }

        private char advance()
        {
            current++;
            return source[current - 1];
        }

        private void addToken(TokenType type)
        {
            addToken(type, null);
        }

        private void addToken(TokenType type, object literal)
        {
            var text = source.JavaSubstring(start, current);
            tokens.Add(new Token(type, text, literal, line));
        }

        private bool match(char expected)
        {
            if (isAtEnd()) return false;
            if (source[current] != expected) return false;
            current++;
            return true;
        }

        private char peek()
        {
            if (isAtEnd()) return '\0';
            return source[current];
        }
        
        private void @string()
        {
            while (peek() != '"' && !isAtEnd())
            {
                if (peek() == '\n') line++;
                advance();
            }

            if (isAtEnd())
            {
                errorFunc(line, "Unterminated string.");
                return;
            }

            advance();

            var value = source.JavaSubstring(start + 1, current - 1);
            addToken(STRING, value);
        }

        private bool isDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private void number()
        {
            while (isDigit(peek())) advance();

            if (peek() == '.' && isDigit(peekNext()))
            {
                advance();

                while (isDigit(peek())) advance();
            }

            addToken(NUMBER, double.Parse(source.JavaSubstring(start, current)));
        }

        private char peekNext()
        {
            if (current + 1 >= source.Length) return '\0';
            return source[current + 1];
        }

        private void identifier()
        {
            while (isAlphaNumeric(peek())) advance();

            var text = source.JavaSubstring(start, current);
            var type = keywords.ContainsKey(text) ? (TokenType?)keywords[text] : null;
            if (type == null) type = IDENTIFIER;

            addToken(type.Value);
        }

        private bool isAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
               (c >= 'A' && c <= 'Z') ||
                c == '_';
        }

        private bool isAlphaNumeric(char c)
        {
            return isAlpha(c) || isDigit(c);
        }
    }
}
