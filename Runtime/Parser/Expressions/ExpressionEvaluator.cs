using System;
using System.Globalization;

namespace Ibralogue.Parser.Expressions
{
	/// <summary>
	/// Evaluates an expression AST against a variable resolver, returning a typed result.
	/// </summary>
	internal class ExpressionEvaluator
	{
		/// <summary>
		/// Delegate used to look up the current value of a variable by name.
		/// Returns null if the variable is undefined.
		/// </summary>
		public delegate object VariableResolver(string name);

		private readonly VariableResolver _resolver;

		public ExpressionEvaluator(VariableResolver resolver)
		{
			_resolver = resolver;
		}

		/// <summary>
		/// Evaluates the expression tree and returns the result as an object
		/// (double, string, or bool).
		/// </summary>
		public object Evaluate(ExpressionNode node)
		{
			if (node is LiteralNode literal)
				return literal.Value;

			if (node is VariableNode variable)
				return _resolver(variable.Name);

			if (node is UnaryNode unary)
				return EvaluateUnary(unary);

			if (node is BinaryNode binary)
				return EvaluateBinary(binary);

			throw new Exception("Unknown expression node type");
		}

		/// <summary>
		/// Evaluates the expression and interprets the result as a boolean for use in conditions.
		/// Truthiness rules: null, empty string, "false", "0", 0.0, and false are falsy.
		/// </summary>
		public bool EvaluateTruthy(ExpressionNode node)
		{
			return IsTruthy(Evaluate(node));
		}

		private object EvaluateUnary(UnaryNode node)
		{
			object operand = Evaluate(node.Operand);

			switch (node.Operator)
			{
				case ExpressionTokenType.Not:
					return !IsTruthy(operand);

				case ExpressionTokenType.Minus:
					return -ToNumber(operand);

				default:
					throw new Exception($"Unknown unary operator: {node.Operator}");
			}
		}

		private object EvaluateBinary(BinaryNode node)
		{
			if (node.Operator == ExpressionTokenType.And)
			{
				object left = Evaluate(node.Left);
				if (!IsTruthy(left)) return false;
				return IsTruthy(Evaluate(node.Right));
			}

			if (node.Operator == ExpressionTokenType.Or)
			{
				object left = Evaluate(node.Left);
				if (IsTruthy(left)) return true;
				return IsTruthy(Evaluate(node.Right));
			}

			object lhs = Evaluate(node.Left);
			object rhs = Evaluate(node.Right);

			switch (node.Operator)
			{
				case ExpressionTokenType.Plus:
					if (lhs is string || rhs is string)
						return ToString(lhs) + ToString(rhs);
					return ToNumber(lhs) + ToNumber(rhs);

				case ExpressionTokenType.Minus:
					return ToNumber(lhs) - ToNumber(rhs);

				case ExpressionTokenType.Star:
					return ToNumber(lhs) * ToNumber(rhs);

				case ExpressionTokenType.Slash:
					double divisor = ToNumber(rhs);
					if (divisor == 0) throw new Exception("Division by zero");
					return ToNumber(lhs) / divisor;

				case ExpressionTokenType.Equal:
					return AreEqual(lhs, rhs);

				case ExpressionTokenType.NotEqual:
					return !AreEqual(lhs, rhs);

				case ExpressionTokenType.LessThan:
					return Compare(lhs, rhs) < 0;

				case ExpressionTokenType.GreaterThan:
					return Compare(lhs, rhs) > 0;

				case ExpressionTokenType.LessOrEqual:
					return Compare(lhs, rhs) <= 0;

				case ExpressionTokenType.GreaterOrEqual:
					return Compare(lhs, rhs) >= 0;

				default:
					throw new Exception($"Unknown binary operator: {node.Operator}");
			}
		}

		private static bool AreEqual(object a, object b)
		{
			if (a == null && b == null) return true;
			if (a == null || b == null) return false;

			if (TryParseNumber(a, out double numA) && TryParseNumber(b, out double numB))
				return Math.Abs(numA - numB) < double.Epsilon;

			if (a is bool boolA && b is bool boolB)
				return boolA == boolB;

			return string.Equals(ToString(a), ToString(b), StringComparison.Ordinal);
		}

		private static int Compare(object a, object b)
		{
			if (TryParseNumber(a, out double numA) && TryParseNumber(b, out double numB))
				return numA.CompareTo(numB);

			return string.Compare(ToString(a), ToString(b), StringComparison.Ordinal);
		}

		private static bool IsTruthy(object value)
		{
			if (value == null) return false;
			if (value is bool b) return b;
			if (value is double d) return d != 0.0;

			string s = value.ToString();
			return s.Length > 0 && s != "false" && s != "0";
		}

		private static double ToNumber(object value)
		{
			if (value is double d) return d;
			if (value is bool b) return b ? 1.0 : 0.0;
			if (value == null) return 0.0;

			if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
				return result;

			return 0.0;
		}

		private static bool TryParseNumber(object value, out double result)
		{
			if (value is double d)
			{
				result = d;
				return true;
			}

			if (value is bool)
			{
				result = 0;
				return false;
			}

			if (value != null)
				return double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);

			result = 0;
			return false;
		}

		private static string ToString(object value)
		{
			if (value == null) return "";
			if (value is double d) return d.ToString(CultureInfo.InvariantCulture);
			if (value is bool b) return b ? "true" : "false";
			return value.ToString();
		}
	}
}
