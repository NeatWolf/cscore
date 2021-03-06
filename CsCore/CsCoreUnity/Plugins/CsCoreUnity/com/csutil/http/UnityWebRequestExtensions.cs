﻿using com.csutil;
using com.csutil.http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace com.csutil {

    public static class UnityWebRequestExtensions {

        public static RestRequest SendV2(this UnityWebRequest self) {
            return new UnityRestRequest(self);
        }

        public static IEnumerator SendWebRequestV2<T>(this UnityWebRequest self, Response<T> s) {
            yield return self.SendAndWait(s);
            HandleResult(self, s);
        }

        private static IEnumerator SendAndWait<T>(this UnityWebRequest self, Response<T> resp) {
            SetupDownloadAndUploadHanders(self, resp);
            resp.duration = Stopwatch.StartNew();
            var timer = Stopwatch.StartNew();
            self.ApplyAllCookiesToRequest();
            resp.debugInfo = self.method + " " + self.url + " with cookies=[" + self.GetRequestHeader("Cookie") + "]";
            Log.d("Sending: " + resp);
            var req = self.SendWebRequest();
            timer.AssertUnderXms(40);
            while (!req.isDone) {
                var currentProgress = req.progress * 100;
                if (resp.progressInPercent.setNewValue(currentProgress)) {
                    timer.Restart();
                    resp.onProgress.InvokeIfNotNull(resp.progressInPercent.value);
                }
                yield return resp.wait;
                if (timer.ElapsedMilliseconds > resp.maxMsWithoutProgress) { self.Abort(); }
            }
            resp.duration.Stop();
            Log.d("   > Finished " + resp);
            AssertResponseLooksNormal(self, resp);
            self.SaveAllNewCookiesFromResponse();
            if (self.error.IsNullOrEmpty()) { resp.progressInPercent.setNewValue(100); }
            resp.getResult = () => { return self.GetResult<T>(); };
        }

        private static void SetupDownloadAndUploadHanders<T>(UnityWebRequest self, Response<T> resp) {
            switch (self.method) {
                case UnityWebRequest.kHttpVerbGET:
                    AssertV2.IsNotNull(self.downloadHandler, "Get-request had no downloadHandler set");
                    break;
                case UnityWebRequest.kHttpVerbPUT:
                case UnityWebRequest.kHttpVerbPOST:
                    AssertV2.IsNotNull(self.uploadHandler, "Put/Post-request had no uploadHandler set");
                    break;
            }
            if (self.downloadHandler == null && resp.onResult != null) { self.downloadHandler = resp.createDownloadHandler(); }
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        private static void AssertResponseLooksNormal<T>(UnityWebRequest self, Response<T> resp) {
            AssertV2.IsNotNull(self, "WebRequest object was null: " + resp);
            if (self != null) {
                AssertV2.IsTrue(self.isDone, "Request never finished: " + resp);
                if (self.isNetworkError) { Log.w("isNetworkError=true for " + resp); }
                if (self.error != null) { Log.w("error=" + self.error + " for " + resp); }
                if (self.isHttpError) { Log.w("isHttpError=true for " + resp); }
                if (self.responseCode < 200 || self.responseCode >= 300) { Log.w("responseCode=" + self.responseCode + " for " + resp); }
                if (self.isNetworkError && self.responseCode == 0 && self.useHttpContinue) { Log.w("useHttpContinue flag was true, request might work if its set to false"); }
            }
        }

        private static void HandleResult<T>(UnityWebRequest self, Response<T> resp) {
            if (self.isNetworkError || self.isHttpError) {
                resp.onError(self, new Exception(self.error));
            } else {
                try { resp.onResult.InvokeIfNotNull(self.GetResult<T>()); } catch (Exception e) { resp.onError(self, e); }
            }
        }

        public static T GetResult<T>(this UnityWebRequest self) { return self.GetResult<T>(JsonReader.GetReader()); }

        public static T GetResult<T>(this UnityWebRequest self, IJsonReader r) {
            AssertV2.IsTrue(self.isDone, "web request was not done!");
            if (TypeCheck.AreEqual<T, UnityWebRequest>()) { return (T)(object)self; }
            if (typeof(Texture2D).IsCastableTo(typeof(T))) {
                AssertV2.IsTrue(self.downloadHandler is DownloadHandlerTexture, "self.downloadHandler was not a DownloadHandlerTexture");
                var h = (DownloadHandlerTexture)self.downloadHandler;
                return (T)(object)h.texture;
            }
            if (TypeCheck.AreEqual<T, Headers>()) { return (T)(object)self.GetResponseHeadersV2(); }
            var text = self.downloadHandler.text;
            if (TypeCheck.AreEqual<T, String>()) { return (T)(object)text; }
            return r.Read<T>(text);
        }

        public static UnityWebRequest SetRequestHeaders(this UnityWebRequest self, Headers headersToAdd) {
            if (!headersToAdd.IsNullOrEmpty()) { foreach (var h in headersToAdd) { self.SetRequestHeader(h.Key, h.Value); } }
            return self;
        }

        public static Headers GetResponseHeadersV2(this UnityWebRequest self) { return new Headers(self.GetResponseHeaders()); }

    }

}
