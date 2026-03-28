// Ibralogue .ibra syntax highlighter for docfx
// Ported from the landing page's highlightIbra.tsx
(function () {
  function escapeHtml(str) {
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  function highlightVars(raw, baseClass) {
    var parts = raw.split(/(\$[a-zA-Z_]\w*)/);
    if (parts.length === 1) return '<span class="ib-' + baseClass + '">' + escapeHtml(raw) + '</span>';
    return parts.map(function (p) {
      if (p.charAt(0) === '$') return '<span class="ib-variable">' + escapeHtml(p) + '</span>';
      return p ? '<span class="ib-' + baseClass + '">' + escapeHtml(p) + '</span>' : '';
    }).join('');
  }

  function highlightLine(line) {
    var trimmed = line.replace(/^\s+/, '');
    var pad = line.length - trimmed.length;
    var indent = pad > 0 ? '<span>' + new Array(pad + 1).join(' ') + '</span>' : '';

    if (!trimmed) return '';

    // # comment (but not ## metadata)
    if (/^#(?!#)/.test(trimmed))
      return indent + '<span class="ib-comment">' + escapeHtml(trimmed) + '</span>';

    // ## metadata
    if (/^##/.test(trimmed))
      return indent + '<span class="ib-comment">' + escapeHtml(trimmed) + '</span>';

    // {{Keyword(args)}} commands
    var cmd = trimmed.match(/^(\{\{)(\w+)(?:(\()(.+?)(\)))?(\}\})$/);
    if (cmd) {
      var isConv = cmd[2] === 'ConversationName';
      var result = indent;
      result += '<span class="ib-punct">' + escapeHtml(cmd[1]) + '</span>';
      result += '<span class="ib-keyword">' + escapeHtml(cmd[2]) + '</span>';
      if (cmd[3]) {
        result += '<span class="ib-punct">' + escapeHtml(cmd[3]) + '</span>';
        result += highlightVars(cmd[4], isConv ? 'section' : 'text');
        result += '<span class="ib-punct">' + escapeHtml(cmd[5]) + '</span>';
      }
      result += '<span class="ib-punct">' + escapeHtml(cmd[6]) + '</span>';
      return result;
    }

    // {{If/ElseIf/Else/EndIf}} with expressions
    var cond = trimmed.match(/^(\{\{)(If|ElseIf|Else|EndIf)(?:(\()(.+?)(\)))?(\}\})$/);
    if (cond) {
      var r = indent;
      r += '<span class="ib-punct">' + escapeHtml(cond[1]) + '</span>';
      r += '<span class="ib-keyword">' + escapeHtml(cond[2]) + '</span>';
      if (cond[3]) {
        r += '<span class="ib-punct">' + escapeHtml(cond[3]) + '</span>';
        r += highlightVars(cond[4], 'text');
        r += '<span class="ib-punct">' + escapeHtml(cond[5]) + '</span>';
      }
      r += '<span class="ib-punct">' + escapeHtml(cond[6]) + '</span>';
      return r;
    }

    // [Speaker] or [>>]
    var spk = trimmed.match(/^\[([^\]]*)\]$/);
    if (spk)
      return indent + '<span class="ib-punct">[</span>' + highlightVars(spk[1], 'speaker') + '<span class="ib-punct">]</span>';

    // - Choice text -> Target
    var ch = trimmed.match(/^(-)\s+(.*?)\s*(->)\s*(\S+)(.*)$/);
    if (ch) {
      var c = indent;
      c += '<span class="ib-punct">' + escapeHtml(ch[1]) + ' </span>';
      c += highlightVars(ch[2], 'text');
      c += '<span class="ib-operator"> ' + escapeHtml(ch[3]) + ' </span>';
      c += '<span class="ib-section">' + escapeHtml(ch[4]) + '</span>';
      if (ch[5]) c += '<span class="ib-comment">' + escapeHtml(ch[5]) + '</span>';
      return c;
    }

    // - Choice text >> (continue)
    var cont = trimmed.match(/^(-)\s+(.*?)\s*(>>)\s*$/);
    if (cont) {
      var cc = indent;
      cc += '<span class="ib-punct">' + escapeHtml(cont[1]) + ' </span>';
      cc += highlightVars(cont[2], 'text');
      cc += '<span class="ib-operator"> ' + escapeHtml(cont[3]) + '</span>';
      return cc;
    }

    // Dialogue text (with possible $vars)
    return indent + highlightVars(trimmed, 'text');
  }

  function highlightAllIbra() {
    var blocks = document.querySelectorAll('code.lang-ibra, code.lang-text');
    for (var i = 0; i < blocks.length; i++) {
      var el = blocks[i];
      // Only highlight blocks that look like ibra syntax (have speakers or commands)
      var text = el.textContent || '';
      if (el.classList.contains('lang-text') && !/\[.+\]|^\{\{/m.test(text)) continue;

      var lines = text.split('\n');
      // Remove trailing empty line that docfx adds
      if (lines.length > 0 && lines[lines.length - 1].trim() === '') lines.pop();

      var html = lines.map(function (line) {
        var highlighted = highlightLine(line);
        return '<div class="ib-line">' + (highlighted || '&nbsp;') + '</div>';
      }).join('');

      el.innerHTML = html;
      el.classList.add('ib-highlighted');
    }
  }

  // Run after DOM is ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', highlightAllIbra);
  } else {
    highlightAllIbra();
  }
})();
