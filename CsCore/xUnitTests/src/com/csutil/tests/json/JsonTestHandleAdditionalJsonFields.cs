using System;
using System.Collections.Generic;
using System.Diagnostics;
using com.csutil.json;
using Newtonsoft.Json;
using Xunit;

namespace com.csutil.tests.json {

    public class JsonTestHandleAdditionalJsonFields {

        private class MyClass1 {
            public string myString;
            public string myString2;
            public MyClass1 myComplexField { get; set; }
        }

        private class MyClass2 : HandleAdditionalJsonFields {
            public string myString = "Some string";

            public Dictionary<string, object> AdditionalJsonFields { get; set; }
        }

        [Fact]
        public void TestKeepMissingFieldsInClass() {
            var x1 = new MyClass1() { myString = "s1", myString2 = "s2", myComplexField = new MyClass1() { myString2 = "s11" } };
            var x1JsonString = JsonWriter.GetWriter().Write(x1);
            var x2 = JsonReader.GetReader().Read<MyClass2>(x1JsonString);
            // myString2 and myComplexChildField are missing x2 as fields/porperties so the count of additionl json fields must be 2:
            Assert.Equal(2, x2.AdditionalJsonFields.Count);
            Assert.Equal(x1.myString, x2.myString);
            // The json will still contain the additional fields since they are attached again during serialization:
            var x2JsonString = JsonWriter.GetWriter().Write(x2);
            // Now parse it back to a MySubClass1 type:
            var x3 = JsonReader.GetReader().Read<MyClass1>(x2JsonString);
            // Ensure that all fields from the original x1 are still there:
            Assert.Equal(x1.myString, x3.myString);
            Assert.Equal(x1.myString2, x3.myString2);
            Assert.Equal(x1.myComplexField.myString2, x3.myComplexField.myString2);
        }

    }

}