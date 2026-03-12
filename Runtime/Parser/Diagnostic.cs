using System.Collections.Generic;

namespace Ibralogue.Parser
{
	/// <summary>
	/// Severity level for a diagnostic message.
	/// </summary>
	internal enum DiagnosticSeverity
	{
		Warning,
		Error
	}

	/// <summary>
	/// A structured diagnostic message with source location information.
	/// </summary>
	internal readonly struct Diagnostic
	{
		public readonly DiagnosticSeverity Severity;
		public readonly SourceSpan Span;
		public readonly string Message;

		public Diagnostic(DiagnosticSeverity severity, SourceSpan span, string message)
		{
			Severity = severity;
			Span = span;
			Message = message;
		}

		public override string ToString()
		{
			return $"[line {Span.Start.Line}:{Span.Start.Column}] {Severity}: {Message}";
		}
	}

	/// <summary>
	/// Collects diagnostic messages during lexing, parsing, and analysis.
	/// </summary>
	internal class DiagnosticBag
	{
		private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

		public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
		public bool HasErrors { get; private set; }

		public void ReportError(SourceSpan span, string message)
		{
			_diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, span, message));
			HasErrors = true;
		}

		public void ReportWarning(SourceSpan span, string message)
		{
			_diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, span, message));
		}
	}
}
