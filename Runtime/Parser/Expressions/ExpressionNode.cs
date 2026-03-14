namespace Ibralogue.Parser.Expressions
{
	/// <summary>
	/// Base type for all expression AST nodes.
	/// </summary>
	internal abstract class ExpressionNode
	{
	}

	/// <summary>
	/// A literal value: string, number, or boolean.
	/// </summary>
	internal class LiteralNode : ExpressionNode
	{
		public readonly object Value;

		public LiteralNode(object value)
		{
			Value = value;
		}
	}

	/// <summary>
	/// A variable reference within an expression: $VariableName
	/// </summary>
	internal class VariableNode : ExpressionNode
	{
		public readonly string Name;

		public VariableNode(string name)
		{
			Name = name;
		}
	}

	/// <summary>
	/// A binary operation: left OP right (arithmetic, comparison, or boolean).
	/// </summary>
	internal class BinaryNode : ExpressionNode
	{
		public readonly ExpressionNode Left;
		public readonly ExpressionTokenType Operator;
		public readonly ExpressionNode Right;

		public BinaryNode(ExpressionNode left, ExpressionTokenType op, ExpressionNode right)
		{
			Left = left;
			Operator = op;
			Right = right;
		}
	}

	/// <summary>
	/// A unary operation: NOT expr or -expr.
	/// </summary>
	internal class UnaryNode : ExpressionNode
	{
		public readonly ExpressionTokenType Operator;
		public readonly ExpressionNode Operand;

		public UnaryNode(ExpressionTokenType op, ExpressionNode operand)
		{
			Operator = op;
			Operand = operand;
		}
	}
}
