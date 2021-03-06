﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace com.csutil {

    public class MainThread : MonoBehaviour {

        public static MainThread instance { get { return IoC.inject.GetOrAddComponentSingleton<MainThread>(new object()); } }

        public static bool isMainThread { get { return mainThreadRef.Equals(Thread.CurrentThread); } }

        private static Thread mainThreadRef;

        public long maxAllowedTaskDurationInMsPerFrame = 33;
        private Stopwatch stopWatch;
        private ConcurrentQueue<Action> actionsForMainThread = new ConcurrentQueue<Action>();

        private void Awake() {
            Log.d("Now initializing MainThread helper");
            AssertV2.IsTrue(mainThreadRef == null || mainThreadRef == Thread.CurrentThread, "MainThread already set to " + mainThreadRef);
            mainThreadRef = Thread.CurrentThread;
            stopWatch = Stopwatch.StartNew();
        }

        private void Update() {
            if (!actionsForMainThread.IsEmpty) {
                stopWatch.Restart();
                while (!actionsForMainThread.IsEmpty) {
                    // if the tasks take too long do the rest of the waiting tasks in the next frame:
                    if (stopWatch.ElapsedMilliseconds > maxAllowedTaskDurationInMsPerFrame) {
                        Log.w("Will wait until next frame to run the remaining " + actionsForMainThread.Count + " tasks");
                        break;
                    }
                    Action a; if (actionsForMainThread.TryDequeue(out a)) {
                        try { a.Invoke(); } catch (Exception e) { Log.e(e); }
                    }
                }
            }
        }

        public static void Invoke(Action a) { instance.ExecuteOnMainThread(a); }

        public void ExecuteOnMainThread(Action a) { actionsForMainThread.Enqueue(a); }

    }

}
