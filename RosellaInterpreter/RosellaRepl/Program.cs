using RosellaInterpreter;
using System;
using System.IO;
using static RosellaInterpreter.Interpreter;

namespace RosellaRepl
{
    class Program
    {
        private static bool hadError;
        private static bool hadRuntimeError;

        private static readonly Interpreter interpreter = new Interpreter(runtimeError);

        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: RosellaRepl [script]");
                Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                runFile(args[0]);
            }
            else
            {
                runPrompt();
            }
        }

        private static void runFile(string path)
        {
            var text = File.ReadAllText(path);
            run(text);

            if (hadError) Environment.Exit(65);
            if (hadRuntimeError) Environment.Exit(70);
        }

        private static void runPrompt()
        {
            while (true)
            {
                Console.Write("> ");
                run(Console.ReadLine());
                hadError = false;
            }
        }

        private static void run(string source)
        {
            var scanner = new Scanner(source, error);
            var tokens = scanner.scanTokens();
            var parser = new Parser(tokens, tokenError);
            var statements = parser.parse();

            if (hadError) return;

            interpreter.interpret(statements);
        }

        private static void error(int line, string message)
        {
            report(line, "", message);
        }

        private static void tokenError(Token token, string message)
        {
            if (token.type == TokenType.EOF)
            {
                report(token.line, " at end", message);
            }
            else
            {
                report(token.line, $" at '{token.lexeme}'", message);
            }
        }

        private static void runtimeError(RuntimeError error)
        {
            Console.Error.WriteLine($"{error.Message} \n[line {error.token.line} ]");
            hadRuntimeError = true;
        }

        private static void report(int line, string where, string message)
        {
            Console.Error.WriteLine("[line " + line + "] Error" + where + ": " + message);
            hadError = true;
        }
    }
}
