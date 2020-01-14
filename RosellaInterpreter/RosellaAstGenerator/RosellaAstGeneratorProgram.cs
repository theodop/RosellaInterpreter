using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosellaAstGenerator
{
    public class RosellaAstGeneratorProgram
    {
        public static void Main(string[] args)
        {
            var outputDir = $"{Environment.CurrentDirectory}/../../../RosellaInterpreter/Ast";

            if (!Directory.Exists(outputDir)) throw new IOException($"AST folder not found");

            var definitions = new[]
            {
                "Assign   : Token name, Expr value",
                "Binary   : Expr left, Token @operator, Expr right",
                "Call     : Expr callee, Token paren, IList<Expr> arguments",
                "Grouping : Expr expression",
                "Literal  : object value",
                "Logical  : Expr left, Token @operator, Expr right",
                "Unary    : Token @operator, Expr right",
                "Variable : Token name"
            };

            defineAst(outputDir, "Expr", definitions);

            definitions = new[]
            {
                "Block      : IList<Stmt> statements",
                "Expression : Expr expression",
                "Function   : Token name, IList<Token> @params, IList<Stmt> body",
                "If         : Expr condition, Stmt thenBranch, Stmt elseBranch",
                "Print      : Expr expression",
                "Return     : Token keyword, Expr value",
                "Var        : Token name, Expr initializer",
                "While      : Expr condition, Stmt body"
            };

            defineAst(outputDir, "Stmt", definitions);
        }

        private static void defineAst(string outputDir, string baseClass, string[] types)
        {
            string path = $"{outputDir}/{baseClass}.cs";

            var fields = new LinkedList<string>();

            using (var fs = File.Open(path, FileMode.Create))
            using (var writer = new StreamWriter(fs))
            {
                writer.WriteLine("using System.Collections.Generic;");
                writer.WriteLine("namespace RosellaInterpreter {");
                writer.WriteLine($"  public abstract class {baseClass} " + "{");
                defineVisitor(writer, baseClass, types);
                writer.WriteLine($"  public abstract R accept<R>(Visitor<R> visitor);");

                foreach (var type in types)
                {
                    var className = type.Split(':')[0].Trim();
                    var field = type.Split(':')[1].Trim();
                    if (!fields.Contains(field)) fields.AddLast(field);
                    defineType(writer, baseClass, className, field);
                }
                writer.WriteLine("  }");
                writer.WriteLine("}");
            }
        }

        private static void defineVisitor(StreamWriter writer, string baseClass, string[] types)
        {
            writer.WriteLine("  public interface Visitor<R> {");

            foreach (var type in types)
            {
                var typeName = type.Split(':')[0].Trim();
                writer.WriteLine($"    R visit{typeName}{baseClass} ({typeName} {baseClass.ToLower()});");
            }

            writer.WriteLine("  }");
        }

        private static void defineType(StreamWriter writer, string baseClass, string className, string fieldList)
        {
            writer.WriteLine($"  public class {className} : {baseClass} " + "{");

            // Constructor
            writer.WriteLine($"    public {className} ({fieldList}) " + "{");

            var fields = fieldList.Split(',').Select(x => x.Trim()).ToArray();

            // setters for the fields
            foreach (var field in fields)
            {
                var name = field.Split(' ')[1];
                writer.WriteLine($"      this.{name} = {name};");
            }

            writer.WriteLine("    }");

            writer.WriteLine();

            // Visitor pattern
            writer.WriteLine($"    public override R accept<R>(Visitor<R> visitor) " + "{");
            writer.WriteLine($"      return visitor.visit{className}{baseClass}(this);");
            writer.WriteLine("    }");

            writer.WriteLine();


            // field definitions
            foreach (var field in fields)
            {
                writer.WriteLine($"    public readonly {field};");
            }

            writer.WriteLine("  }");
        }
    }
}
