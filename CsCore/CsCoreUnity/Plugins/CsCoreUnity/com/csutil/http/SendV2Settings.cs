﻿using com.csutil.datastructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace com.csutil.http {

    public class Response<T> {
        public Action<T> onResult;
        public Action<UnityWebRequest, Exception> onError = (r, e) => { Log.e(e); };
        public Action<float> onProgress;
        public long maxMsWithoutProgress = 60000;
        public StackTrace stacktrace = new StackTrace();
        public WaitForSeconds wait = new WaitForSeconds(0.05f);
        public ChangeTracker<float> progressInPercent = new ChangeTracker<float>(0);
        public Func<DownloadHandler> createDownloadHandler = NewDefaultDownloadHandler;
        public Func<T> getResult = () => { throw new Exception("Request not yet finished"); };
        public Stopwatch duration;

        public Response<T> WithResultCallback(Action<T> callback) { onResult = callback; return this; }
        public Response<T> WithProgress(Action<float> callback) { onProgress = callback; return this; }

        private static DownloadHandler NewDefaultDownloadHandler() {
            if (typeof(Texture2D).IsAssignableFrom<T>()) { return new DownloadHandlerTexture(false); }
            return new DownloadHandlerBuffer();
        }
    }

}