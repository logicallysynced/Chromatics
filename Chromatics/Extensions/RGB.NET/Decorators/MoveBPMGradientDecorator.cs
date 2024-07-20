using RGB.NET.Core;
using RGB.NET.Presets.Textures.Gradients;
using System;

namespace RGB.NET.Presets.Decorators
{
    /// <inheritdoc cref="AbstractUpdateAwareDecorator" />
    /// <inheritdoc cref="IGradientDecorator" />
    /// <summary>
    /// Represents a decorator which allows to move an <see cref="T:RGB.NET.Presets.Gradients.IGradient" /> by modifying its offset based on BPM.
    /// </summary>
    public class MoveBPMGradientDecorator : AbstractUpdateAwareDecorator, IGradientDecorator
    {
        #region Properties & Fields
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper disable MemberCanBePrivate.Global

        /// <summary>
        /// Gets or sets the direction the <see cref="IGradient"/> is moved.
        /// True leads to an offset-increment (normally moving to the right), false to an offset-decrement (normally moving to the left).
        /// </summary>
        public bool Direction { get; set; }

        /// <summary>
        /// Gets or sets the BPM (Beats Per Minute) which determines the speed of the movement.
        /// </summary>
        public int BPM { get; set; }

        private float speed;
        private const float FULL_CYCLE = 360.0f;

        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
        #endregion

        #region Constructors

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:RGB.NET.Presets.Decorators.MoveBPMGradientDecorator" /> class.
        /// </summary>
        /// <param name="surface">The surface this decorator belongs to.</param>
        /// <param name="bpm">The BPM (Beats Per Minute) which determines the speed of the movement.</param>
        /// <param name="direction">The direction the <see cref="T:RGB.NET.Presets.Gradients.IGradient" /> is moved.
        /// True leads to an offset-increment (normally moving to the right), false to an offset-decrement (normally moving to the left).</param>
        public MoveBPMGradientDecorator(RGBSurface surface, int bpm = 120, bool direction = true)
            : base(surface)
        {
            this.BPM = bpm;
            this.Direction = direction;
            CalculateSpeed();
        }

        #endregion

        #region Methods

        private void CalculateSpeed()
        {
            // Convert BPM to units per second (360 units per cycle, 1 cycle per beat)
            speed = FULL_CYCLE / (60.0f / BPM);
        }

        /// <inheritdoc />
        protected override void Update(double deltaTime)
        {
            float movement = speed * (float)deltaTime;

            if (!Direction)
                movement = -movement;

            foreach (IDecoratable decoratedObject in DecoratedObjects)
                if (decoratedObject is IGradient gradient)
                    gradient.Move(movement);
        }

        #endregion
    }
}
