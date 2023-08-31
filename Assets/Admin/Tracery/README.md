A full C#/.NET 3.5 port of Kate Compton's [Tracery](http://tracery.io "Tracery") procedural generation tool, including. support for modifiers and actions.

# Creating a TraceryGrammar Object

    using UnityTracery;
    
    ...
    
    var grammarString = @"
    {
      'origin': ['#sentence#'],
      'sentence': ['#[#setPronouns#][#setOccupation#][hero:#name#]story#'],
      ...
    }";
    var grammar = new TraceryGrammar(grammarString);

# Generating Text

    // Automatically generates from the base symbol "#origin#".
    var originOutput = grammar.Generate();
    // Parses custom text which may be a single symbol or mixed.
    var roomOutput = grammar.Parse("#room#");
    var heroInput = "[#setPronouns#][hero:#name#]#name# was a great hero. This is #heroTheir# story.";
    var heroIntro = grammar.Parse(heroInput);

# Actions

## Options

Actions can have multiple options which evaluate randomly when invoked, just like regular grammar rules.

    var grammar = new TraceryGrammar(@"
    {
      'output': ['#color.capitalize# is pretty. #color.capitalize# is really, really pretty.']
    }");
    Debug.Log(grammar.Parse("[color:red,yellow,green,blue,purple]#output#"));

The above call will result in text similar to:

    Red is pretty. Blue is really, really pretty.
    Purple is pretty. Green is really, really pretty.
    Yellow is pretty. Yellow is really, really pretty.

If you want a consistent #color# you can use another action to offer options, then select an option with the [color] action itself.

    Debug.Log(grammar.Parse("[color:#[select:red,yellow,green,blue,purple]select#]#output#"));

In this example, the symbol "color" will always evaluate to the same thing both times.

    Blue is pretty. Blue is really, really pretty.
    Purple is pretty. Purple is really, really pretty.

## Scope

Actions' replacement rules are created and destroyed within the applicable scope.
I.e. an action that is defined in the base text remains for the entire call to parse.

    var input = "I've made my decision: [decision:yes]#decision#. One thousand times #decision#.";
    Debug.Log(grammar.Parse(input));

Will output

    I've made my decision: yes. One thousand times yes.

You can also override symbols within local scopes by putting the action inside the symbol's #'s.

    var input = "I have decided [decision:yes]#decision#. " +
      "At first I decided #[decision:no]decision#, " +
      "but then I changed my mind to #decision#."
    Debug.Log(grammar.Parse(input));

Because the second invocation of #decision# includes an action that overwrites the definition of [decision] within the context of that tag only, it evaluates differently. Once that tag goes out of scope, the previous definition of [decision] returns. The output of the above command is therefore:

    I have decided yes. At first I decided no, but then I changed my mind to yes.

In this way, even grammar rules can be overridden within the context of an evaluation of a string.

# Implementation Details

Most C# ports of unity are based on regexes to find tags within strings. However, for a truly recursive solution that supports actions and modifiers, the regex gets to be very, very long and unreadable for humans. It also significantly increases the time to evaluate versus evaluating strings character-by-character, like the original tracery.io does in javascript. This is why I ditched the regex solution for the incremental one.

# Credits

The base modifiers (a, pluralize, capitalize, etc) are mostly lifted from [Tracery.Net](https://github.com/josh-perry/Tracery.Net). However, that library does not work with Unity (.NET 3.5), and does not include support for actions.

The Asset depends on the [JSON Object Asset](https://assetstore.unity.com/packages/tools/input-management/json-object-710), and provides one extension method to JSONObject: "Random()", which returns a random entry in a true JSONObject or JSONArray, or the entry itself for other types.
