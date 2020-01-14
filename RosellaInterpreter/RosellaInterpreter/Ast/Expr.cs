using System.Collections.Generic;
namespace RosellaInterpreter {
  public abstract class Expr {
  public interface Visitor<R> {
    R visitAssignExpr (Assign expr);
    R visitBinaryExpr (Binary expr);
    R visitCallExpr (Call expr);
    R visitGroupingExpr (Grouping expr);
    R visitLiteralExpr (Literal expr);
    R visitLogicalExpr (Logical expr);
    R visitUnaryExpr (Unary expr);
    R visitVariableExpr (Variable expr);
  }
  public abstract R accept<R>(Visitor<R> visitor);
  public class Assign : Expr {
    public Assign (Token name, Expr value) {
      this.name = name;
      this.value = value;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitAssignExpr(this);
    }

    public readonly Token name;
    public readonly Expr value;
  }
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
  public class Call : Expr {
    public Call (Expr callee, Token paren, IList<Expr> arguments) {
      this.callee = callee;
      this.paren = paren;
      this.arguments = arguments;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitCallExpr(this);
    }

    public readonly Expr callee;
    public readonly Token paren;
    public readonly IList<Expr> arguments;
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
  public class Logical : Expr {
    public Logical (Expr left, Token @operator, Expr right) {
      this.left = left;
      this.@operator = @operator;
      this.right = right;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitLogicalExpr(this);
    }

    public readonly Expr left;
    public readonly Token @operator;
    public readonly Expr right;
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
  public class Variable : Expr {
    public Variable (Token name) {
      this.name = name;
    }

    public override R accept<R>(Visitor<R> visitor) {
      return visitor.visitVariableExpr(this);
    }

    public readonly Token name;
  }
  }
}
