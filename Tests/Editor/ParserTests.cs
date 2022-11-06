using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ibralogue.Editor.Tests
{
    public class ParserTests
    {
        [Test]
        public void EmptyDialogue_Throws_Exception()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueAsset>();
            dialogue.Content = "";

            TestDelegate action = () => DialogueParser.ParseDialogue(dialogue);

            Assert.That(action, Throws.ArgumentException);
        }
    }
}
