using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RosellaInterpreter;
using static RosellaInterpreter.TokenType;

namespace RosellaTests
{
    [TestClass]
    public class ScannerTests
    {
        [TestMethod]
        public void scanTokens_tokenCount()
        {
            // Arrange: given we have a simple statement
            var text = "var a = \"b\"";

            // and a scanner
            var scanner = new Scanner(text, error);

            // Act: execute the action
            var tokens = scanner.scanTokens();

            // Assert: we have been returned 5 tokens
            Assert.AreEqual(5, tokens.Count);
        }

        [TestMethod]
        public void scanTokens_multilineString()
        {
            // Arrange: given we have a multiline string statement
            var text = "\"line1\nline2\nline3\"";

            // and a scanner
            var scanner = new Scanner(text, error);

            // Act: execute the action
            var tokens = scanner.scanTokens();

            // Assert: we have been returned a multiline string
            Assert.AreEqual(tokens[0].lexeme, text);
        }

        private void error(int line, string text)
        {

        }
    }
}
