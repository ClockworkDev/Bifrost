using System;
using ClockworkHolographicClient.Content;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;

namespace ClockworkHolographicClient.Common
{
    // Sample gesture handler.
    // Hooks up events to recognize a tap gesture, and keeps track of input using a boolean value.
    public class SpatialInputHandler
    {
        // API objects used to process gesture input, and generate gesture events.
        private SpatialInteractionManager interactionManager;

        // Used to indicate that a Pressed input event was received this frame.
        private SpatialInteractionSourceState sourceState;

        SpatialCoordinateSystem currentCoordinateSystem;

        // Creates and initializes a GestureRecognizer that listens to a Person.
        public SpatialInputHandler()
        {
            // The interaction manager provides an event that informs the app when
            // spatial interactions are detected.
            interactionManager = SpatialInteractionManager.GetForCurrentView();

            // Bind a handler to the SourcePressed event.
            interactionManager.SourcePressed += this.OnSourcePressed;

        }

        public void OnSourcePressed(SpatialInteractionManager sender, SpatialInteractionSourceEventArgs args)
        {
            ClockworkSocket.processInput("tap",args.State.TryGetPointerPose(this.currentCoordinateSystem));
        }

        internal void setCoordinateSystem(SpatialCoordinateSystem currentCoordinateSystem)
        {
            this.currentCoordinateSystem = currentCoordinateSystem;
        }
    }
}