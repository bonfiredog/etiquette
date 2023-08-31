using System.Collections.Generic;
using System.Linq;

namespace UnityTracery {
  enum TagType {
    Raw,
    PlainText,
    Tag,
    Action
  }

  class ActionToken : GrammarToken {
    public string Key;

    protected override string innerText {
      get {
        var text = Resolved ?? Raw;
        return text.Substring(1, text.Length - 2);
      }
    }

    public ActionToken(TraceryGrammar grammar, int start, GrammarToken parent) : base(grammar, start, parent) {
      Type = TagType.Action;
    }

    protected override void AddCharInner(char ch) {
      switch (ch) {
      case ':':
        // Occurrances of ':' within actions separate key from value(s).
        AddIndexOfInterest();
        break;
      case ',':
        if (IndecesOfInterest == null) {
          // If we haven't hit a ':' yet, then commas mean nothing.
          break;
        }
        AddIndexOfInterest();
        break;
      default:
        break;
      }
      Raw += ch;
    }

    public override bool Resolve() {
      if (!base.Resolve()) {
        return false;
      }
      Key = GetSubstringOfInterest(0);
      if (Key == Resolved) {
        // No separating ":", just resolving contents itself.
        Key = null;
      } else {
        // Remove opening '[' from key.
        Key = Key.Substring(1);
        var numOptions = IndecesOfInterest == null ? 0 : IndecesOfInterest.Count;
        var options = new string[numOptions];
        for (var i = 1; i <= numOptions; i++) {
          options[i - 1] = GetSubstringOfInterest(i);
        }
        // Remove closing ']' from last option.
        var last = options[options.Length - 1];
        last = last.Substring(0, last.Length - 1);
        options[options.Length - 1] = last;
        // If action is "POP", pop the selected key instead of saving it.
        if (options.Length == 1 && options[0] == "POP") {
          Grammar.PopAction(Key);
          Key = "";
        } else {
          Grammar.PushAction(Key, options);
        }
      }

      Resolved = "";
      return true;
    }

    public override void PopRule() {
      Grammar.PopAction(Key);
      if (Children != null) {
        Children.ForEach(child => child.PopRule());
      }
    }
  }

  class TagToken : GrammarToken {
    protected override string innerText {
      get {
        var text = Resolved ?? Raw;
        return text.Substring(1, text.Length - 2);
      }
    }

    public TagToken(TraceryGrammar grammar, int start, GrammarToken parent) : base(grammar, start, parent) {
      Type = TagType.Tag;
    }

    protected override void AddCharInner(char ch) {
      switch (ch) {
      case '.':
        AddIndexOfInterest();
        Raw += ch;
        break;
      default:
        Raw += ch;
        break;
      }
    }

    public override bool Resolve() {
      if (!base.Resolve()) {
        return false;
      }
      // Once text is fully resolved, replace it with the grammar/action rule, if any.
      var finalText = Grammar.ParseInner(Grammar.ResolveSymbol(GetSubstringOfInterest(0)));
      // If there are any modifiers, apply them now.
      if (IndecesOfInterest != null && IndecesOfInterest.Count > 0) {
        var modifiers = new string[IndecesOfInterest.Count];
        for (var i = 1; i <= IndecesOfInterest.Count; i++) {
          modifiers[i - 1] = GetSubstringOfInterest(i);
        }
        // Remove closing # from last modifier.
        var last = modifiers[modifiers.Length - 1];
        last = last.Substring(0, last.Length - 1);
        modifiers[modifiers.Length - 1] = last;
        finalText = Grammar.ApplyModifiers(finalText, modifiers);
      }
      Resolved = finalText;
      // When a tag is resolved, any of its children that are actions pop their save rule,
      // as those actions are meant to be local to the tag.
      if (Children != null) {
        Children.ForEach(child => child.PopRule());
      }
      return true;
    }
  }

  abstract class GrammarToken {
    public TraceryGrammar Grammar;
    public int Start;
    public GrammarToken Parent;
    public TagType Type;
    public string Raw;
    public string Resolved;
    public bool IsResolved;
    public List<GrammarToken> Children;
    public List<int> IndecesOfInterest;

    protected virtual string innerText { get { return Raw ?? Resolved; } }

    public override string ToString() {
      return string.Format("Raw: {0}\nResolved: {1}\nIndecesOfInterest: {2}", Raw, Resolved, IndecesOfInterest.Aggregate("", (cume, ind) => cume + ", " + ind.ToString()));
    }

    public GrammarToken(TraceryGrammar grammar, int start, GrammarToken parent) {
      Raw = "";
      Grammar = grammar;
      Start = start;
      Parent = parent;
    }

    /// <summary>
    /// Resolves the Raw text according to the token type.
    /// </summary>
    /// <returns>False if the token was already resolved.</returns>
    public virtual bool Resolve() {
      if (IsResolved) {
        return false;
      }
      Resolved = Raw;
      if (Children != null) {
        Children.ForEach(child => {
          child.Resolve();
          ReplaceInnerText(child);
        });
      }
      IsResolved = true;
      return true;
    }

    public virtual void PopRule() {
    }

    public void ReplaceInnerText(GrammarToken innerToken) {
      // Update the Resolved text, replacing inner token Raw with Resolved.
      var start = Resolved.IndexOf(innerToken.Raw);
      if (start < 0 || start >= Resolved.Length) {
        return;
      }
      Resolved = Resolved.Substring(0, start) + innerToken.Resolved + Resolved.Substring(start + innerToken.Raw.Length);

      // Update the positions of any IndecesOfInterest relative to replaced text.
      if (innerToken.Raw.Length == innerToken.Resolved.Length) {
        return;
      }
      var difference = innerToken.Raw.Length - innerToken.Resolved.Length;
      if (IndecesOfInterest == null || IndecesOfInterest.Count == 0) {
        return;
      }
      for (var i = 0; i < IndecesOfInterest.Count; i++) {
        if (start <= IndecesOfInterest[i]) {
          IndecesOfInterest[i] -= difference;
          if (IndecesOfInterest[i] < 0) {
            IndecesOfInterest.RemoveAt(i);
            i--;
          }
        } else {
          continue;
        }
      }
    }

    protected void AddIndexOfInterest() {
      if (IndecesOfInterest == null) {
        IndecesOfInterest = new List<int>();
      }
      IndecesOfInterest.Add(Raw.Length);
    }

    protected string GetSubstringOfInterest(int region) {
      if (IndecesOfInterest == null || IndecesOfInterest.Count == 0) {
        return Resolved ?? Raw;
      }
      if (region < 0) {
        return "";
      }
      var resolvedOrRaw = Resolved ?? Raw;
      if (region == 0) {
        return resolvedOrRaw.Substring(0, IndecesOfInterest[0]);
      }
      // Shave off the character at the actual index.
      var start = IndecesOfInterest[region - 1] + 1;
      var end = region == IndecesOfInterest.Count ? resolvedOrRaw.Length : IndecesOfInterest[region];
      var length = end - start;
      if (length < 0) {
        return "";
      }
      return resolvedOrRaw.Substring(start, length);
    }

    public void AddChild(GrammarToken child) {
      var open = FindLowestOpenToken();
      if (open.Children == null) {
        open.Children = new List<GrammarToken>();
      }
      open.Children.Add(child);
      child.Parent = open;
    }

    public GrammarToken FindLowestOpenToken() {
      if (Children == null) {
        return this;
      }

      for (var i = Children.Count - 1; i >= 0; i--) {
        if (Children[i].IsResolved) {
          continue;
        }
        return Children[i].FindLowestOpenToken();
      }
      return this;
    }

    public GrammarToken FindLowestOpenOfType(TagType type) {
      if (Children == null) {
        return Type == type ? this : null;
      }

      for (var i = Children.Count - 1; i >= 0; i--) {
        if (Children[i].IsResolved) {
          continue;
        }
        var lowest = Children[i].FindLowestOpenOfType(type);
        if (lowest == null) {
          continue;
        }
        return lowest;
      }
      return Type == type ? this : null;
    }

    /// <summary>
    /// Put logic handling special characters here (such as ':' in an action or '.' in a tag).
    /// Adds the character to this tag as well as any open child rags recursively, for text replacement.
    /// </summary>
    /// <param name="ch">Character to add.</param>
    /// <param name="escaped">Whether character is escaped (always add to Raw).</param>
    public void AddChar(char ch, bool escaped = false) {
      // If this token is closed, add nothing.
      if (IsResolved) {
        return;
      }
      // String also needs to be added to parent string for replacement,
      // but only literally (treat as escaped).
      if (Parent != null) {
        Parent.AddChar(ch, true);
      }
      if (escaped) {
        Raw += ch;
        return;
      }
      AddCharInner(ch);
    }

    protected virtual void AddCharInner(char ch) {
      Raw += ch;
      return;
    }
  }
}