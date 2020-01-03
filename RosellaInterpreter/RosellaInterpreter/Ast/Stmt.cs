namespace RosellaInterpreter {
  public abstract class Stmt {
  public interface Visitor<R> {
    R visitExpressionStmt (Expression stmt);
    R visitPrintStmt (Print stmt);
    R visitVarStmt (Var stmt);
  }
  public abstract R accept<R>(Visitor<R> visitor);
  public class Expression : Stmt {
    public Expression (Expr expression) {
      this.expression = expression;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitExpressionStmt(this);
    }

    public readonly Expr expression;
  }
  public class Print : Stmt {
    public Print (Expr expression) {
      this.expression = expression;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitPrintStmt(this);
    }

    public readonly Expr expression;
  }
  public class Var : Stmt {
    public Var (Token name, Expr initializer) {
      this.name = name;
      this.initializer = initializer;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitVarStmt(this);
    }

    public readonly Token name;
    public readonly Expr initializer;
  }
  }
}
