using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using com.csutil.datastructures;
using com.csutil.encryption;
using com.csutil.random;
using Xunit;

namespace com.csutil.tests {

    public class StateMachineTests {

        private enum MyStates { MyState1, MyState2, MyState3 }

        [Fact]
        public static void StateMachine_ExampleUsage1() {

            // First define a set of allowed transitions to define the state machine:
            var stateMachine = new Dictionary<MyStates, HashSet<MyStates>>();
            stateMachine.AddToValues(MyStates.MyState1, MyStates.MyState2);
            stateMachine.AddToValues(MyStates.MyState2, MyStates.MyState3);

            // Initialize a state-machine:
            MyStates currentState = MyStates.MyState1;

            // It is possible to listen to state machine transitions:
            StateMachine.SubscribeToAllTransitions(new object(), (MyStates oldState, MyStates newState) => {
                Log.d("Transitioned from " + oldState + " to " + newState);
            });
            // And its possible to listen only to specific transitions:
            StateMachine.SubscribeToTransition(new object(), MyStates.MyState1, MyStates.MyState2, () => {
                Log.d("Transitioned from 1 => 2");
            });

            // Transition the state-machine from state 1 to 2:
            currentState = stateMachine.TransitionTo(currentState, MyStates.MyState2);
            Assert.Equal(MyStates.MyState2, currentState);

            // Invalid transitions throw exceptions (current state is 2):
            Assert.Throws<Exception>(() => {
                currentState = stateMachine.TransitionTo(currentState, MyStates.MyState1);
            });

        }

        [Fact]
        public static void StateMachine_ExampleUsage2() {

            // Create the state machine similar to the first example but wrapped in a enclosing class (see below):
            var myStateMachine = new MyStateMachineForExample2();

            // The transitions are encapsulared behind methods, so switching state is more enclosed this way:
            myStateMachine.SwitchToSecondState();
            Assert.Equal(MyStates.MyState2, myStateMachine.currentState);

            // Invalid transitions throw exceptions (current state is 2):
            Assert.Throws<Exception>(() => { myStateMachine.SwitchToFirstState(); });

        }

        /// <summary> 
        /// This state machine works the same way example usage 1 works only that it's 
        /// encapsulated to prevent direct access to modifying the state
        /// </summary>
        private class MyStateMachineForExample2 {

            private Dictionary<MyStates, HashSet<MyStates>> allowedTransitions;
            public MyStates currentState { get; private set; }

            // In the constructor the allowed transitions and the initial state are set:
            public MyStateMachineForExample2() {
                // First define a set of allowed transitions to define the state machine:
                allowedTransitions = new Dictionary<MyStates, HashSet<MyStates>>();
                allowedTransitions.AddToValues(MyStates.MyState1, MyStates.MyState2);
                allowedTransitions.AddToValues(MyStates.MyState2, MyStates.MyState3);
                // Initialize the state-machine:
                currentState = MyStates.MyState1;
            }

            public void SwitchToFirstState() { SwitchToState(MyStates.MyState1); }
            public void SwitchToSecondState() { SwitchToState(MyStates.MyState2); }
            public void SwitchToFinalState() { SwitchToState(MyStates.MyState3); }
            private void SwitchToState(MyStates newState) {
                currentState = allowedTransitions.TransitionTo(currentState, newState);
            }

        }

        [Fact]
        public static void StateMachine_TransitionEventTests() {

            var stateMachine = new Dictionary<MyStates, HashSet<MyStates>>();
            stateMachine.AddToValues(MyStates.MyState1, MyStates.MyState2);
            MyStates currentState = MyStates.MyState1;

            var listenerForAllTransitionsTriggered = false;
            var listenerForSpecificTransitionTriggered = false;
            var listenerForExitingState1Triggered = false;
            var listenerForEnteringState2Triggered = false;
            StateMachine.SubscribeToAllTransitions(new object(), (MyStates oldState, MyStates newState) => {
                listenerForAllTransitionsTriggered = true;
            });
            StateMachine.SubscribeToTransition(new object(), MyStates.MyState1, MyStates.MyState2, () => {
                listenerForSpecificTransitionTriggered = true;
            });
            StateMachine.SubscribeToStateExited(new object(), MyStates.MyState1, () => {
                listenerForExitingState1Triggered = true;
            });
            StateMachine.SubscribeToStateEntered(new object(), MyStates.MyState2, () => {
                listenerForEnteringState2Triggered = true;
            });

            currentState = stateMachine.TransitionTo(currentState, MyStates.MyState2);
            Assert.True(listenerForAllTransitionsTriggered);
            Assert.True(listenerForSpecificTransitionTriggered);
            Assert.True(listenerForExitingState1Triggered);
            Assert.True(listenerForEnteringState2Triggered);

        }

    }

}