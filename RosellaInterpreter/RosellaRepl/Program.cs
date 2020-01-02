using RosellaInterpreter;
using System;
using System.IO;

namespace RosellaRepl
{
    class Program
    {
        private static bool hadError;

        static void Main(string[] args)
        {
            Expr expression = new Expr.Binary(
        new Expr.Unary(
            new Token(TokenType.MINUS, "-", null, 1),
            new Expr.Literal(123)),
        new Token(TokenType.STAR, "*", null, 1),
        new Expr.Grouping(
            new Expr.Literal(45.67)));

            Console.WriteLine(new AstPrinter().print(expression));

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

            foreach (var token in tokens)
            {
                Console.WriteLine(token);
            }

        }

        private static void error(int line, string message)
        {
            report(line, "", message);
        }

        private static void report(int line, string where, string message)
        {
            Console.Error.WriteLine("[line " + line + "] Error" + where + ": " + message);
            hadError = true;
        }
    }
}
