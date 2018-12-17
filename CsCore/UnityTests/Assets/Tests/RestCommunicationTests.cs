﻿using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace com.csutil.http.tests {
    public class RestCommunicationTests {

        [UnityTest]
        public IEnumerator Test1() {
            var www = UnityWebRequest.Get("https://httpbin.org/get");

            HttpBinGetResp a = null;
            yield return www.SendWebRequestV2(new Response<HttpBinGetResp>().WithResultCallback((x) => {
                Log.d("Your IP is " + x.origin);
                a = x;
            }));
            HttpBinGetResp b = www.GetResult<HttpBinGetResp>();

            var w = JsonWriter.NewWriter();
            Assert.AreEqual(w.Write(a), w.Write(b));
        }

        [UnityTest]
        public IEnumerator Test2() {
            var resp = new Response<HttpBinGetResp>().WithProgress((p) => {
                Log.d("Now progress=" + p + "%");
            });
            yield return UnityWebRequest.Get("https://httpbin.org/get").SendWebRequestV2(resp);
            var result = resp.getResult();
            Assert.NotNull(result.origin);
            Assert.AreEqual(100, resp.progressInPercent.value);
        }

        [UnityTest]
        public IEnumerator Test3() {
            var www = UnityWebRequest.Get("https://httpbin.org/get");

            var runningTask = www.SendV2().GetResult<HttpBinGetResp>(x => {
                Log.d("Your IP is " + x.origin);
            });
            while (!runningTask.IsCompleted) {
                Log.d("Waiting..");
                yield return new WaitForSeconds(0.1f);
            }
            var x2 = runningTask.Result;
            Log.d("Your IP is " + x2.origin);
        }

        public class HttpBinGetResp {
            public Dictionary<string, object> args { get; set; }
            public string origin { get; set; }
            public string url { get; set; }
            public Headers headers { get; set; }
            public class Headers {
                public string Accept { get; set; }
                public string Accept_Encoding { get; set; }
                public string Accept_Language { get; set; }
                public string Connection { get; set; }
                public string Host { get; set; }
                public string Upgrade_Insecure_Requests { get; set; }
                public string User_Agent { get; set; }
            }
        }

    }
}
