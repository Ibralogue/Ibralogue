using Ibralogue.Parser;
using NUnit.Framework;

namespace Ibralogue.Editor.Tests
{
	public class PreprocessorTests
	{
		[Test]
		public void ExtractConversation_FindsNamedConversation()
		{
			string source =
				"{{ConversationName(A)}}\n[Foo]\nHello\n" +
				"{{ConversationName(B)}}\n[Bar]\nWorld\n";

			string result = DialoguePreprocessor.ExtractConversation(source, "A");

			Assert.That(result, Is.Not.Null);
			Assert.That(result, Does.Contain("[Foo]"));
			Assert.That(result, Does.Contain("Hello"));
			Assert.That(result, Does.Not.Contain("[Bar]"));
			Assert.That(result, Does.Not.Contain("World"));
		}

		[Test]
		public void ExtractConversation_FindsLastConversation()
		{
			string source =
				"{{ConversationName(A)}}\n[Foo]\nHello\n" +
				"{{ConversationName(B)}}\n[Bar]\nWorld\n";

			string result = DialoguePreprocessor.ExtractConversation(source, "B");

			Assert.That(result, Is.Not.Null);
			Assert.That(result, Does.Contain("[Bar]"));
			Assert.That(result, Does.Contain("World"));
			Assert.That(result, Does.Not.Contain("[Foo]"));
		}

		[Test]
		public void ExtractConversation_ReturnsNull_WhenNotFound()
		{
			string source =
				"{{ConversationName(A)}}\n[Foo]\nHello\n";

			string result = DialoguePreprocessor.ExtractConversation(source, "DoesNotExist");

			Assert.That(result, Is.Null);
		}

		[Test]
		public void ExtractConversation_IncludesChoices()
		{
			string source =
				"{{ConversationName(A)}}\n[Foo]\nHello\n" +
				"- Choice 1 -> B\n" +
				"- Choice 2 -> C\n" +
				"{{ConversationName(B)}}\n[Bar]\nWorld\n";

			string result = DialoguePreprocessor.ExtractConversation(source, "A");

			Assert.That(result, Does.Contain("- Choice 1 -> B"));
			Assert.That(result, Does.Contain("- Choice 2 -> C"));
			Assert.That(result, Does.Not.Contain("[Bar]"));
		}

		[Test]
		public void Process_NoIncludes_ReturnsSourceUnchanged()
		{
			DiagnosticBag diagnostics = new DiagnosticBag();
			DialoguePreprocessor preprocessor = new DialoguePreprocessor(diagnostics, "Test");

			string source = "{{ConversationName(A)}}\n[Foo]\nHello\n";
			string result = preprocessor.Process(source);

			Assert.That(result, Is.EqualTo(source));
			Assert.That(diagnostics.HasErrors, Is.False);
		}

		[Test]
		public void Process_CircularInclude_ReportsError()
		{
			DiagnosticBag diagnostics = new DiagnosticBag();
			DialoguePreprocessor preprocessor = new DialoguePreprocessor(diagnostics, "SelfRef");

			string source = "{{Include(SelfRef)}}\n[Foo]\nHello\n";
			string result = preprocessor.Process(source);

			Assert.That(diagnostics.HasErrors, Is.True);
			Assert.That(diagnostics.Diagnostics[0].Message, Does.Contain("Circular include"));
		}

		[Test]
		public void Process_QuotedCircularInclude_ReportsError()
		{
			DiagnosticBag diagnostics = new DiagnosticBag();
			DialoguePreprocessor preprocessor = new DialoguePreprocessor(diagnostics, "SelfRef");

			string source = "{{Include(\"SelfRef\")}}\n[Foo]\nHello\n";
			string result = preprocessor.Process(source);

			Assert.That(diagnostics.HasErrors, Is.True);
			Assert.That(diagnostics.Diagnostics[0].Message, Does.Contain("Circular include"));
		}

		[Test]
		public void Process_QuotedIncludeNotFound_ReportsError()
		{
			DiagnosticBag diagnostics = new DiagnosticBag();
			DialoguePreprocessor preprocessor = new DialoguePreprocessor(diagnostics, "Test");

			string source = "{{Include(\"NonExistentAsset\")}}\n[Foo]\nHello\n";
			string result = preprocessor.Process(source);

			Assert.That(diagnostics.HasErrors, Is.True);
			Assert.That(diagnostics.Diagnostics[0].Message, Does.Contain("not found in Resources"));
		}

		[Test]
		public void Process_IncludeNotFound_ReportsError()
		{
			DiagnosticBag diagnostics = new DiagnosticBag();
			DialoguePreprocessor preprocessor = new DialoguePreprocessor(diagnostics, "Test");

			string source = "{{Include(NonExistentAsset)}}\n[Foo]\nHello\n";
			string result = preprocessor.Process(source);

			Assert.That(diagnostics.HasErrors, Is.True);
			Assert.That(diagnostics.Diagnostics[0].Message, Does.Contain("not found in Resources"));
		}

		[Test]
		public void Process_InlineInclude_NotExpanded()
		{
			// Include must be on its own line to be treated as a directive.
			// An inline {{Include(...)}} in the middle of text should NOT be expanded.
			DiagnosticBag diagnostics = new DiagnosticBag();
			DialoguePreprocessor preprocessor = new DialoguePreprocessor(diagnostics, "Test");

			string source = "[Foo]\nSome text {{Include(Other)}} more text\n";
			string result = preprocessor.Process(source);

			Assert.That(result, Is.EqualTo(source));
			Assert.That(diagnostics.HasErrors, Is.False);
		}
	}
}
