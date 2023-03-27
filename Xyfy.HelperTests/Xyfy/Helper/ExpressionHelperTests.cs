using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xyfy.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xyfy.Helper.Tests
{
    [TestClass()]
    public class ExpressionHelperTests
    {

        internal class TestSetProperty
        {
            public string? StringValue { get; set; }

            public Uri? Uri { get; set; }

            public List<string>? ListValue { get; set; }

            public int IntValue { get; set; }
            public int? IntNullableValue { get; set; }
        }



        [TestMethod()]
        public void HashToAnchorStringTest()
        {
            Assert.Fail();
        }

        [DataRow("Masa团队", "https://www.baidu.com", null, 1, 0)]
        [DataRow("Masa团队", "https://www.baidu.com", null, 2, null)]
        [TestMethod()]
        public void SetPropertyValueTest(string? stringValue, string? uri, string listString, int? intValue, int intNullableValue)
        {
            TestSetProperty test = new TestSetProperty();
            ExpressionHelper.SetPropertyValue(test, nameof(test.StringValue), stringValue);
            ExpressionHelper.SetPropertyValue(test, nameof(test.Uri), new Uri(uri));
            ExpressionHelper.SetPropertyValue(test, nameof(test.ListValue), listString?.Split(','));
            ExpressionHelper.SetPropertyValue(test, nameof(test.IntValue), intValue);
            ExpressionHelper.SetPropertyValue(test, nameof(test.IntNullableValue), intNullableValue);
            Assert.AreEqual(test.StringValue, stringValue);
            Assert.AreEqual(test.Uri, new Uri(uri));
            Assert.AreEqual(test.ListValue, listString?.Split(','));
            Assert.AreEqual(test.IntNullableValue, intNullableValue);
        }
    }
}