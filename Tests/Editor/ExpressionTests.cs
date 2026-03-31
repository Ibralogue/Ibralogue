using NUnit.Framework;
using Ibralogue.Parser.Expressions;
using ExpressionEvaluator = Ibralogue.Parser.Expressions.ExpressionEvaluator;
using ExpressionParser = Ibralogue.Parser.Expressions.ExpressionParser;

namespace Ibralogue.Editor.Tests
{
	public class ExpressionTests
	{
		private ExpressionNode Parse(string input)
		{
			var lexer = new ExpressionLexer(input);
			var tokens = lexer.Tokenize();
			var parser = new ExpressionParser(tokens);
			return parser.Parse();
		}

		private ExpressionEvaluator CreateEvaluator(
			ExpressionEvaluator.VariableResolver variableResolver = null,
			ExpressionEvaluator.FunctionResolver functionResolver = null)
		{
			return new ExpressionEvaluator(
				variableResolver ?? (name => null),
				functionResolver
			);
		}

		// --- Parser: function calls ---

		[Test]
		public void Parser_FunctionCall_NoArgs()
		{
			var node = Parse("GetValue()");

			Assert.That(node, Is.InstanceOf<FunctionCallNode>());
			var call = (FunctionCallNode)node;
			Assert.That(call.Name, Is.EqualTo("GetValue"));
			Assert.That(call.Arguments, Has.Count.EqualTo(0));
		}

		[Test]
		public void Parser_FunctionCall_WithStringArg()
		{
			var node = Parse("Visited(\"Tavern\")");

			Assert.That(node, Is.InstanceOf<FunctionCallNode>());
			var call = (FunctionCallNode)node;
			Assert.That(call.Name, Is.EqualTo("Visited"));
			Assert.That(call.Arguments, Has.Count.EqualTo(1));
			Assert.That(call.Arguments[0], Is.InstanceOf<LiteralNode>());
			Assert.That(((LiteralNode)call.Arguments[0]).Value, Is.EqualTo("Tavern"));
		}

		[Test]
		public void Parser_FunctionCall_WithMultipleArgs()
		{
			var node = Parse("Clamp($X, 0, 100)");

			Assert.That(node, Is.InstanceOf<FunctionCallNode>());
			var call = (FunctionCallNode)node;
			Assert.That(call.Name, Is.EqualTo("Clamp"));
			Assert.That(call.Arguments, Has.Count.EqualTo(3));
		}

		[Test]
		public void Parser_FunctionCall_InBinaryExpression()
		{
			var node = Parse("Visited(\"Tavern\") AND $GOLD > 50");

			Assert.That(node, Is.InstanceOf<BinaryNode>());
			var binary = (BinaryNode)node;
			Assert.That(binary.Left, Is.InstanceOf<FunctionCallNode>());
		}

		[Test]
		public void Parser_BareIdentifier_IsVariable()
		{
			var node = Parse("MYVAR");

			Assert.That(node, Is.InstanceOf<VariableNode>());
		}

		// --- Evaluator: function calls ---

		[Test]
		public void Evaluator_FunctionResolver_Called()
		{
			var node = Parse("IsReady(\"test\")");
			var evaluator = CreateEvaluator(
				functionResolver: (name, args) =>
					name == "IsReady" && args.Length == 1 && (string)args[0] == "test"
			);

			Assert.That(evaluator.EvaluateTruthy(node), Is.True);
		}

		[Test]
		public void Evaluator_FunctionReturningFalse_IsFalsy()
		{
			var node = Parse("AlwaysFalse()");
			var evaluator = CreateEvaluator(
				functionResolver: (name, args) => false
			);

			Assert.That(evaluator.EvaluateTruthy(node), Is.False);
		}

		[Test]
		public void Evaluator_NotFunctionCall()
		{
			var node = Parse("NOT Visited(\"Tavern\")");
			var evaluator = CreateEvaluator(
				functionResolver: (name, args) => false
			);

			Assert.That(evaluator.EvaluateTruthy(node), Is.True);
		}

		// --- Evaluator: arithmetic ---

		[Test]
		public void Evaluator_Addition()
		{
			var node = Parse("3 + 4");
			var evaluator = CreateEvaluator();

			Assert.That(evaluator.Evaluate(node), Is.EqualTo(7.0));
		}

		[Test]
		public void Evaluator_Subtraction()
		{
			var node = Parse("10 - 3");
			var evaluator = CreateEvaluator();

			Assert.That(evaluator.Evaluate(node), Is.EqualTo(7.0));
		}

		[Test]
		public void Evaluator_Multiplication()
		{
			var node = Parse("5 * 6");
			var evaluator = CreateEvaluator();

			Assert.That(evaluator.Evaluate(node), Is.EqualTo(30.0));
		}

		[Test]
		public void Evaluator_Division()
		{
			var node = Parse("10 / 4");
			var evaluator = CreateEvaluator();

			Assert.That(evaluator.Evaluate(node), Is.EqualTo(2.5));
		}

		[Test]
		public void Evaluator_DivisionByZero_Throws()
		{
			var node = Parse("1 / 0");
			var evaluator = CreateEvaluator();

			Assert.That(() => evaluator.Evaluate(node), Throws.Exception.With.Message.Contains("Division by zero"));
		}

		[Test]
		public void Evaluator_StringConcatenation()
		{
			var node = Parse("\"hello\" + \" world\"");
			var evaluator = CreateEvaluator();

			Assert.That(evaluator.Evaluate(node), Is.EqualTo("hello world"));
		}

		[Test]
		public void Evaluator_UnaryNegation()
		{
			var node = Parse("-42");
			var evaluator = CreateEvaluator();

			Assert.That(evaluator.Evaluate(node), Is.EqualTo(-42.0));
		}

		// --- Evaluator: comparisons ---

		[Test]
		public void Evaluator_LessThan()
		{
			var evaluator = CreateEvaluator();

			Assert.That(evaluator.Evaluate(Parse("3 < 5")), Is.True);
			Assert.That(evaluator.Evaluate(Parse("5 < 3")), Is.False);
		}

		[Test]
		public void Evaluator_GreaterThan()
		{
			var evaluator = CreateEvaluator();

			Assert.That(evaluator.Evaluate(Parse("5 > 3")), Is.True);
			Assert.That(evaluator.Evaluate(Parse("3 > 5")), Is.False);
		}

		[Test]
		public void Evaluator_Equality()
		{
			var evaluator = CreateEvaluator();

			Assert.That(evaluator.Evaluate(Parse("42 == 42")), Is.True);
			Assert.That(evaluator.Evaluate(Parse("42 == 43")), Is.False);
		}

		[Test]
		public void Evaluator_NotEqual()
		{
			var evaluator = CreateEvaluator();

			Assert.That(evaluator.Evaluate(Parse("42 != 43")), Is.True);
			Assert.That(evaluator.Evaluate(Parse("42 != 42")), Is.False);
		}

		// --- Evaluator: boolean logic ---

		[Test]
		public void Evaluator_And_ShortCircuits()
		{
			bool rhsEvaluated = false;
			var evaluator = CreateEvaluator(
				variableResolver: name =>
				{
					if (name == "RHS") { rhsEvaluated = true; return true; }
					return null;
				}
			);

			var node = Parse("false AND $RHS");
			Assert.That(evaluator.EvaluateTruthy(node), Is.False);
			Assert.That(rhsEvaluated, Is.False);
		}

		[Test]
		public void Evaluator_Or_ShortCircuits()
		{
			bool rhsEvaluated = false;
			var evaluator = CreateEvaluator(
				variableResolver: name =>
				{
					if (name == "RHS") { rhsEvaluated = true; return false; }
					return null;
				}
			);

			var node = Parse("true OR $RHS");
			Assert.That(evaluator.EvaluateTruthy(node), Is.True);
			Assert.That(rhsEvaluated, Is.False);
		}

		// --- Evaluator: truthiness ---

		[Test]
		public void Evaluator_Truthiness_FalsyValues()
		{
			var evaluator = CreateEvaluator();

			// null is falsy
			Assert.That(evaluator.EvaluateTruthy(Parse("$UNDEFINED")), Is.False);

			// false is falsy
			Assert.That(evaluator.EvaluateTruthy(Parse("false")), Is.False);

			// 0 is falsy
			Assert.That(evaluator.EvaluateTruthy(Parse("0")), Is.False);

			// "false" string is falsy
			var evalWithFalseStr = CreateEvaluator(
				variableResolver: name => name == "X" ? (object)"false" : null
			);
			Assert.That(evalWithFalseStr.EvaluateTruthy(Parse("$X")), Is.False);

			// "0" string is falsy
			var evalWithZeroStr = CreateEvaluator(
				variableResolver: name => name == "X" ? (object)"0" : null
			);
			Assert.That(evalWithZeroStr.EvaluateTruthy(Parse("$X")), Is.False);

			// empty string is falsy
			var evalWithEmpty = CreateEvaluator(
				variableResolver: name => name == "X" ? (object)"" : null
			);
			Assert.That(evalWithEmpty.EvaluateTruthy(Parse("$X")), Is.False);
		}

		[Test]
		public void Evaluator_Truthiness_TruthyValues()
		{
			// true is truthy
			var evaluator = CreateEvaluator();
			Assert.That(evaluator.EvaluateTruthy(Parse("true")), Is.True);

			// non-zero number is truthy
			Assert.That(evaluator.EvaluateTruthy(Parse("42")), Is.True);

			// non-empty string is truthy
			var evalWithStr = CreateEvaluator(
				variableResolver: name => name == "X" ? (object)"hello" : null
			);
			Assert.That(evalWithStr.EvaluateTruthy(Parse("$X")), Is.True);
		}

		// --- Evaluator: variable resolution ---

		[Test]
		public void Evaluator_VariableWithoutDollar_ResolvesAsVariable()
		{
			var evaluator = CreateEvaluator(
				variableResolver: name => name == "MYVAR" ? (object)true : null
			);

			Assert.That(evaluator.EvaluateTruthy(Parse("MYVAR")), Is.True);
		}
	}
}
