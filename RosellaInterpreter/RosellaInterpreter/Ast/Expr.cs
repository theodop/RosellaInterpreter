namespace RosellaInterpreter {
  public abstract class Expr {
  public interface Visitor<R> {
    R visitBinaryExpr (Binary expr);
    R visitGroupingExpr (Grouping expr);
    R visitLiteralExpr (Literal expr);
    R visitUnaryExpr (Unary expr);
  }
  public abstract R accept<R>(Visitor<R> visitor);
  public class Binary : Expr {
    public Binary (Expr left, Token @operator, Expr right) {
      this.left = left;
      this.@operator = @operator;
      this.right = right;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitBinaryExpr(this);
    }

    public readonly Expr left;
    public readonly Token @operator;
    public readonly Expr right;
  }
  public class Grouping : Expr {
    public Grouping (Expr expression) {
      this.expression = expression;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitGroupingExpr(this);
    }

    public readonly Expr expression;
  }
  public class Literal : Expr {
    public Literal (object value) {
      this.value = value;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitLiteralExpr(this);
    }

    public readonly object value;
  }
  public class Unary : Expr {
    public Unary (Token @operator, Expr right) {
      this.@operator = @operator;
      this.right = right;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitUnaryExpr(this);
    }

    public readonly Token @operator;
    public readonly Expr right;
  }
  }
}